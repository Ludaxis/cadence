using System.Collections.Generic;
using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class LevelTypeTests
    {
        private AdjustmentEngine _engine;
        private AdjustmentEngineConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            _config.TargetWinRateMin = 0.3f;
            _config.TargetWinRateMax = 0.7f;
            _config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _config.LossStreakThreshold = 3;
            _config.WinStreakThreshold = 5;
            _config.LossStreakEaseAmount = 0.10f;
            _config.WinStreakHardenAmount = 0.05f;
            _config.FrustrationReliefThreshold = 0.7f;
            _config.GlobalCooldownSeconds = 0f;
            _config.PerParameterCooldownSeconds = 0f;
            _config.MaxDeltaPerAdjustment = 0.50f;

            _engine = new AdjustmentEngine(_config);
        }

        [Test]
        public void Tutorial_ReturnsEmptyProposals()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.Tutorial);
            Assert.IsFalse(typeConfig.DDAEnabled);

            var context = CreateContext(10, 0.1f, LevelType.Tutorial);
            var proposal = _engine.Evaluate(context);

            // All rules should be skipped since DDAEnabled=false
            Assert.AreEqual(0, proposal.Deltas.Count);
        }

        [Test]
        public void Breather_NoUpwardAdjustment()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.Breather);
            Assert.IsFalse(typeConfig.AllowUpwardAdjustment);

            // Player winning too much — normally would harden
            var context = CreateContext(10, 0.85f, LevelType.Breather);
            var proposal = _engine.Evaluate(context);

            // FlowChannel should NOT add upward deltas
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "FlowChannel")
                    Assert.Fail("FlowChannel should not produce upward deltas for Breather");
            }
        }

        [Test]
        public void Boss_ScalesAdjustmentBy03()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.Boss);
            Assert.AreEqual(0.3f, typeConfig.AdjustmentScale, 0.001f);

            // Low win rate — should ease, but at 0.3x scale
            var bossContext = CreateContext(10, 0.1f, LevelType.Boss);
            var bossProposal = _engine.Evaluate(bossContext);

            // Standard for comparison
            var stdContext = CreateContext(10, 0.1f, LevelType.Standard);
            var stdProposal = _engine.Evaluate(stdContext);

            // Both should have FlowChannel deltas, Boss should be smaller
            float bossDelta = SumDeltasByRule(bossProposal, "FlowChannel");
            float stdDelta = SumDeltasByRule(stdProposal, "FlowChannel");

            // Boss delta magnitude should be roughly 0.3x of standard
            if (stdDelta != 0f)
            {
                float ratio = Mathf.Abs(bossDelta) / Mathf.Abs(stdDelta);
                Assert.Less(ratio, 0.5f, "Boss adjustment should be significantly smaller than Standard");
            }
        }

        [Test]
        public void MoveLimited_PrioritizesMoveLimit()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.MoveLimited);
            Assert.AreEqual("move_limit", typeConfig.PrimaryParameterKey);

            var context = CreateContext(10, 0.1f, LevelType.MoveLimited);
            var proposal = _engine.Evaluate(context);

            // Find FlowChannel deltas
            float moveLimitDelta = 0f;
            float difficultyDelta = 0f;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName != "FlowChannel") continue;
                if (d.ParameterKey == "move_limit") moveLimitDelta = Mathf.Abs(d.Delta);
                if (d.ParameterKey == "difficulty") difficultyDelta = Mathf.Abs(d.Delta);
            }

            // move_limit should get full magnitude, difficulty should get half
            if (moveLimitDelta > 0f && difficultyDelta > 0f)
            {
                // Normalize by current value to compare proportionally
                float moveLimitPct = moveLimitDelta / 30f;
                float difficultyPct = difficultyDelta / 100f;
                Assert.Greater(moveLimitPct, difficultyPct,
                    "move_limit should be adjusted more proportionally than difficulty");
            }
        }

        [Test]
        public void DefaultStandard_BackwardCompatible()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.Standard);
            Assert.AreEqual(1f, typeConfig.AdjustmentScale, 0.001f);
            Assert.IsTrue(typeConfig.AllowUpwardAdjustment);
            Assert.IsTrue(typeConfig.DDAEnabled);
            Assert.IsNull(typeConfig.PrimaryParameterKey);
        }

        [Test]
        public void LevelTypeDefaults_AllTypesReturnValidConfig()
        {
            var types = new[]
            {
                LevelType.Standard, LevelType.MoveLimited, LevelType.TimeLimited,
                LevelType.GoalCollection, LevelType.Boss, LevelType.Breather,
                LevelType.Tutorial
            };

            foreach (var t in types)
            {
                var config = LevelTypeDefaults.GetDefaults(t);
                Assert.IsNotNull(config, $"GetDefaults({t}) returned null");
                Assert.AreEqual(t, config.Type, $"Config Type mismatch for {t}");
                Assert.GreaterOrEqual(config.AdjustmentScale, 0f,
                    $"AdjustmentScale negative for {t}");
            }
        }

        [Test]
        public void BreatherWinStreak_NoHardening()
        {
            var history = new List<SessionHistoryEntry>();
            for (int i = 0; i < 6; i++)
                history.Add(new SessionHistoryEntry { Outcome = 1f });

            var context = CreateContext(6, 1.0f, LevelType.Breather, history);
            var proposal = _engine.Evaluate(context);

            // StreakDamper should NOT harden for Breather
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "StreakDamper")
                    Assert.Less(d.ProposedValue, d.CurrentValue + 0.001f,
                        "StreakDamper should not harden Breather levels");
            }
        }

        // --- Helpers ---

        private AdjustmentContext CreateContext(int sessionsCompleted, float averageOutcome,
            LevelType levelType, List<SessionHistoryEntry> history = null)
        {
            if (history == null)
                history = CreateHistory(sessionsCompleted, averageOutcome);

            var typeConfig = LevelTypeDefaults.GetDefaults(levelType);

            return new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f,
                    Deviation = 100f,
                    SessionsCompleted = sessionsCompleted,
                    AverageOutcome = averageOutcome,
                    RecentHistory = history
                },
                LastSession = new SessionSummary
                {
                    Outcome = averageOutcome > 0.5f ? SessionOutcome.Win : SessionOutcome.Lose,
                    MoveEfficiency = averageOutcome
                },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float>
                {
                    { "difficulty", 100f },
                    { "move_limit", 30f }
                },
                RecentHistory = history,
                TimeSinceLastAdjustment = 1000f,
                LevelType = levelType,
                LevelTypeConfig = typeConfig
            };
        }

        private static List<SessionHistoryEntry> CreateHistory(int count, float winRate)
        {
            var history = new List<SessionHistoryEntry>();
            int wins = Mathf.RoundToInt(count * winRate);
            for (int i = 0; i < count; i++)
            {
                history.Add(new SessionHistoryEntry
                {
                    Outcome = i < wins ? 1f : 0f,
                    Efficiency = 0.5f
                });
            }
            return history;
        }

        private static float SumDeltasByRule(AdjustmentProposal proposal, string ruleName)
        {
            float sum = 0f;
            foreach (var d in proposal.Deltas)
                if (d.RuleName == ruleName) sum += d.Delta;
            return sum;
        }
    }
}
