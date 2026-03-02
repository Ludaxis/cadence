using System.Collections.Generic;

namespace Cadence
{
    public class AdjustmentContext
    {
        public PlayerSkillProfile Profile;
        public SessionSummary LastSession;
        public FlowReading LastFlowReading;
        public Dictionary<string, float> LevelParameters;
        public List<SessionHistoryEntry> RecentHistory;
        public float SessionGapDays;
        public float TimeSinceLastAdjustment;

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
