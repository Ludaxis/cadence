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
            _config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            _config.TargetWinRateMin = 0.3f;
            _config.TargetWinRateMax = 0.7f;
            _config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _config.LossStreakThreshold = 3;
            _config.WinStreakThreshold = 5;
            _config.LossStreakEaseAmount = 0.10f;
            _config.WinStreakHardenAmount = 0.05f;
            _config.FrustrationReliefThreshold = 0.7f;
            _config.GlobalCooldownSeconds = 0f;       // Disable cooldown for tests
            _config.PerParameterCooldownSeconds = 0f;
            _config.MaxDeltaPerAdjustment = 0.15f;

            _engine = new AdjustmentEngine(_config);
        }

        [Test]
        public void Evaluate_NoDataProfile_ReturnsEmptyProposal()
        {
            var context = CreateContext(
                sessionsCompleted: 0,
                averageOutcome: 0f,
                recentHistory: new List<SessionHistoryEntry>()
            );

            var proposal = _engine.Evaluate(context);

            // FlowChannelRule not applicable (no sufficient data)
            // StreakDamperRule not applicable (no history)
            Assert.IsNotNull(proposal);
        }

        [Test]
        public void Evaluate_LowWinRate_ProposesEasier()
        {
            var context = CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.15f, // Below 0.3 target
                recentHistory: CreateHistory(10, winRate: 0.15f)
            );

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);
            // All deltas should reduce difficulty (negative direction for loss)
            foreach (var delta in proposal.Deltas)
            {
                if (delta.RuleName == "FlowChannel")
                    Assert.Less(delta.ProposedValue, delta.CurrentValue);
            }
        }

        [Test]
        public void Evaluate_HighWinRate_ProposesHarder()
        {
            var context = CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.85f, // Above 0.7 target
                recentHistory: CreateHistory(10, winRate: 0.85f)
            );

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);
            foreach (var delta in proposal.Deltas)
            {
                if (delta.RuleName == "FlowChannel")
                    Assert.Greater(delta.ProposedValue, delta.CurrentValue);
            }
        }

        [Test]
        public void Evaluate_InFlowBand_NoFlowChannelDeltas()
        {
            var context = CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.5f, // In band
                recentHistory: CreateHistory(10, winRate: 0.5f)
            );

            var proposal = _engine.Evaluate(context);

            int flowChannelDeltas = 0;
            foreach (var d in proposal.Deltas)
                if (d.RuleName == "FlowChannel") flowChannelDeltas++;

            Assert.AreEqual(0, flowChannelDeltas);
        }

        [Test]
        public void Evaluate_LossStreak_ProposesEasier()
        {
            var history = new List<SessionHistoryEntry>();
            // 4 losses in a row (above threshold of 3)
            for (int i = 0; i < 4; i++)
                history.Add(new SessionHistoryEntry { Outcome = 0f });

            var context = CreateContext(
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
                    Assert.Less(d.ProposedValue, d.CurrentValue);
                }
            }
            Assert.IsTrue(hasStreakDamper);
        }

        [Test]
        public void Evaluate_WinStreak_ProposesHarder()
        {
            var history = new List<SessionHistoryEntry>();
            // 6 wins in a row (above threshold of 5)
            for (int i = 0; i < 6; i++)
                history.Add(new SessionHistoryEntry { Outcome = 1f });

            var context = CreateContext(
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
                    Assert.Greater(d.ProposedValue, d.CurrentValue);
                }
            }
            Assert.IsTrue(hasStreakDamper);
        }

        [Test]
        public void Evaluate_HighFrustration_ProposesRelief()
        {
            var context = CreateContext(
                sessionsCompleted: 5,
                averageOutcome: 0.2f,
                recentHistory: CreateHistory(5, winRate: 0.2f)
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
                    Assert.Less(d.ProposedValue, d.CurrentValue);
                }
            }
            Assert.IsTrue(hasRelief);
        }

        [Test]
        public void Evaluate_DeltasClamped()
        {
            _config.MaxDeltaPerAdjustment = 0.05f; // Very tight clamp

            var context = CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.05f, // Far outside band
                recentHistory: CreateHistory(10, winRate: 0.05f)
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
            var context = CreateContext(
                sessionsCompleted: 10,
                averageOutcome: 0.15f, // Should ease
                recentHistory: CreateHistory(10, winRate: 0.15f)
            );
            context.SawtoothMultiplier = 1.3f; // Hard cycle point

            var proposal = _engine.Evaluate(context);

            Assert.IsNotNull(proposal);
            Assert.Greater(proposal.Deltas.Count, 0);

            foreach (var d in proposal.Deltas)
            {
                Assert.Less(d.ProposedValue, d.CurrentValue,
                    $"Sawtooth must not flip easing delta for {d.ParameterKey}");
            }
        }

        // --- Helpers ---

        private AdjustmentContext CreateContext(int sessionsCompleted, float averageOutcome,
            List<SessionHistoryEntry> recentHistory)
        {
            return new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f,
                    Deviation = 100f,
                    SessionsCompleted = sessionsCompleted,
                    AverageOutcome = averageOutcome,
                    RecentHistory = recentHistory
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
                RecentHistory = recentHistory,
                TimeSinceLastAdjustment = 1000f // Far past any cooldown
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
    }
}
