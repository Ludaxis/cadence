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
            var proposal = _service.GetProposal(nextParams, LevelType.Standard, 1);
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

        [Test]
        public void LevelAbandonedSignal_CoercesOutcomeToAbandoned()
        {
            var levelParams = new Dictionary<string, float> { { "move_limit", 25f } };
            _service.BeginSession("level_abandon", levelParams, LevelType.MoveLimited);
            _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            _service.RecordSignal(SignalKeys.LevelAbandoned, 1f, SignalTier.RetryMeta);

            _service.EndSession(SessionOutcome.Win);

            var snapshot = _service.GetDebugSnapshot();
            Assert.AreEqual(SessionOutcome.Abandoned, snapshot.LastSessionSummary.Outcome);
        }

        [Test]
        public void ExplicitProposalOverload_UsesProvidedNextLevelType()
        {
            var service = CreateServiceForFatigueTests();

            for (int i = 0; i < 8; i++)
            {
                service.BeginSession($"mixed_type_{i}",
                    new Dictionary<string, float> { { "move_limit", 30f } },
                    LevelType.MoveLimited);
                service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
                service.RecordSignal(SignalKeys.MoveOptimal, i % 2 == 0 ? 1f : 0f,
                    SignalTier.DecisionQuality, 0);
                service.EndSession(i % 2 == 0 ? SessionOutcome.Win : SessionOutcome.Lose);
            }

            var proposal = service.GetProposal(
                new Dictionary<string, float> { { "time_limit", 30f } },
                LevelType.TimeLimited,
                8);

            Assert.IsNotNull(proposal);
            Assert.AreEqual(31.5f, GetProposedValue(proposal, "time_limit"), 0.01f);
        }

        [Test]
        public void SessionFatigue_DefaultRuleTriggersAfterEightCompletedLevels()
        {
            var service = CreateServiceForFatigueTests();

            for (int i = 0; i < 8; i++)
            {
                service.BeginSession($"fatigue_{i}",
                    new Dictionary<string, float> { { "move_limit", 30f } },
                    LevelType.MoveLimited);
                service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
                service.RecordSignal(SignalKeys.MoveOptimal, i % 2 == 0 ? 1f : 0f,
                    SignalTier.DecisionQuality, 0);
                service.EndSession(i % 2 == 0 ? SessionOutcome.Win : SessionOutcome.Lose);
            }

            var proposal = service.GetProposal(
                new Dictionary<string, float> { { "move_limit", 30f } },
                LevelType.MoveLimited,
                8);

            Assert.IsNotNull(proposal);
            Assert.IsTrue(ContainsRule(proposal, "SessionFatigue"));

            var snapshot = service.GetDebugSnapshot();
            Assert.AreEqual(8, snapshot.LevelsThisSession);
            Assert.IsTrue(snapshot.SessionFatigueActive);
        }

        [UnityTest]
        public IEnumerator SessionFatigue_ResetsAfterIdleGap()
        {
            var config = CreateBaseConfig();
            config.AdjustmentEngineConfig.TargetWinRateMin = 0f;
            config.AdjustmentEngineConfig.TargetWinRateMax = 1f;
            config.AdjustmentEngineConfig.LossStreakThreshold = 99;
            config.AdjustmentEngineConfig.WinStreakThreshold = 99;
            config.AdjustmentEngineConfig.FrustrationReliefThreshold = 1f;
            config.AdjustmentEngineConfig.SessionFatigueThresholdLevels = 1;
            config.AdjustmentEngineConfig.SessionFatigueEasePerLevel = 0.05f;
            config.AdjustmentEngineConfig.SessionFatigueMaxEase = 0.10f;
            config.AdjustmentEngineConfig.SessionFatigueResetGapMinutes = 0.0001f;

            var service = new DDAService(config);

            service.BeginSession("fatigue_reset_1",
                new Dictionary<string, float> { { "move_limit", 30f } },
                LevelType.MoveLimited);
            service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            service.EndSession(SessionOutcome.Win);

            var firstProposal = service.GetProposal(
                new Dictionary<string, float> { { "move_limit", 30f } },
                LevelType.MoveLimited,
                1);
            float firstMoveLimit = GetProposedValue(firstProposal, "move_limit");
            Assert.AreEqual(31.5f, firstMoveLimit, 0.01f);

            yield return new WaitForSecondsRealtime(0.02f);

            service.BeginSession("fatigue_reset_2",
                new Dictionary<string, float> { { "move_limit", 30f } },
                LevelType.MoveLimited);
            service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            service.EndSession(SessionOutcome.Win);

            var secondProposal = service.GetProposal(
                new Dictionary<string, float> { { "move_limit", 30f } },
                LevelType.MoveLimited,
                2);
            float secondMoveLimit = GetProposedValue(secondProposal, "move_limit");

            Assert.AreEqual(31.5f, secondMoveLimit, 0.01f);
            Assert.AreEqual(1, service.GetDebugSnapshot().LevelsThisSession);
        }

        [Test]
        public void ReplaySession_SkipsGlickoUpdate()
        {
            var levelParams = new Dictionary<string, float> { { "difficulty", 100f } };

            // Normal session first
            _service.BeginSession("level_1", levelParams);
            _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            _service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            _service.EndSession(SessionOutcome.Win);
            Assert.AreEqual(1, _service.PlayerProfile.SessionsCompleted);

            // Replay session
            _service.BeginSession("level_1_replay", levelParams);
            _service.RecordSignal(SignalKeys.PlayType, SignalKeys.PlayTypeReplay, SignalTier.RetryMeta);
            _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            _service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            _service.EndSession(SessionOutcome.Win);

            Assert.AreEqual(1, _service.PlayerProfile.SessionsCompleted,
                "Replay session should not increment SessionsCompleted");
        }

        [Test]
        public void ReplaySession_ReturnsEmptyProposal()
        {
            var levelParams = new Dictionary<string, float> { { "difficulty", 100f } };

            // Replay session
            _service.BeginSession("level_replay", levelParams);
            _service.RecordSignal(SignalKeys.PlayType, SignalKeys.PlayTypeReplay, SignalTier.RetryMeta);
            _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            _service.EndSession(SessionOutcome.Win);

            var proposal = _service.GetProposal(
                new Dictionary<string, float> { { "difficulty", 100f } },
                LevelType.Standard, 1);

            Assert.IsNotNull(proposal);
            Assert.IsTrue(proposal.Deltas == null || proposal.Deltas.Count == 0,
                "Replay session should return empty proposal with no deltas");
        }

        [Test]
        public void ParMoves_FlowsToSkillIndex()
        {
            var levelParams = new Dictionary<string, float>
            {
                { "difficulty", 100f },
                { "par_moves", 20f }
            };

            _service.BeginSession("level_par", levelParams);

            // Record exactly 20 moves (par)
            for (int i = 0; i < 20; i++)
            {
                _service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, i);
                _service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, i);
            }

            _service.EndSession(SessionOutcome.Win);

            var debug = _service.GetDebugSnapshot();
            Assert.AreEqual(20f, debug.ParMoves, 0.01f);
            Assert.AreEqual(1.0f, debug.SkillIndex, 0.01f);
            Assert.AreEqual(1.0f, debug.LastSessionSummary.SkillIndex, 0.01f);
            Assert.IsTrue(debug.LastSessionSummary.HasSkillIndex);
        }

        private static DDAService CreateServiceForFatigueTests()
        {
            var config = CreateBaseConfig();
            config.AdjustmentEngineConfig.TargetWinRateMin = 0f;
            config.AdjustmentEngineConfig.TargetWinRateMax = 1f;
            config.AdjustmentEngineConfig.LossStreakThreshold = 99;
            config.AdjustmentEngineConfig.WinStreakThreshold = 99;
            config.AdjustmentEngineConfig.FrustrationReliefThreshold = 1f;
            return new DDAService(config);
        }

        private static DDAConfig CreateBaseConfig()
        {
            var config = ScriptableObject.CreateInstance<DDAConfig>();
            config.RingBufferCapacity = 256;
            config.EnableSignalStorage = false;
            config.EnableMidSessionDetection = true;
            config.EnableBetweenSessionAdjustment = true;

            config.FlowDetectorConfig = ScriptableObject.CreateInstance<FlowDetectorConfig>();
            config.FlowDetectorConfig.WarmupMoves = 3;
            config.FlowDetectorConfig.HysteresisCount = 2;

            config.PlayerModelConfig = ScriptableObject.CreateInstance<PlayerModelConfig>();
            config.AdjustmentEngineConfig = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.AdjustmentEngineConfig.GlobalCooldownSeconds = 0f;
            config.AdjustmentEngineConfig.PerParameterCooldownSeconds = 0f;
            return config;
        }

        private static bool ContainsRule(AdjustmentProposal proposal, string ruleName)
        {
            if (proposal?.Deltas == null) return false;
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                if (proposal.Deltas[i].RuleName == ruleName)
                    return true;
            }

            return false;
        }

        private static float GetProposedValue(AdjustmentProposal proposal, string parameterKey)
        {
            Assert.IsNotNull(proposal);
            Assert.IsNotNull(proposal.Deltas);
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                if (proposal.Deltas[i].ParameterKey == parameterKey)
                    return proposal.Deltas[i].ProposedValue;
            }

            Assert.Fail($"No delta found for {parameterKey}");
            return 0f;
        }
    }
}
