using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class CooldownIntegrationTests
    {
        [Test]
        public void RecordAdjustment_BlocksRepeatProposal()
        {
            var config = TestFixtureHelper.CreateDefaultConfig();
            config.GlobalCooldownSeconds = 60f;
            config.PerParameterCooldownSeconds = 120f;

            var engine = new AdjustmentEngine(config);

            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f, Deviation = 100f,
                    SessionsCompleted = 10, AverageOutcome = 0.15f,
                    RecentHistory = TestFixtureHelper.CreateLossHistory(4)
                },
                LastSession = new SessionSummary { Outcome = SessionOutcome.Lose },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                RecentHistory = TestFixtureHelper.CreateLossHistory(4),
                TimeSinceLastAdjustment = 1000f
            };

            // First proposal should have deltas
            var first = engine.Evaluate(context);
            Assert.Greater(first.Deltas.Count, 0, "First proposal should have deltas");

            // Record it
            engine.RecordAdjustment(first, 1000f);

            // Second proposal immediately after should be blocked by cooldown
            context.TimeSinceLastAdjustment = 1001f; // Only 1 second later
            var second = engine.Evaluate(context);
            Assert.AreEqual(0, second.Deltas.Count, "Second proposal should be blocked by cooldown");
        }

        [Test]
        public void Cooldown_Expires_AllowsNewProposal()
        {
            var config = TestFixtureHelper.CreateDefaultConfig();
            config.GlobalCooldownSeconds = 10f;
            config.PerParameterCooldownSeconds = 10f;

            var engine = new AdjustmentEngine(config);

            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f, Deviation = 100f,
                    SessionsCompleted = 10, AverageOutcome = 0.15f,
                    RecentHistory = TestFixtureHelper.CreateLossHistory(4)
                },
                LastSession = new SessionSummary { Outcome = SessionOutcome.Lose },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                RecentHistory = TestFixtureHelper.CreateLossHistory(4),
                TimeSinceLastAdjustment = 100f
            };

            var first = engine.Evaluate(context);
            engine.RecordAdjustment(first, 100f);

            // After cooldown expires
            context.TimeSinceLastAdjustment = 200f; // Well past 10s cooldown
            var second = engine.Evaluate(context);
            Assert.Greater(second.Deltas.Count, 0, "After cooldown expires, proposals should work again");
        }

        [Test]
        public void PerParameterCooldown_BlocksOneParam_AllowsOther()
        {
            var config = TestFixtureHelper.CreateDefaultConfig();
            config.GlobalCooldownSeconds = 0f; // No global cooldown
            config.PerParameterCooldownSeconds = 100f; // Long per-param cooldown

            var engine = new AdjustmentEngine(config);

            // First proposal with "difficulty" only
            var context1 = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f, Deviation = 100f,
                    SessionsCompleted = 10, AverageOutcome = 0.15f,
                    RecentHistory = TestFixtureHelper.CreateLossHistory(4)
                },
                LastSession = new SessionSummary { Outcome = SessionOutcome.Lose },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                RecentHistory = TestFixtureHelper.CreateLossHistory(4),
                TimeSinceLastAdjustment = 1000f,
                LevelType = LevelType.Standard,
                LevelTypeConfig = LevelTypeDefaults.GetDefaults(LevelType.Standard)
            };

            var first = engine.Evaluate(context1);
            Assert.Greater(first.Deltas.Count, 0, "First proposal should have deltas");
            engine.RecordAdjustment(first, 1000f);

            // Second proposal immediately after, but with a NEW parameter "speed"
            var context2 = new AdjustmentContext
            {
                Profile = context1.Profile,
                LastSession = context1.LastSession,
                LastFlowReading = context1.LastFlowReading,
                LevelParameters = new Dictionary<string, float>
                {
                    { "difficulty", 100f },
                    { "speed", 50f }
                },
                RecentHistory = context1.RecentHistory,
                TimeSinceLastAdjustment = 1001f, // 1 second later
                LevelType = LevelType.Standard,
                LevelTypeConfig = LevelTypeDefaults.GetDefaults(LevelType.Standard)
            };

            var second = engine.Evaluate(context2);

            // "difficulty" should be on cooldown, but "speed" should be allowed
            bool hasDifficulty = false;
            bool hasSpeed = false;
            foreach (var d in second.Deltas)
            {
                if (d.ParameterKey == "difficulty") hasDifficulty = true;
                if (d.ParameterKey == "speed") hasSpeed = true;
            }

            Assert.IsFalse(hasDifficulty, "difficulty should be blocked by per-parameter cooldown");
            Assert.IsTrue(hasSpeed, "speed should NOT be blocked (not previously adjusted)");
        }

    }
}
