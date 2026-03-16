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
            _config = TestFixtureHelper.CreateDefaultConfig();
            _config.MaxDeltaPerAdjustment = 0.50f;

            _engine = new AdjustmentEngine(_config);
        }

        [Test]
        public void Tutorial_ReturnsEmptyProposals()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.Tutorial);
            Assert.IsFalse(typeConfig.DDAEnabled);

            var context = TestFixtureHelper.CreateContext(10, 0.1f, TestFixtureHelper.CreateHistory(10, 0.1f), LevelType.Tutorial);
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
            var context = TestFixtureHelper.CreateContext(10, 0.85f, TestFixtureHelper.CreateHistory(10, 0.85f), LevelType.Breather);
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
            var bossContext = TestFixtureHelper.CreateContext(10, 0.1f, TestFixtureHelper.CreateHistory(10, 0.1f), LevelType.Boss);
            var bossProposal = _engine.Evaluate(bossContext);

            // Standard for comparison
            var stdContext = TestFixtureHelper.CreateContext(10, 0.1f, TestFixtureHelper.CreateHistory(10, 0.1f), LevelType.Standard);
            var stdProposal = _engine.Evaluate(stdContext);

            // Both should have FlowChannel deltas, Boss should be smaller
            float bossDelta = TestFixtureHelper.SumDeltasByRule(bossProposal, "FlowChannel");
            float stdDelta = TestFixtureHelper.SumDeltasByRule(stdProposal, "FlowChannel");

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
            Assert.IsTrue(typeConfig.TryGetParameterSemantics("move_limit", out var moveLimitSemantics));
            Assert.AreEqual(ParameterPolarity.HigherIsEasier, moveLimitSemantics.Polarity);

            var context = TestFixtureHelper.CreateContext(10, 0.1f, TestFixtureHelper.CreateHistory(10, 0.1f), LevelType.MoveLimited);
            var proposal = _engine.Evaluate(context);

            // Find FlowChannel deltas
            float moveLimitDelta = 0f;
            float difficultyDelta = 0f;
            float moveLimitProposed = 0f;
            float moveLimitCurrent = 0f;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName != "FlowChannel") continue;
                if (d.ParameterKey == "move_limit") moveLimitDelta = Mathf.Abs(d.Delta);
                if (d.ParameterKey == "move_limit")
                {
                    moveLimitCurrent = d.CurrentValue;
                    moveLimitProposed = d.ProposedValue;
                }
                if (d.ParameterKey == "difficulty") difficultyDelta = Mathf.Abs(d.Delta);
            }

            if (moveLimitCurrent > 0f)
            {
                Assert.Greater(moveLimitProposed, moveLimitCurrent,
                    "Low win rate should ease move-limited levels by increasing move_limit");
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
        public void TimeLimited_DefaultsUseHigherIsEasierPolarity()
        {
            var typeConfig = LevelTypeDefaults.GetDefaults(LevelType.TimeLimited);

            Assert.IsTrue(typeConfig.TryGetParameterSemantics("time_limit", out var semantics));
            Assert.AreEqual(ParameterPolarity.HigherIsEasier, semantics.Polarity);
            Assert.IsTrue(semantics.HasMinValue);
            Assert.AreEqual(1f, semantics.MinValue, 0.001f);
        }

        [Test]
        public void BreatherWinStreak_NoHardening()
        {
            var history = new List<SessionHistoryEntry>();
            for (int i = 0; i < 6; i++)
                history.Add(new SessionHistoryEntry { Outcome = 1f });

            var context = TestFixtureHelper.CreateContext(6, 1.0f, history, LevelType.Breather);
            var proposal = _engine.Evaluate(context);

            // StreakDamper should NOT harden for Breather
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "StreakDamper")
                    Assert.Less(d.ProposedValue, d.CurrentValue + 0.001f,
                        "StreakDamper should not harden Breather levels");
            }
        }

    }
}
