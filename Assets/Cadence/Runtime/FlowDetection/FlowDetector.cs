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
            float alpha = _config != null ? _config.ExponentialAlpha : 0.3f;
            float rawTempo = _tempoWindow.ConsistencyScore();
            float rawEfficiency = _efficiencyWindow.Count > 0 ? _efficiencyWindow.Mean : 0.5f;
            float rawEngagement = ComputeEngagement(recentSignals);

            _smoothedTempo = Mathf.Lerp(_smoothedTempo, rawTempo, alpha);
            _smoothedEfficiency = Mathf.Lerp(_smoothedEfficiency, rawEfficiency, alpha);
            _smoothedEngagement = Mathf.Lerp(_smoothedEngagement, rawEngagement, alpha);

            // Classify
            int warmup = _config != null ? _config.WarmupMoves : 5;
            FlowState classified = _movesSeen < warmup
                ? FlowState.Unknown
                : Classify(_smoothedTempo, _smoothedEfficiency, _smoothedEngagement);

            // Hysteresis
            int hysteresis = _config != null ? _config.HysteresisCount : 3;
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
                    if (_lastMoveTime >= 0f)
                    {
                        float interval = entry.Timestamp.SessionTime - _lastMoveTime;
                        _tempoWindow.Push(interval);
                    }
                    _lastMoveTime = entry.Timestamp.SessionTime;
                    break;

                case SignalKeys.MoveOptimal:
                    _efficiencyWindow.Push(entry.Value);
                    break;

                case SignalKeys.PauseTriggered:
                case SignalKeys.InputRejected:
                    _engagementWindow.Push(0f);
                    break;

                case SignalKeys.ProgressDelta:
                    if (entry.Value > 0f)
                        _engagementWindow.Push(1f);
                    break;
            }
        }

        private float ComputeEngagement(SignalRingBuffer recentSignals)
        {
            if (_engagementWindow.Count == 0) return 0.5f;
            return _engagementWindow.Mean;
        }

        private FlowState Classify(float tempo, float efficiency, float engagement)
        {
            float boredomEff = _config != null ? _config.BoredomEfficiencyMin : 0.85f;
            float boredomTempo = _config != null ? _config.BoredomTempoMin : 0.7f;
            float anxietyEff = _config != null ? _config.AnxietyEfficiencyMax : 0.3f;
            float anxietyTempo = _config != null ? _config.AnxietyTempoMax : 0.2f;
            float frustrationThreshold = _config != null ? _config.FrustrationThreshold : 0.7f;

            // Compute frustration signal from low efficiency + erratic tempo
            float frustrationScore = (1f - efficiency) * FrustrationEfficiencyWeight
                + (1f - tempo) * FrustrationTempoWeight
                + (1f - engagement) * FrustrationEngagementWeight;
            if (frustrationScore > frustrationThreshold)
                return FlowState.Frustration;

            if (efficiency > boredomEff && tempo > boredomTempo)
                return FlowState.Boredom;

            if (efficiency < anxietyEff && tempo < anxietyTempo)
                return FlowState.Anxiety;

            return FlowState.Flow;
        }
    }
}
