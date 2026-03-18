using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// Production implementation of <see cref="IDDAService"/>.
    /// Orchestrates signal collection, session analysis, Glicko-2 skill modeling,
    /// real-time flow detection, and difficulty adjustment proposals.
    /// </summary>
    /// <remarks>
    /// NOT thread-safe. All methods must be called from the main Unity thread.
    /// For a drag-and-drop setup, use <see cref="CadenceManager"/> instead.
    /// </remarks>
    public sealed class DDAService : IDDAService
    {
        private readonly DDAConfig _config;
        private readonly ISignalCollector _collector;
        private readonly ISessionAnalyzer _analyzer;
        private readonly IPlayerModel _playerModel;
        private readonly IFlowDetector _flowDetector;
        private readonly AdjustmentEngine _adjustmentEngine;
        private readonly ISignalStorage _storage;
        private readonly IDifficultyScheduler _scheduler;
        private readonly IPlayerProfiler _profiler;
        private readonly List<ILevelTypeConfigProvider> _levelTypeConfigProviders =
            new List<ILevelTypeConfigProvider>();

        private bool _sessionActive;
        private string _currentLevelId;
        private Dictionary<string, float> _currentLevelParams;
        private float _sessionStartTime;
        private SessionSummary _lastSessionSummary;
        private AdjustmentProposal _lastProposal;
        private LevelType _currentLevelType;
        private LevelTypeConfig _currentLevelTypeConfig;
        private PlayerArchetypeReading _currentArchetype;
        private bool _explicitAbandonRequested;
        private int _levelsCompletedInCurrentPlaySession;
        private float _lastCompletedLevelEndTime = float.NegativeInfinity;

        public bool IsSessionActive => _sessionActive;
        public FlowReading CurrentFlow => _flowDetector.CurrentReading;
        public PlayerSkillProfile PlayerProfile => _playerModel.Profile;
        public PlayerArchetypeReading CurrentArchetype => _currentArchetype;

        public DDAService(DDAConfig config, ISignalStorage storage = null)
        {
            _config = config;

            int ringCapacity = config != null ? config.RingBufferCapacity : 512;
            _collector = new SignalCollector(ringCapacity);
            _analyzer = new SessionAnalyzer();
            _playerModel = new GlickoPlayerModel(config != null ? config.PlayerModelConfig : null);
            _flowDetector = new FlowDetector(config != null ? config.FlowDetectorConfig : null);
            _adjustmentEngine = new AdjustmentEngine(config != null ? config.AdjustmentEngineConfig : null);
            _storage = storage;

            // Create difficulty scheduler if sawtooth config is provided
            if (config != null && config.SawtoothCurveConfig != null)
                _scheduler = new DifficultyScheduler(config.SawtoothCurveConfig);

            _profiler = new PlayerProfiler();
        }

        public void BeginSession(string levelId, Dictionary<string, float> levelParameters)
        {
            BeginSession(levelId, levelParameters, LevelType.Standard);
        }

        public void BeginSession(string levelId, Dictionary<string, float> levelParameters,
            LevelType type)
        {
            if (_sessionActive)
            {
                Debug.LogWarning("[Cadence] BeginSession called while session is active. Ending previous session.");
                EndSession(SessionOutcome.Abandoned);
            }

            _currentLevelId = levelId;
            _currentLevelType = type;
            _currentLevelTypeConfig = ResolveLevelTypeConfig(type);
            _currentLevelParams = levelParameters != null
                ? new Dictionary<string, float>(levelParameters)
                : new Dictionary<string, float>();
            _explicitAbandonRequested = false;

            ResetContiguousPlaySessionIfIdleGapExceeded(Time.unscaledTime);

            _sessionStartTime = Time.unscaledTime;
            _collector.Reset(levelId, _sessionStartTime);
            _collector.CurrentBatch.LevelParameters = _currentLevelParams;
            _flowDetector.Reset();
            _sessionActive = true;

            // Apply time decay if returning player
            if (_playerModel.Profile.LastSessionUtcTicks > 0)
            {
                float daysSince = (float)(DateTime.UtcNow.Ticks - _playerModel.Profile.LastSessionUtcTicks)
                    / TimeSpan.TicksPerDay;
                if (daysSince > 0.1f)
                {
                    _playerModel.ApplyTimeDecay(daysSince);
                    _collector.Record(SignalKeys.SessionGapDays, daysSince,
                        SignalTier.RetryMeta);
                }
            }

            _collector.Record(SignalKeys.SessionStarted, 1f, SignalTier.RetryMeta);
        }

        public void EndSession(SessionOutcome outcome)
        {
            if (!_sessionActive) return;

            if (_explicitAbandonRequested)
                outcome = SessionOutcome.Abandoned;

            // Record outcome signal
            float outcomeValue = outcome == SessionOutcome.Win ? 1f :
                                 outcome == SessionOutcome.Abandoned ? -1f : 0f;
            _collector.Record(SignalKeys.SessionOutcome, outcomeValue, SignalTier.RetryMeta);
            _collector.Record(SignalKeys.SessionEnded, 1f, SignalTier.RetryMeta);

            // Analyze the session
            _lastSessionSummary = _analyzer.Analyze(_collector.CurrentBatch);
            _lastSessionSummary.Outcome = outcome;
            _lastSessionSummary.LevelType = _currentLevelType;

            // Update player model (skip for replay sessions)
            if (!_lastSessionSummary.IsReplay)
                _playerModel.UpdateFromSession(_lastSessionSummary);

            // Persist signals if storage is available
            if (_storage != null && (_config == null || _config.EnableSignalStorage))
            {
                _storage.Save(_currentLevelId, _collector.CurrentBatch);
                int maxSessions = _config != null ? _config.MaxStoredSessions : 50;
                _storage.Prune(maxSessions);
            }

            if (!_lastSessionSummary.IsReplay)
                _levelsCompletedInCurrentPlaySession++;
            _lastCompletedLevelEndTime = Time.unscaledTime;
            _explicitAbandonRequested = false;
            _sessionActive = false;
        }

        public void RecordSignal(string key, float value = 1f,
            SignalTier tier = SignalTier.DecisionQuality, int moveIndex = -1)
        {
            if (!_sessionActive) return;

            if (key == SignalKeys.LevelAbandoned)
                _explicitAbandonRequested = true;

            _collector.Record(key, value, tier, moveIndex);
        }

        public void Tick(float deltaTime)
        {
            if (!_sessionActive) return;
            if (_config != null && !_config.EnableMidSessionDetection) return;
            _flowDetector.Tick(deltaTime, _collector.RecentSignals);
        }

        public AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters,
            LevelType nextLevelType, int nextLevelIndex = -1)
        {
            return GetProposal(nextLevelParameters, nextLevelType, nextLevelIndex, null);
        }

        public AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters,
            LevelType nextLevelType, int nextLevelIndex, float? simulatedTime)
        {
            if (_config != null && !_config.EnableBetweenSessionAdjustment)
                return null;

            if (_lastSessionSummary.IsReplay)
                return new AdjustmentProposal { Timing = AdjustmentTiming.BeforeNextLevel };

            var typeConfig = ResolveLevelTypeConfig(nextLevelType);

            // Tutorial levels skip DDA entirely
            if (!typeConfig.DDAEnabled)
                return new AdjustmentProposal { Timing = AdjustmentTiming.BeforeNextLevel };

            float sawtoothMult = 1f;
            if (_scheduler != null && nextLevelIndex >= 0)
                sawtoothMult = _scheduler.GetTargetMultiplier(nextLevelIndex);

            // Classify player archetype
            _currentArchetype = _profiler.Classify(_playerModel.Profile, _lastSessionSummary);

            float timeNow = simulatedTime ?? Time.unscaledTime;

            var context = new AdjustmentContext
            {
                Profile = _playerModel.Profile,
                LastSession = _lastSessionSummary,
                LastFlowReading = _flowDetector.CurrentReading,
                LevelParameters = nextLevelParameters != null
                    ? new Dictionary<string, float>(nextLevelParameters)
                    : new Dictionary<string, float>(),
                RecentHistory = _playerModel.Profile.RecentHistory,
                SessionGapDays = _lastSessionSummary.SessionGapDays,
                TimeSinceLastAdjustment = timeNow,
                LevelType = nextLevelType,
                LevelTypeConfig = typeConfig,
                SawtoothMultiplier = sawtoothMult,
                CurrentLevelIndex = nextLevelIndex,
                LevelsThisSession = _levelsCompletedInCurrentPlaySession,
                ArchetypeReading = _currentArchetype
            };

            _lastProposal = _adjustmentEngine.Evaluate(context);

            if (_lastProposal != null && _lastProposal.Deltas.Count > 0)
                _adjustmentEngine.RecordAdjustment(_lastProposal, timeNow);

            return _lastProposal;
        }

        public float GetTargetMultiplier(int levelIndex)
        {
            return _scheduler != null ? _scheduler.GetTargetMultiplier(levelIndex) : 1f;
        }

        public void RegisterRule(IAdjustmentRule rule)
        {
            if (rule == null) return;
            _adjustmentEngine.AddRule(rule);
        }

        public void RegisterRuleProvider(IAdjustmentRuleProvider provider)
        {
            if (provider == null) return;

            try
            {
                _adjustmentEngine.AddRules(provider.CreateRules(_config));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cadence] Rule provider '{provider.GetType().Name}' failed: {ex.Message}");
            }
        }

        public void RegisterLevelTypeConfigProvider(ILevelTypeConfigProvider provider)
        {
            if (provider == null) return;
            _levelTypeConfigProviders.Add(provider);
        }

        public DDADebugData GetDebugSnapshot()
        {
            return new DDADebugData
            {
                SessionActive = _sessionActive,
                CurrentLevelId = _currentLevelId,
                SessionTime = _sessionActive ? Time.unscaledTime - _sessionStartTime : 0f,
                TotalSignals = _collector.TotalSignalCount,
                RingBufferCount = _collector.RecentSignals.Count,
                CurrentFlow = _flowDetector.CurrentReading,
                Profile = _playerModel.Profile,
                LastSessionSummary = _lastSessionSummary,
                LastProposal = _lastProposal,
                CurrentLevelParams = _currentLevelParams,
                CurrentLevelType = _currentLevelType,
                ArchetypeReading = _currentArchetype,
                LevelsThisSession = _levelsCompletedInCurrentPlaySession,
                SessionFatigueActive = IsSessionFatigueEnabled() &&
                    _levelsCompletedInCurrentPlaySession >= GetFatigueThresholdLevels(),
                ExplicitAbandonPending = _explicitAbandonRequested,
                ParMoves = _currentLevelParams != null && _currentLevelParams.ContainsKey("par_moves")
                    ? _currentLevelParams["par_moves"] : 0f,
                SkillIndex = _lastSessionSummary.SkillIndex,
                IsReplaySession = _lastSessionSummary.IsReplay
            };
        }

        /// <summary>
        /// Load a previously serialized player profile.
        /// Call this after construction and before BeginSession.
        /// </summary>
        public void LoadProfile(string json)
        {
            _playerModel.Deserialize(json);
        }

        /// <summary>
        /// Serialize the current player profile for persistence.
        /// </summary>
        public string SaveProfile()
        {
            return _playerModel.Serialize();
        }

        private void ResetContiguousPlaySessionIfIdleGapExceeded(float currentTime)
        {
            if (float.IsNegativeInfinity(_lastCompletedLevelEndTime))
                return;

            float resetGapSeconds = GetFatigueResetGapSeconds();
            if (resetGapSeconds <= 0f || currentTime - _lastCompletedLevelEndTime > resetGapSeconds)
                _levelsCompletedInCurrentPlaySession = 0;
        }

        private float GetFatigueResetGapSeconds()
        {
            var adjustmentConfig = _config != null ? _config.AdjustmentEngineConfig : null;
            float resetGapMinutes = adjustmentConfig != null
                ? adjustmentConfig.SessionFatigueResetGapMinutes
                : AdjustmentEngineConfig.DefaultSessionFatigueResetGapMinutes;
            return Mathf.Max(0f, resetGapMinutes) * 60f;
        }

        private int GetFatigueThresholdLevels()
        {
            var adjustmentConfig = _config != null ? _config.AdjustmentEngineConfig : null;
            int threshold = adjustmentConfig != null
                ? adjustmentConfig.SessionFatigueThresholdLevels
                : AdjustmentEngineConfig.DefaultSessionFatigueThresholdLevels;
            return Mathf.Max(1, threshold);
        }

        private bool IsSessionFatigueEnabled()
        {
            var adjustmentConfig = _config != null ? _config.AdjustmentEngineConfig : null;
            return adjustmentConfig == null || adjustmentConfig.EnableSessionFatigueRule;
        }

        private LevelTypeConfig ResolveLevelTypeConfig(LevelType type)
        {
            for (int i = _levelTypeConfigProviders.Count - 1; i >= 0; i--)
            {
                var provider = _levelTypeConfigProviders[i];
                if (provider == null) continue;

                try
                {
                    if (provider.TryGetLevelTypeConfig(type, out var config) && config != null)
                        return config;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Cadence] LevelTypeConfig provider '{provider.GetType().Name}' failed for {type}: {ex.Message}");
                }
            }

            return LevelTypeDefaults.GetDefaults(type);
        }
    }
}
