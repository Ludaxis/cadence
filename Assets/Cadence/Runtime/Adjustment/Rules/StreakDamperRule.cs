using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Prevents long loss or win streaks by adjusting difficulty.
    /// 3+ losses → reduce difficulty by configured amount.
    /// 5+ wins → increase difficulty by configured amount.
    /// </summary>
    public sealed class StreakDamperRule : IAdjustmentRule
    {
        private readonly AdjustmentEngineConfig _config;

        public string RuleName => "StreakDamper";

        public StreakDamperRule(AdjustmentEngineConfig config) => _config = config;

        public bool IsApplicable(AdjustmentContext context)
        {
            if (context.RecentHistory == null || context.RecentHistory.Count < 2)
                return false;

            int lossThreshold = _config != null ? _config.LossStreakThreshold : 3;
            int winThreshold = _config != null ? _config.WinStreakThreshold : 5;

            int lossStreak = context.CountRecentStreak(false);
            int winStreak = context.CountRecentStreak(true);

            return lossStreak >= lossThreshold || winStreak >= winThreshold;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            int lossThreshold = _config != null ? _config.LossStreakThreshold : 3;
            int winThreshold = _config != null ? _config.WinStreakThreshold : 5;

            int lossStreak = context.CountRecentStreak(false);
            int winStreak = context.CountRecentStreak(true);

            float direction;
            float amount;

            if (lossStreak >= lossThreshold)
            {
                direction = -1f;
                amount = _config != null ? _config.LossStreakEaseAmount : 0.10f;
                // Scale with streak length
                float extra = Mathf.Max(0, lossStreak - lossThreshold) * 0.02f;
                amount += extra;
            }
            else if (winStreak >= winThreshold)
            {
                direction = 1f;
                amount = _config != null ? _config.WinStreakHardenAmount : 0.05f;
                float extra = Mathf.Max(0, winStreak - winThreshold) * 0.01f;
                amount += extra;
            }
            else
            {
                return;
            }

            if (context.LevelParameters == null) return;

            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                float delta = current * amount * direction;
                proposal.Deltas.Add(new ParameterDelta
                {
                    ParameterKey = kvp.Key,
                    CurrentValue = current,
                    ProposedValue = current + delta,
                    RuleName = RuleName
                });
            }
        }
    }
}
