using System.Collections.Generic;

namespace Cadence
{
    /// <summary>
    /// Read-only context passed to <see cref="IAdjustmentRule"/> implementations during proposal generation.
    /// Contains the player's current state, session history, level configuration, and scheduling data
    /// needed for rules to decide whether and how to adjust difficulty.
    /// </summary>
    public class AdjustmentContext
    {
        /// <summary>Current player skill profile (Glicko-2 rating, deviation, volatility).</summary>
        public PlayerSkillProfile Profile;

        /// <summary>Summary of the most recently completed session (scores, timing, outcome).</summary>
        public SessionSummary LastSession;

        /// <summary>Last flow reading from the session's real-time flow detector.</summary>
        public FlowReading LastFlowReading;

        /// <summary>Baseline parameters for the next level that rules will adjust.</summary>
        public Dictionary<string, float> LevelParameters;

        /// <summary>Recent session history entries for streak and trend analysis.</summary>
        public List<SessionHistoryEntry> RecentHistory;

        /// <summary>Days elapsed since the player's last session, for churn risk detection.</summary>
        public float SessionGapDays;

        /// <summary>Time in days since the last difficulty adjustment was applied.</summary>
        public float TimeSinceLastAdjustment;

        /// <summary>Level type of the upcoming level (e.g., MoveLimited, Boss, Breather).</summary>
        public LevelType LevelType;

        /// <summary>Per-level-type configuration controlling adjustment scale and constraints.</summary>
        public LevelTypeConfig LevelTypeConfig;

        /// <summary>Sawtooth curve multiplier at the current level index from the difficulty scheduler.</summary>
        public float SawtoothMultiplier;

        /// <summary>Zero-based global level index for the upcoming level.</summary>
        public int CurrentLevelIndex;

        /// <summary>Number of levels the player has completed in the current play session.</summary>
        public int LevelsThisSession;

        /// <summary>Current player archetype classification with per-archetype confidence scores.</summary>
        public PlayerArchetypeReading ArchetypeReading;

        /// <summary>
        /// Counts consecutive outcomes of the same type from most recent history.
        /// </summary>
        public int CountRecentStreak(bool wins)
        {
            if (RecentHistory == null || RecentHistory.Count == 0) return 0;
            int streak = 0;
            float target = wins ? 1f : 0f;
            for (int i = RecentHistory.Count - 1; i >= 0; i--)
            {
                if (Approximately(RecentHistory[i].Outcome, target))
                    streak++;
                else
                    break;
            }
            return streak;
        }

        private static bool Approximately(float a, float b) => a > b - 0.01f && a < b + 0.01f;
    }
}
