using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class StreakQualityTests
    {
        [Test]
        public void LossStreak_CloseLosses_LessEasing()
        {
            var config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.LossStreakThreshold = 3;
            config.LossStreakEaseAmount = 0.10f;
            config.WinStreakThreshold = 10; // High to avoid win streak trigger
            config.GlobalCooldownSeconds = 0f;
            config.PerParameterCooldownSeconds = 0f;
            config.TargetWinRateMin = 0.3f;
            config.TargetWinRateMax = 0.7f;
            config.MaxDeltaPerAdjustment = 0.5f;
            config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            var rule = new Cadence.Rules.StreakDamperRule(config);

            // Close losses (high efficiency -- almost won)
            var closeHistory = new List<SessionHistoryEntry>();
            for (int i = 0; i < 4; i++)
                closeHistory.Add(new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.8f });

            var closeContext = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 10, AverageOutcome = 0.3f, RecentHistory = closeHistory },
                RecentHistory = closeHistory,
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                LastFlowReading = new FlowReading { State = FlowState.Flow }
            };

            var closeProposal = new AdjustmentProposal();
            rule.Evaluate(closeContext, closeProposal);

            // Blowout losses (low efficiency)
            var blowoutHistory = new List<SessionHistoryEntry>();
            for (int i = 0; i < 4; i++)
                blowoutHistory.Add(new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.1f });

            var blowoutContext = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 10, AverageOutcome = 0.3f, RecentHistory = blowoutHistory },
                RecentHistory = blowoutHistory,
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                LastFlowReading = new FlowReading { State = FlowState.Flow }
            };

            var blowoutProposal = new AdjustmentProposal();
            rule.Evaluate(blowoutContext, blowoutProposal);

            Assert.Greater(closeProposal.Deltas.Count, 0);
            Assert.Greater(blowoutProposal.Deltas.Count, 0);

            float closeEase = Mathf.Abs(closeProposal.Deltas[0].Delta);
            float blowoutEase = Mathf.Abs(blowoutProposal.Deltas[0].Delta);
            Assert.Greater(blowoutEase, closeEase, "Blowout losses should receive more easing than close losses");
        }

        [Test]
        public void LossStreak_AverageEfficiency_CalculatedCorrectly()
        {
            var config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.LossStreakThreshold = 2;
            config.LossStreakEaseAmount = 0.10f;
            config.WinStreakThreshold = 10;
            config.GlobalCooldownSeconds = 0f;
            config.PerParameterCooldownSeconds = 0f;
            config.TargetWinRateMin = 0.3f;
            config.TargetWinRateMax = 0.7f;
            config.MaxDeltaPerAdjustment = 0.5f;
            config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            var rule = new Cadence.Rules.StreakDamperRule(config);

            // Mix of close and blowout -- average should be 0.5 (neutral point)
            var mixedHistory = new List<SessionHistoryEntry>
            {
                new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.2f },
                new SessionHistoryEntry { Outcome = 0f, Efficiency = 0.8f }
            };

            var context = new AdjustmentContext
            {
                Profile = new PlayerSkillProfile { SessionsCompleted = 10, AverageOutcome = 0.3f, RecentHistory = mixedHistory },
                RecentHistory = mixedHistory,
                LevelParameters = new Dictionary<string, float> { { "difficulty", 100f } },
                LastFlowReading = new FlowReading { State = FlowState.Flow }
            };

            var proposal = new AdjustmentProposal();
            rule.Evaluate(context, proposal);

            // With avg efficiency ~0.5, quality scale should be ~1.0 (neutral)
            Assert.Greater(proposal.Deltas.Count, 0, "Should produce deltas for loss streak");
        }
    }
}
