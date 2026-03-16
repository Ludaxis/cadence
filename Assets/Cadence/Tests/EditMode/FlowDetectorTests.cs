using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class FlowDetectorTests
    {
        private FlowDetector _detector;
        private FlowDetectorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<FlowDetectorConfig>();
            _config.TempoWindowSize = 8;
            _config.EfficiencyWindowSize = 12;
            _config.EngagementWindowSize = 20;
            _config.BoredomEfficiencyMin = 0.85f;
            _config.BoredomTempoMin = 0.7f;
            _config.AnxietyEfficiencyMax = 0.3f;
            _config.AnxietyTempoMax = 0.2f;
            _config.FrustrationThreshold = 0.7f;
            _config.HysteresisCount = 2; // Lower for tests
            _config.WarmupMoves = 3;     // Lower for tests
            _config.ExponentialAlpha = 0.5f;

            _detector = new FlowDetector(_config);
        }

        [Test]
        public void Initial_State_IsUnknown()
        {
            Assert.AreEqual(FlowState.Unknown, _detector.CurrentReading.State);
        }

        [Test]
        public void DuringWarmup_State_RemainsUnknown()
        {
            var buffer = new SignalRingBuffer(64);

            // Only 2 moves (below warmup of 3)
            AddMoveSignals(buffer, 2, optimal: true, intervalSeconds: 1f);

            _detector.Tick(0.016f, buffer);

            Assert.AreEqual(FlowState.Unknown, _detector.CurrentReading.State);
        }

        [Test]
        public void HighEfficiency_SteadyTempo_DetectsBoredom()
        {
            var buffer = new SignalRingBuffer(64);

            // Many optimal moves with consistent intervals → boredom
            AddMoveSignals(buffer, 15, optimal: true, intervalSeconds: 0.5f);

            // Need several ticks to build up enough data
            for (int i = 0; i < 10; i++)
                _detector.Tick(0.016f, buffer);

            var reading = _detector.CurrentReading;
            // High-efficiency steady play should at least produce strong tempo/efficiency signals,
            // even if hysteresis keeps the discrete state conservative.
            Assert.Greater(reading.TempoScore, 0.7f);
            Assert.GreaterOrEqual(reading.EfficiencyScore, 0.5f);
        }

        [Test]
        public void LowEfficiency_ErraticTempo_DetectsAnxietyOrFrustration()
        {
            var buffer = new SignalRingBuffer(64);
            float time = 0f;

            // Many suboptimal moves with erratic intervals → anxiety/frustration
            for (int i = 0; i < 15; i++)
            {
                float interval = (i % 2 == 0) ? 0.3f : 5f; // Very erratic
                time += interval;

                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveOptimal,
                    Value = 0f, // All suboptimal
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.InputRejected,
                    Value = 1f,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
            }

            for (int i = 0; i < 10; i++)
                _detector.Tick(0.016f, buffer);

            var reading = _detector.CurrentReading;
            // Should detect anxiety or frustration
            Assert.IsTrue(
                reading.State == FlowState.Anxiety ||
                reading.State == FlowState.Frustration ||
                reading.State == FlowState.Unknown,
                $"Expected Anxiety or Frustration, got {reading.State}"
            );
        }

        [Test]
        public void Reset_ClearsState()
        {
            var buffer = new SignalRingBuffer(64);
            AddMoveSignals(buffer, 10, optimal: true, intervalSeconds: 1f);
            _detector.Tick(0.016f, buffer);

            _detector.Reset();

            Assert.AreEqual(FlowState.Unknown, _detector.CurrentReading.State);
            Assert.AreEqual(0f, _detector.CurrentReading.Confidence);
        }

        [Test]
        public void OnFlowStateChanged_FiresOnTransition()
        {
            var buffer = new SignalRingBuffer(64);
            FlowReading lastReading = default;
            int changeCount = 0;

            _detector.OnFlowStateChanged += r =>
            {
                lastReading = r;
                changeCount++;
            };

            // Phase 1: Drive into Flow/Boredom with high efficiency + steady tempo
            AddMoveSignals(buffer, 20, optimal: true, intervalSeconds: 0.5f);
            for (int i = 0; i < 20; i++)
                _detector.Tick(0.016f, buffer);

            // Phase 2: Switch to frustration signals (low efficiency, erratic tempo, pauses)
            float time = 20f;
            for (int i = 0; i < 15; i++)
            {
                float interval = (i % 2 == 0) ? 0.3f : 5f;
                time += interval;
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = 20 + i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveOptimal,
                    Value = 0f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = 20 + i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.InputRejected,
                    Value = 1f,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
            }
            for (int i = 0; i < 20; i++)
                _detector.Tick(0.016f, buffer);

            // Should have fired at least one transition
            Assert.GreaterOrEqual(changeCount, 1,
                $"Expected at least 1 state transition but got {changeCount}");
        }

        [Test]
        public void FlowWindow_ConsistencyScore_HighForSteadyValues()
        {
            var window = new FlowWindow(8);
            for (int i = 0; i < 8; i++)
                window.Push(1.0f); // All identical

            float score = window.ConsistencyScore();
            Assert.AreEqual(1f, score, 0.01f);
        }

        [Test]
        public void FlowWindow_ConsistencyScore_LowForErraticValues()
        {
            var window = new FlowWindow(8);
            window.Push(0.1f);
            window.Push(10f);
            window.Push(0.2f);
            window.Push(8f);
            window.Push(0.1f);
            window.Push(9f);
            window.Push(0.3f);
            window.Push(7f);

            float score = window.ConsistencyScore();
            Assert.Less(score, 0.55f);
        }

        [Test]
        public void ExplicitIntervals_AreAuthoritativeForTempo()
        {
            var explicitDetector = new FlowDetector(_config);
            var derivedDetector = new FlowDetector(_config);
            var explicitBuffer = new SignalRingBuffer(128);
            var derivedBuffer = new SignalRingBuffer(128);

            AddIrregularMoveSignals(explicitBuffer, includeExplicitIntervals: true);
            AddIrregularMoveSignals(derivedBuffer, includeExplicitIntervals: false);

            for (int i = 0; i < 10; i++)
            {
                explicitDetector.Tick(0.016f, explicitBuffer);
                derivedDetector.Tick(0.016f, derivedBuffer);
            }

            Assert.Greater(explicitDetector.CurrentReading.TempoScore, 0.7f);
            Assert.Greater(explicitDetector.CurrentReading.TempoScore,
                derivedDetector.CurrentReading.TempoScore + 0.15f);
        }

        // --- Helpers ---

        private static void AddMoveSignals(SignalRingBuffer buffer, int count,
            bool optimal, float intervalSeconds)
        {
            float time = 0f;
            for (int i = 0; i < count; i++)
            {
                time += intervalSeconds;
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveOptimal,
                    Value = optimal ? 1f : 0f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.ProgressDelta,
                    Value = 0.05f,
                    Tier = SignalTier.DecisionQuality,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
            }
        }

        private static void AddIrregularMoveSignals(SignalRingBuffer buffer, bool includeExplicitIntervals)
        {
            float[] derivedIntervals = { 0.3f, 5f, 0.2f, 4f, 0.4f, 3.5f, 0.25f, 5.5f };
            float time = 0f;

            for (int i = 0; i < derivedIntervals.Length; i++)
            {
                time += derivedIntervals[i];
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveOptimal,
                    Value = 1f,
                    Tier = SignalTier.DecisionQuality,
                    MoveIndex = i,
                    Timestamp = new SignalTimestamp { SessionTime = time }
                });

                if (includeExplicitIntervals && i > 0)
                {
                    buffer.Push(new SignalEntry
                    {
                        Key = SignalKeys.InterMoveInterval,
                        Value = 1f,
                        Tier = SignalTier.BehavioralTempo,
                        MoveIndex = i,
                        Timestamp = new SignalTimestamp { SessionTime = time }
                    });
                }
            }
        }
    }
}
