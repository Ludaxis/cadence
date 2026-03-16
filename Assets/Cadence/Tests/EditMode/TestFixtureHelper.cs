using System.Collections.Generic;
using UnityEngine;

namespace Cadence.Tests
{
    /// <summary>
    /// Shared helpers for adjustment engine tests. Eliminates duplicate
    /// CreateContext / CreateHistory / config setup across test files.
    /// </summary>
    public static class TestFixtureHelper
    {
        /// <summary>
        /// Creates a standard AdjustmentEngineConfig with cooldowns disabled for testing.
        /// </summary>
        public static AdjustmentEngineConfig CreateDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.TargetWinRateMin = 0.3f;
            config.TargetWinRateMax = 0.7f;
            config.DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            config.LossStreakThreshold = 3;
            config.WinStreakThreshold = 5;
            config.LossStreakEaseAmount = 0.10f;
            config.WinStreakHardenAmount = 0.05f;
            config.FrustrationReliefThreshold = 0.7f;
            config.GlobalCooldownSeconds = 0f;
            config.PerParameterCooldownSeconds = 0f;
            config.MaxDeltaPerAdjustment = 0.15f;
            return config;
        }

        /// <summary>
        /// Creates an AdjustmentContext with standard test parameters.
        /// </summary>
        public static AdjustmentContext CreateContext(int sessionsCompleted, float averageOutcome,
            List<SessionHistoryEntry> recentHistory, LevelType levelType = LevelType.MoveLimited)
        {
            if (recentHistory == null)
                recentHistory = new List<SessionHistoryEntry>();

            var typeConfig = LevelTypeDefaults.GetDefaults(levelType);

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
                TimeSinceLastAdjustment = 1000f,
                LevelType = levelType,
                LevelTypeConfig = typeConfig
            };
        }

        /// <summary>
        /// Creates a session history with the given count and approximate win rate.
        /// </summary>
        public static List<SessionHistoryEntry> CreateHistory(int count, float winRate)
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

        /// <summary>
        /// Creates a consecutive loss history.
        /// </summary>
        public static List<SessionHistoryEntry> CreateLossHistory(int count, float efficiency = 0.4f)
        {
            var list = new List<SessionHistoryEntry>();
            for (int i = 0; i < count; i++)
                list.Add(new SessionHistoryEntry { Outcome = 0f, Efficiency = efficiency });
            return list;
        }

        /// <summary>
        /// Asserts that the delta moves the parameter in the "easier" direction
        /// (higher for move_limit/time_limit, lower for others).
        /// </summary>
        public static void AssertSemanticallyEasier(ParameterDelta delta, string message = null)
        {
            if (delta.ParameterKey == "move_limit" || delta.ParameterKey == "time_limit")
                NUnit.Framework.Assert.Greater(delta.ProposedValue, delta.CurrentValue, message);
            else
                NUnit.Framework.Assert.Less(delta.ProposedValue, delta.CurrentValue, message);
        }

        /// <summary>
        /// Asserts that the delta moves the parameter in the "harder" direction.
        /// </summary>
        public static void AssertSemanticallyHarder(ParameterDelta delta, string message = null)
        {
            if (delta.ParameterKey == "move_limit" || delta.ParameterKey == "time_limit")
                NUnit.Framework.Assert.Less(delta.ProposedValue, delta.CurrentValue, message);
            else
                NUnit.Framework.Assert.Greater(delta.ProposedValue, delta.CurrentValue, message);
        }

        /// <summary>
        /// Sums the deltas for a specific rule in a proposal.
        /// </summary>
        public static float SumDeltasByRule(AdjustmentProposal proposal, string ruleName)
        {
            float sum = 0f;
            foreach (var d in proposal.Deltas)
                if (d.RuleName == ruleName) sum += d.Delta;
            return sum;
        }
    }
}
