using System.Collections.Generic;
using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class AdjustmentEngineTests
    {
        private AdjustmentEngine _engine;
        private AdjustmentEngineConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = TestFixtureHelper.CreateDefaultConfig();

            _engine = new AdjustmentEngine(_config);
        }

        [Test]
        public void Evaluate_NoDataProfile_ReturnsEmptyProposal()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 0,
                averageOutcome: 0f,
                recentHistory: new List<SessionHistoryEntry>()
            );

            var proposal = _engine.Evaluate(context);

            // FlowChannelRule not applicable (no sufficient data)
            // StreakDamperRule not applicable (no history)
            Assert.IsNotNull(proposal);
            Assert.AreEqual(AdjustmentRuleAttribution.NewPlayer, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_LowWinRate_ProposesEasier()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.15f, // Below 0.3 target
                recentHistory: CreateNoStreakHistory()
            );

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);
            Assert.AreEqual(AdjustmentRuleAttribution.FlowChannel, proposal.RuleFired);
            foreach (var delta in proposal.Deltas)
            {
                if (delta.RuleName == "FlowChannel")
                    TestFixtureHelper.AssertSemanticallyEasier(delta);
            }
        }

        [Test]
        public void Evaluate_HighWinRate_ProposesHarder()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.85f, // Above 0.7 target
                recentHistory: CreateNoStreakHistory()
            );

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);
            Assert.AreEqual(AdjustmentRuleAttribution.FlowChannel, proposal.RuleFired);
            foreach (var delta in proposal.Deltas)
            {
                if (delta.RuleName == "FlowChannel")
                    TestFixtureHelper.AssertSemanticallyHarder(delta);
            }
        }

        [Test]
        public void Evaluate_InFlowBand_NoFlowChannelDeltas()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f, // In band
                recentHistory: TestFixtureHelper.CreateHistory(10, winRate: 0.5f)
            );

            var proposal = _engine.Evaluate(context);

            int flowChannelDeltas = 0;
            foreach (var d in proposal.Deltas)
                if (d.RuleName == "FlowChannel") flowChannelDeltas++;

            Assert.AreEqual(0, flowChannelDeltas);
        }

        [Test]
        public void Evaluate_EarlySessionNoOtherRule_UsesGateBlockedAttribution()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 3,
                averageOutcome: 0.6f,
                recentHistory: CreateNoStreakHistory()
            );
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0f,
                Outcome = SessionOutcome.Win
            };

            var proposal = _engine.Evaluate(context);

            Assert.AreEqual(0, proposal.Deltas.Count);
            Assert.AreEqual(AdjustmentRuleAttribution.GateBlocked, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_LossStreak_ProposesEasier()
        {
            var history = new List<SessionHistoryEntry>();
            // 4 losses in a row (above threshold of 3)
            for (int i = 0; i < 4; i++)
                history.Add(new SessionHistoryEntry { Outcome = 0f });

            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 4,
                averageOutcome: 0.0f,
                recentHistory: history
            );

            var proposal = _engine.Evaluate(context);

            bool hasStreakDamper = false;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "StreakDamper")
                {
                    hasStreakDamper = true;
                    TestFixtureHelper.AssertSemanticallyEasier(d);
                }
            }
            Assert.IsTrue(hasStreakDamper);
            Assert.AreEqual(AdjustmentRuleAttribution.StreakDamper, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_WinStreak_ProposesHarder()
        {
            var history = new List<SessionHistoryEntry>();
            // 6 wins in a row (above threshold of 5)
            for (int i = 0; i < 6; i++)
                history.Add(new SessionHistoryEntry { Outcome = 1f });

            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 6,
                averageOutcome: 1.0f,
                recentHistory: history
            );

            var proposal = _engine.Evaluate(context);

            bool hasStreakDamper = false;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "StreakDamper")
                {
                    hasStreakDamper = true;
                    TestFixtureHelper.AssertSemanticallyHarder(d);
                }
            }
            Assert.IsTrue(hasStreakDamper);
            Assert.AreEqual(AdjustmentRuleAttribution.StreakDamper, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_HighFrustration_ProposesRelief()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 5,
                averageOutcome: 0.5f,
                recentHistory: CreateNoStreakHistory()
            );
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0.9f,
                Outcome = SessionOutcome.Lose
            };

            var proposal = _engine.Evaluate(context);

            bool hasRelief = false;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "FrustrationRelief")
                {
                    hasRelief = true;
                    TestFixtureHelper.AssertSemanticallyEasier(d);
                }
            }
            Assert.IsTrue(hasRelief);
            Assert.AreEqual(AdjustmentRuleAttribution.FrustrationRelief, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_NoRulesFire_UsesNoneAttribution()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f,
                recentHistory: new List<SessionHistoryEntry>
                {
                    new SessionHistoryEntry { Outcome = 1f },
                    new SessionHistoryEntry { Outcome = 0f },
                    new SessionHistoryEntry { Outcome = 1f },
                    new SessionHistoryEntry { Outcome = 0f },
                    new SessionHistoryEntry { Outcome = 1f },
                    new SessionHistoryEntry { Outcome = 0f }
                }
            );
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0f,
                Outcome = SessionOutcome.Win
            };

            var proposal = _engine.Evaluate(context);

            Assert.AreEqual(0, proposal.Deltas.Count);
            Assert.AreEqual(AdjustmentRuleAttribution.None, proposal.RuleFired);
        }

        [Test]
        public void Evaluate_DeltasClamped()
        {
            _config.MaxDeltaPerAdjustment = 0.05f; // Very tight clamp

            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.05f, // Far outside band
                recentHistory: TestFixtureHelper.CreateHistory(10, winRate: 0.05f)
            );

            var proposal = _engine.Evaluate(context);

            foreach (var d in proposal.Deltas)
            {
                float percentChange = Mathf.Abs(d.Delta / d.CurrentValue);
                Assert.LessOrEqual(percentChange, 0.051f,
                    $"Delta for {d.ParameterKey} exceeds max: {percentChange:P}");
            }
        }

        [Test]
        public void Evaluate_SawtoothHardening_DoesNotFlipEasingDirection()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.15f, // Should ease
                recentHistory: TestFixtureHelper.CreateHistory(10, winRate: 0.15f)
            );
            context.SawtoothMultiplier = 1.3f; // Hard cycle point

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);

            foreach (var d in proposal.Deltas)
            {
                TestFixtureHelper.AssertSemanticallyEasier(d,
                    $"Sawtooth must not flip easing delta for {d.ParameterKey}");
            }
        }

        [Test]
        public void Evaluate_FrustrationFlowState_TriggersMidSessionRelief()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f,
                recentHistory: CreateNoStreakHistory()
            );
            context.LastFlowReading = new FlowReading { State = FlowState.Frustration };
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0.9f,
                Outcome = SessionOutcome.Lose
            };

            var proposal = _engine.Evaluate(context);

            bool hasMidSession = false;
            foreach (var d in proposal.Deltas)
            {
                if (d.RuleName == "FrustrationRelief")
                {
                    hasMidSession = true;
                    break;
                }
            }
            Assert.IsTrue(hasMidSession, "Frustration flow state should trigger FrustrationRelief");
            Assert.AreEqual(AdjustmentTiming.MidSession, proposal.Timing,
                "Frustration flow state should set MidSession timing");
        }

        [Test]
        public void Evaluate_FrustrationFlowState_DoesNotTriggerRelief_WhenMidSessionDisabled()
        {
            _config.AllowFrustrationReliefMidSession = false;
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f,
                recentHistory: CreateNoStreakHistory()
            );
            context.LastFlowReading = new FlowReading { State = FlowState.Frustration };
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0.2f,
                Outcome = SessionOutcome.Lose
            };

            var proposal = _engine.Evaluate(context);

            Assert.IsFalse(ContainsRule(proposal, "FrustrationRelief"),
                "FlowState.Frustration should not trigger relief when mid-session relief is disabled");
            Assert.AreEqual(AdjustmentTiming.BeforeNextLevel, proposal.Timing,
                "Disabled mid-session relief must not force MidSession timing");
        }

        [Test]
        public void Evaluate_FrustrationScoreStillTriggersRelief_WhenMidSessionDisabled()
        {
            _config.AllowFrustrationReliefMidSession = false;
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f,
                recentHistory: CreateNoStreakHistory()
            );
            context.LastFlowReading = new FlowReading { State = FlowState.Frustration };
            context.LastSession = new SessionSummary
            {
                FrustrationScore = 0.9f,
                Outcome = SessionOutcome.Lose
            };

            var proposal = _engine.Evaluate(context);

            Assert.IsTrue(ContainsRule(proposal, "FrustrationRelief"),
                "FrustrationScore should still trigger between-session relief");
            Assert.AreEqual(AdjustmentTiming.BeforeNextLevel, proposal.Timing,
                "Score-triggered relief should stay between-session when mid-session relief is disabled");
        }

        [Test]
        public void Evaluate_BoredomFlowState_DoesNotBlockEasing()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.15f,
                recentHistory: TestFixtureHelper.CreateHistory(10, winRate: 0.15f)
            );
            context.LastFlowReading = new FlowReading { State = FlowState.Boredom };

            var proposal = _engine.Evaluate(context);

            Assert.Greater(proposal.Deltas.Count, 0,
                "Boredom flow state should not prevent FlowChannel easing when win rate is low");
        }

        [Test]
        public void Evaluate_AnxietyFlowState_DoesNotAlterFlowChannelBehavior()
        {
            var context = TestFixtureHelper.CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f,
                recentHistory: TestFixtureHelper.CreateHistory(10, winRate: 0.5f)
            );
            context.LastFlowReading = new FlowReading { State = FlowState.Anxiety };

            var proposal = _engine.Evaluate(context);

            // In-band win rate + Anxiety flow state: FlowChannel should not activate
            int flowChannelDeltas = 0;
            foreach (var d in proposal.Deltas)
                if (d.RuleName == "FlowChannel") flowChannelDeltas++;

            Assert.AreEqual(0, flowChannelDeltas,
                "Anxiety flow state should not cause FlowChannel to activate when win rate is in band");
        }

        private static List<SessionHistoryEntry> CreateNoStreakHistory()
        {
            return new List<SessionHistoryEntry>
            {
                new SessionHistoryEntry { Outcome = 1f, Efficiency = 0.5f },
                new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.5f },
                new SessionHistoryEntry { Outcome = 1f, Efficiency = 0.5f },
                new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.5f },
                new SessionHistoryEntry { Outcome = 1f, Efficiency = 0.5f },
                new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.5f }
            };
        }

        private static bool ContainsRule(AdjustmentProposal proposal, string ruleName)
        {
            if (proposal == null || proposal.Deltas == null)
                return false;

            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                if (proposal.Deltas[i].RuleName == ruleName)
                    return true;
            }

            return false;
        }

    }
}
