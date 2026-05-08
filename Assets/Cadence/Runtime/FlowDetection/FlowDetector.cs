using System;
using UnityEngine;

namespace Cadence
{
    public sealed class FlowDetector : IFlowDetector
    {
        // ───────────────────── Named Constants ─────────────────────

        // Confidence calculation
        private const float ConfidenceBase = 0.5f;
        private const float ConfidenceIncrement = 0.1f;
        private const float MissingTempoConfidenceScale = 0.65f;
        private const float MissingEngagementConfidenceScale = 0.8f;

        // Frustration score weights
        private const float FrustrationEfficiencyWeight = 0.5f;
        private const float FrustrationTempoWeight = 0.3f;
        private const float FrustrationEngagementWeight = 0.2f;

        private readonly FlowDetectorConfig _config;
        private readonly FlowWindow _tempoWindow;
        private readonly FlowWindow _efficiencyWindow;
        private readonly FlowWindow _engagementWindow;

        private FlowReading _currentReading;
        private FlowState _candidateState;
        private int _candidateCount;
        private int _movesSeen;
        private float _lastMoveTime;
        private float _sessionTime;
        private bool _usingExplicitInterMoveIntervals;

        // EMA smoothed scores
        private float _smoothedTempo;
        private float _smoothedEfficiency;
        private float _smoothedEngagement;

        // Track last processed signal count to detect new signals
        private long _lastProcessedSignalCount;

        public FlowReading CurrentReading => _currentReading;
        public event Action<FlowReading> OnFlowStateChanged;

        public FlowDetector(FlowDetectorConfig config)
        {
            _config = config;
            _tempoWindow = new FlowWindow(config != null ? config.TempoWindowSize : 8);
            _efficiencyWindow = new FlowWindow(config != null ? config.EfficiencyWindowSize : 12);
            _engagementWindow = new FlowWindow(config != null ? config.EngagementWindowSize : 20);
            Reset();
        }

        public void Reset()
        {
            _tempoWindow.Clear();
            _efficiencyWindow.Clear();
            _engagementWindow.Clear();
            _currentReading = default;
            _candidateState = FlowState.Unknown;
            _candidateCount = 0;
            _movesSeen = 0;
            _lastMoveTime = -1f;
            _sessionTime = 0f;
            _usingExplicitInterMoveIntervals = false;
            _smoothedTempo = 0.5f;
            _smoothedEfficiency = 0.5f;
            _smoothedEngagement = 0.5f;
            _lastProcessedSignalCount = 0;
        }

        public void Tick(float deltaTime, SignalRingBuffer recentSignals)
        {
            _sessionTime += deltaTime;

            if (recentSignals == null || recentSignals.Count == 0) return;

            // Process only new signals since last tick
            long totalPushed = recentSignals.TotalPushed;
            long newSignalCount = totalPushed - _lastProcessedSignalCount;
            if (newSignalCount <= 0)
            {
                _lastProcessedSignalCount = totalPushed;
                return;
            }

            // Clamp to buffer size (can't read signals that were overwritten)
            int newSignals = (int)System.Math.Min(newSignalCount, recentSignals.Count);

            // Process new signals (from oldest new to newest)
            for (int i = newSignals - 1; i >= 0; i--)
            {
                ProcessSignal(recentSignals[i]);
            }
            _lastProcessedSignalCount = totalPushed;

            // Update scores with EMA smoothing
            bool hasTempoData = _tempoWindow.Count > 0;
            bool hasEfficiencyData = _efficiencyWindow.Count > 0;
            bool hasEngagementData = _engagementWindow.Count > 0;

            float alpha = _config != null ? Mathf.Clamp(_config.ExponentialAlpha, 0.01f, 1f) : 0.3f;
            float rawTempo = hasTempoData ? _tempoWindow.ConsistencyScore() : 0.5f;
            float rawEfficiency = hasEfficiencyData ? _efficiencyWindow.Mean : 0.5f;
            float rawEngagement = hasEngagementData ? ComputeEngagement(recentSignals) : 0.5f;

            _smoothedTempo = Mathf.Lerp(_smoothedTempo, rawTempo, alpha);
            _smoothedEfficiency = Mathf.Lerp(_smoothedEfficiency, rawEfficiency, alpha);
            _smoothedEngagement = Mathf.Lerp(_smoothedEngagement, rawEngagement, alpha);

            // Classify
            int warmup = Mathf.Max(1, _config != null ? _config.WarmupMoves : 5);
            FlowState classified = _movesSeen < warmup
                ? FlowState.Unknown
                : Classify(_smoothedTempo, _smoothedEfficiency, _smoothedEngagement,
                    hasTempoData, hasEfficiencyData, hasEngagementData);

            // Hysteresis
            int hysteresis = Mathf.Max(1, _config != null ? _config.HysteresisCount : 3);
            if (classified == _candidateState)
            {
                _candidateCount++;
            }
            else
            {
                _candidateState = classified;
                _candidateCount = 1;
            }

            FlowState previousState = _currentReading.State;
            if (_candidateCount >= hysteresis)
            {
                _currentReading.State = _candidateState;
            }

            // Build reading
            float confidence = _movesSeen < warmup
                ? Mathf.Clamp01((float)_movesSeen / warmup * ConfidenceBase)
                : Mathf.Clamp01(ConfidenceBase + _candidateCount * ConfidenceIncrement);
            confidence *= SignalAvailabilityConfidenceScale(hasTempoData, hasEfficiencyData,
                hasEngagementData);

            _currentReading.Confidence = confidence;
            _currentReading.TempoScore = _smoothedTempo;
            _currentReading.EfficiencyScore = _smoothedEfficiency;
            _currentReading.EngagementScore = _smoothedEngagement;
            _currentReading.SessionTime = _sessionTime;

            if (_currentReading.State != previousState)
            {
                OnFlowStateChanged?.Invoke(_currentReading);
            }
        }

        private void ProcessSignal(SignalEntry entry)
        {
            switch (entry.Key)
            {
                case SignalKeys.MoveExecuted:
                    _movesSeen++;
                    if (!_usingExplicitInterMoveIntervals && _lastMoveTime >= 0f)
                    {
                        float interval = entry.Timestamp.SessionTime - _lastMoveTime;
                        _tempoWindow.Push(interval);
                    }
                    if (!_usingExplicitInterMoveIntervals)
                        _lastMoveTime = entry.Timestamp.SessionTime;
                    break;

                case SignalKeys.InterMoveInterval:
                    if (!_usingExplicitInterMoveIntervals)
                    {
                        _usingExplicitInterMoveIntervals = true;
                        _tempoWindow.Clear();
                        _lastMoveTime = -1f;
                    }

                    if (entry.Value > 0f)
                        _tempoWindow.Push(entry.Value);
                    break;

                case SignalKeys.MoveOptimal:
                    _efficiencyWindow.Push(ApplySignalConfidence(entry));
                    break;

                case SignalKeys.PauseTriggered:
                case SignalKeys.InputRejected:
                    _engagementWindow.Push(0f);
                    break;

                case SignalKeys.ProgressDelta:
                    if (entry.Value > 0f)
                        _engagementWindow.Push(1f);
                    break;

                case SignalKeys.UndoStreak:
                    if (entry.Value >= 2f)
                        _engagementWindow.Push(0f);
                    break;

                case SignalKeys.FrustrationTrigger:
                    _engagementWindow.Push(0f);
                    _efficiencyWindow.Push(0f);
                    break;
            }
        }

        private static float ApplySignalConfidence(SignalEntry entry)
        {
            float confidence = entry.HasConfidence ? entry.Confidence : 1f;
            return Mathf.Lerp(0.5f, Mathf.Clamp01(entry.Value), Mathf.Clamp01(confidence));
        }

        private float ComputeEngagement(SignalRingBuffer recentSignals)
        {
            if (_engagementWindow.Count == 0) return 0.5f;
            return _engagementWindow.Mean;
        }

        private static float SignalAvailabilityConfidenceScale(bool hasTempoData,
            bool hasEfficiencyData,
            bool hasEngagementData)
        {
            if (!hasEfficiencyData)
                return 0f;

            float scale = 1f;
            if (!hasTempoData)
                scale *= MissingTempoConfidenceScale;
            if (!hasEngagementData)
                scale *= MissingEngagementConfidenceScale;
            return scale;
        }

        private FlowState Classify(float tempo, float efficiency, float engagement,
            bool hasTempoData,
            bool hasEfficiencyData,
            bool hasEngagementData)
        {
            if (!hasEfficiencyData)
                return FlowState.Unknown;

            float boredomEff = _config != null ? _config.BoredomEfficiencyMin : 0.85f;
            float boredomTempo = _config != null ? _config.BoredomTempoMin : 0.7f;
            float anxietyEff = _config != null ? _config.AnxietyEfficiencyMax : 0.3f;
            float anxietyTempo = _config != null ? _config.AnxietyTempoMax : 0.2f;
            float frustrationThreshold = _config != null ? _config.FrustrationThreshold : 0.7f;

            if (hasEngagementData)
            {
                float frustrationScore = (1f - efficiency) * FrustrationEfficiencyWeight
                    + (hasTempoData ? (1f - tempo) * FrustrationTempoWeight : 0f)
                    + (1f - engagement) * FrustrationEngagementWeight;
                float frustrationWeight = FrustrationEfficiencyWeight
                    + (hasTempoData ? FrustrationTempoWeight : 0f)
                    + FrustrationEngagementWeight;
                frustrationScore /= frustrationWeight;
                if (frustrationScore > frustrationThreshold)
                    return FlowState.Frustration;
            }

            if (hasTempoData && efficiency > boredomEff && tempo > boredomTempo)
                return FlowState.Boredom;

            if (hasTempoData && efficiency < anxietyEff && tempo < anxietyTempo)
                return FlowState.Anxiety;

            return FlowState.Flow;
        }
    }
}
