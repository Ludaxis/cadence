using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cadence;

namespace Cadence.Tests
{
    [TestFixture]
    public class DDAServiceIntegrationTests
    {
        private DDAService _service;

        [SetUp]
        public void SetUp()
        {
            var config = ScriptableObject.CreateInstance<DDAConfig>();
            config.RingBufferCapacity = 256;
            config.EnableSignalStorage = false; // No file I/O in tests
            config.EnableMidSessionDetection = true;
            config.EnableBetweenSessionAdjustment = true;

            config.FlowDetectorConfig = ScriptableObject.CreateInstance<FlowDetectorConfig>();
            config.FlowDetectorConfig.WarmupMoves = 3;
            config.FlowDetectorConfig.HysteresisCount = 2;

            config.PlayerModelConfig = ScriptableObject.CreateInstance<PlayerModelConfig>();
            config.AdjustmentEngineConfig = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.AdjustmentEngineConfig.GlobalCooldownSeconds = 0f;
            config.AdjustmentEngineConfig.PerParameterCooldownSeconds = 0f;

            _service = new DDAService(config);
        }

        [Test]
        public void FullLifecycle_BeginRecordEndPropose()
        {
            var levelParams = new Dictionary<string, float>
            {
                { "difficulty", 100f },
                { "move_limit", 25f }
            };

            // Begin session
            _service.BeginSession("level_1", levelParams);
            Assert.IsTrue(_service.IsSessionActive);

            // Record signals for a winning session
            for (int i = 0; i < 20; i++)
            {
                _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, i);
                _service.RecordSignal(SignalKeys.MoveOptimal, i < 14 ? 1f : 0f, SignalTier.DecisionQuality, i);
                _service.RecordSignal(SignalKeys.ProgressDelta, 0.05f, SignalTier.DecisionQuality);
                _service.Tick(0.5f); // Simulate half second per move
            }

            // End session
            _service.EndSession(SessionOutcome.Win);
            Assert.IsFalse(_service.IsSessionActive);

            // Check player profile updated
            Assert.AreEqual(1, _service.PlayerProfile.SessionsCompleted);
            Assert.Greater(_service.PlayerProfile.Rating, 1500f); // Won → rating increased

            // Get proposal for next level
            var nextParams = new Dictionary<string, float>
            {
                { "difficulty", 110f },
                { "move_limit", 23f }
            };
            var proposal = _service.GetProposal(nextParams);
            Assert.IsNotNull(proposal);
        }

        [Test]
        public void MultipleSessionsUpdateProfile()
        {
            for (int session = 0; session < 5; session++)
            {
                var @params = new Dictionary<string, float> { { "difficulty", 100f } };
                _service.BeginSession($"level_{session}", @params);

                for (int i = 0; i < 10; i++)
                {
                    _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, i);
                    _service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, i);
                    _service.Tick(0.3f);
                }

                _service.EndSession(SessionOutcome.Win);
            }

            Assert.AreEqual(5, _service.PlayerProfile.SessionsCompleted);
            Assert.AreEqual(5, _service.PlayerProfile.RecentHistory.Count);
            Assert.IsTrue(_service.PlayerProfile.HasSufficientData);
        }

        [Test]
        public void DebugSnapshot_ContainsAllData()
        {
            var @params = new Dictionary<string, float> { { "difficulty", 100f } };
            _service.BeginSession("test_level", @params);

            _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            _service.Tick(0.016f);

            var snapshot = _service.GetDebugSnapshot();

            Assert.IsNotNull(snapshot);
            Assert.IsTrue(snapshot.SessionActive);
            Assert.AreEqual("test_level", snapshot.CurrentLevelId);
            Assert.Greater(snapshot.TotalSignals, 0);
            Assert.IsNotNull(snapshot.Profile);
        }

        [Test]
        public void ProfilePersistence_SaveAndLoad()
        {
            var @params = new Dictionary<string, float> { { "difficulty", 100f } };
            _service.BeginSession("level_1", @params);
            for (int i = 0; i < 10; i++)
            {
                _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, i);
                _service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, i);
            }
            _service.EndSession(SessionOutcome.Win);

            // Save
            string json = _service.SaveProfile();
            Assert.IsFalse(string.IsNullOrEmpty(json));

            // Create new service and load
            var config = ScriptableObject.CreateInstance<DDAConfig>();
            config.EnableSignalStorage = false;
            config.PlayerModelConfig = ScriptableObject.CreateInstance<PlayerModelConfig>();
            config.FlowDetectorConfig = ScriptableObject.CreateInstance<FlowDetectorConfig>();
            config.AdjustmentEngineConfig = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();

            var service2 = new DDAService(config);
            service2.LoadProfile(json);

            Assert.AreEqual(
                _service.PlayerProfile.Rating,
                service2.PlayerProfile.Rating,
                0.01f
            );
            Assert.AreEqual(
                _service.PlayerProfile.SessionsCompleted,
                service2.PlayerProfile.SessionsCompleted
            );
        }

        [Test]
        public void BeginSessionWhileActive_AutoEnds()
        {
            var @params = new Dictionary<string, float> { { "difficulty", 100f } };
            _service.BeginSession("level_1", @params);

            // Start new session without ending — should auto-end as Abandoned
            _service.BeginSession("level_2", @params);

            Assert.IsTrue(_service.IsSessionActive);
            Assert.AreEqual(1, _service.PlayerProfile.SessionsCompleted);
        }

        [Test]
        public void RecordSignal_WhileInactive_Ignored()
        {
            // No active session
            _service.RecordSignal(SignalKeys.MoveExecuted, 1f);

            var snapshot = _service.GetDebugSnapshot();
            Assert.AreEqual(0, snapshot.TotalSignals);
        }
    }
}
