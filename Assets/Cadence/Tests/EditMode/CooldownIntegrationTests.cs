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
            var config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.GlobalCooldownSeconds = 60f;
            config.PerParameterCooldownSeconds = 120f;
            config.TargetWinRateMin = 0.3f;
            config.TargetWinRateMax = 0.7f;
            config.LossStreakThreshold = 3;
            config.WinStreakThreshold = 5;
            config.MaxDeltaPerAdjustment = 0.15f;
            config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            var engine = new AdjustmentEngine(config);

            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f, Deviation = 100f,
                    SessionsCompleted = 10, AverageOutcome = 0.15f,
                    RecentHistory = CreateLossHistory(4)
                },
                LastSession = new SessionSummary { Outcome = SessionOutcome.Lose },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                RecentHistory = CreateLossHistory(4),
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
            var config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.GlobalCooldownSeconds = 10f;
            config.PerParameterCooldownSeconds = 10f;
            config.TargetWinRateMin = 0.3f;
            config.TargetWinRateMax = 0.7f;
            config.LossStreakThreshold = 3;
            config.WinStreakThreshold = 5;
            config.MaxDeltaPerAdjustment = 0.15f;
            config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            var engine = new AdjustmentEngine(config);

            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile
                {
                    Rating = 1500f, Deviation = 100f,
                    SessionsCompleted = 10, AverageOutcome = 0.15f,
                    RecentHistory = CreateLossHistory(4)
                },
                LastSession = new SessionSummary { Outcome = SessionOutcome.Lose },
                LastFlowReading = new FlowReading { State = FlowState.Flow },
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                RecentHistory = CreateLossHistory(4),
                TimeSinceLastAdjustment = 100f
            };

            var first = engine.Evaluate(context);
            engine.RecordAdjustment(first, 100f);

            // After cooldown expires
            context.TimeSinceLastAdjustment = 200f; // Well past 10s cooldown
            var second = engine.Evaluate(context);
            Assert.Greater(second.Deltas.Count, 0, "After cooldown expires, proposals should work again");
        }

        private static List<SessionHistoryEntry> CreateLossHistory(int count)
        {
            var list = new List<SessionHistoryEntry>();
            for (int i = 0; i < count; i++)
                list.Add(new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.4f });
            return list;
        }
    }
}
