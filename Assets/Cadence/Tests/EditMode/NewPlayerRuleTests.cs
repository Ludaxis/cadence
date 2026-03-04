using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class NewPlayerRuleTests
    {
        [Test]
        public void IsApplicable_NewPlayer_ReturnsTrue()
        {
            var rule = new Cadence.Rules.NewPlayerRule(null);
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 2 }
            };

            Assert.IsTrue(rule.IsApplicable(context));
        }

        [Test]
        public void IsApplicable_VeteranPlayer_ReturnsFalse()
        {
            var rule = new Cadence.Rules.NewPlayerRule(null);
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 10 }
            };

            Assert.IsFalse(rule.IsApplicable(context));
        }

        [Test]
        public void Evaluate_NewLosingPlayer_EasesDifficulty()
        {
            var rule = new Cadence.Rules.NewPlayerRule(null);
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 1, AverageOutcome = 0.3f },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } }
            };
            var proposal = new AdjustmentProposal();

            rule.Evaluate(context, proposal);

            Assert.Greater(proposal.Deltas.Count, 0, "Should add easing deltas for new losing player");
            foreach (var d in proposal.Deltas)
            {
                Assert.Less(d.ProposedValue, d.CurrentValue, "Deltas should ease (reduce) difficulty");
                Assert.AreEqual("NewPlayer", d.RuleName);
            }
        }

        [Test]
        public void Evaluate_NewWinningPlayer_NoEasing()
        {
            var rule = new Cadence.Rules.NewPlayerRule(null);
            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 2, AverageOutcome = 0.7f },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } }
            };
            var proposal = new AdjustmentProposal();

            rule.Evaluate(context, proposal);

            Assert.AreEqual(0, proposal.Deltas.Count, "Winning new player should not get easing");
        }

        [Test]
        public void Evaluate_EasingDecreasesWithExperience()
        {
            var rule = new Cadence.Rules.NewPlayerRule(null);

            // Session 0 (brand new)
            var context0 = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 0, AverageOutcome = 0f },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } }
            };
            var proposal0 = new AdjustmentProposal();
            rule.Evaluate(context0, proposal0);

            // Session 4 (almost veteran)
            var context4 = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 4, AverageOutcome = 0.3f },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } }
            };
            var proposal4 = new AdjustmentProposal();
            rule.Evaluate(context4, proposal4);

            Assert.Greater(proposal0.Deltas.Count, 0);
            Assert.Greater(proposal4.Deltas.Count, 0);

            // Session 0 should ease more than session 4
            float ease0 = Mathf.Abs(proposal0.Deltas[0].Delta);
            float ease4 = Mathf.Abs(proposal4.Deltas[0].Delta);
            Assert.Greater(ease0, ease4, "Brand new player should get more easing than near-veteran");
        }
    }
}
