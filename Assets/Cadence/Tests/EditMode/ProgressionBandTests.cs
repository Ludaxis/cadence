using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class ProgressionBandTests
    {
        [Test]
        public void FlowChannel_FlatBands_UsesConfigValues()
        {
            var config = TestFixtureHelper.CreateDefaultConfig();
            config.MaxDeltaPerAdjustment = 0.5f;
            // Leave curves empty (default)

            var rule = new Cadence.Rules.FlowChannelRule(config);
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    SessionsCompleted = 10,
                    AverageOutcome = 0.2f, // Below 0.3 min
                    RecentHistory = new List<SessionHistoryEntry>()
                },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                LastFlowReading = new FlowReading { State = FlowState.Flow }
            };

            var proposal = new AdjustmentProposal();
            rule.Evaluate(context, proposal);

            Assert.Greater(proposal.Deltas.Count, 0, "Should ease when below flat min band");
        }

        [Test]
        public void FlowChannel_CurveBands_OverridesFlat()
        {
            var config = TestFixtureHelper.CreateDefaultConfig();
            config.MaxDeltaPerAdjustment = 0.5f;

            // Set curves that widen the band to 0.1 - 0.9 at session 10
            config.TargetWinRateMinCurve = AnimationCurve.Linear(0f, 0.3f, 20f, 0.1f);
            config.TargetWinRateMaxCurve = AnimationCurve.Linear(0f, 0.7f, 20f, 0.9f);

            var rule = new Cadence.Rules.FlowChannelRule(config);

            // At session 10, curve min should be ~0.2, so 0.25 should be IN band
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    SessionsCompleted = 10,
                    AverageOutcome = 0.25f, // Would be below flat 0.3 but above curve ~0.2
                    RecentHistory = new List<SessionHistoryEntry>()
                },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                LastFlowReading = new FlowReading { State = FlowState.Flow }
            };

            var proposal = new AdjustmentProposal();
            rule.Evaluate(context, proposal);

            Assert.AreEqual(0, proposal.Deltas.Count,
                "With curve bands, 0.25 should be in-band at session 10 (curve min ~0.2)");
        }
    }
}
