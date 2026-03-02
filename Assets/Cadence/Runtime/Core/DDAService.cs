using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    public sealed class DDAService : IDDAService
    {
        private readonly DDAConfig _config;
        private readonly ISignalCollector _collector;
        private readonly ISessionAnalyzer _analyzer;
        private readonly IPlayerModel _playerModel;
        private readonly IFlowDetector _flowDetector;
        private readonly AdjustmentEngine _adjustmentEngine;
        private readonly ISignalStorage _storage;

        private bool _sessionActive;
        private string _currentLevelId;
        private Dictionary<string, float> _currentLevelParams;
        private float _sessionStartTime;
        private SessionSummary _lastSessionSummary;
        private AdjustmentProposal _lastProposal;

        public bool IsSessionActive => _sessionActive;
        public FlowReading CurrentFlow => _flowDetector.CurrentReading;
        public PlayerSkillProfile PlayerProfile => _playerModel.Profile;

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
        }

        public void BeginSession(string levelId, Dictionary<string, float> levelParameters)
        {
            if (_sessionActive)
            {
                Debug.LogWarning("[Cadence] BeginSession called while session is active. Ending previous session.");
                EndSession(SessionOutcome.Abandoned);
            }

            _currentLevelId = levelId;
            _currentLevelParams = levelParameters != null
                ? new Dictionary<string, float>(levelParameters)
                : new Dictionary<string, float>();

            _sessionStartTime = Time.unscaledTime;
            _collector.Reset(levelId, _sessionStartTime);
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

            // Record outcome signal
            float outcomeValue = outcome == SessionOutcome.Win ? 1f :
                                 outcome == SessionOutcome.Abandoned ? -1f : 0f;
            _collector.Record(SignalKeys.SessionOutcome, outcomeValue, SignalTier.RetryMeta);
            _collector.Record(SignalKeys.SessionEnded, 1f, SignalTier.RetryMeta);

            // Analyze the session
            _lastSessionSummary = _analyzer.Analyze(_collector.CurrentBatch);
            _lastSessionSummary.Outcome = outcome;

            // Update player model
            _playerModel.UpdateFromSession(_lastSessionSummary);

            // Persist signals if storage is available
            if (_storage != null && (_config == null || _config.EnableSignalStorage))
            {
                _storage.Save(_currentLevelId, _collector.CurrentBatch);
                int maxSessions = _config != null ? _config.MaxStoredSessions : 50;
                _storage.Prune(maxSessions);
            }

            _sessionActive = false;
        }

        public void RecordSignal(string key, float value = 1f,
            SignalTier tier = SignalTier.DecisionQuality, int moveIndex = -1)
        {
            if (!_sessionActive) return;
            _collector.Record(key, value, tier, moveIndex);
        }

        public void Tick(float deltaTime)
        {
            if (!_sessionActive) return;
            if (_config != null && !_config.EnableMidSessionDetection) return;
            _flowDetector.Tick(deltaTime, _collector.RecentSignals);
        }

        public AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters)
        {
            if (_config != null && !_config.EnableBetweenSessionAdjustment)
                return null;

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
                TimeSinceLastAdjustment = Time.unscaledTime
            };

            _lastProposal = _adjustmentEngine.Evaluate(context);
            return _lastProposal;
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
                CurrentLevelParams = _currentLevelParams
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
    }
}
