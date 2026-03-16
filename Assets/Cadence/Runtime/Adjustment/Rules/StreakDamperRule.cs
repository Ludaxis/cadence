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
            if (context.LevelTypeConfig != null && !context.LevelTypeConfig.DDAEnabled)
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
                float extra = Mathf.Max(0, lossStreak - lossThreshold) * 0.02f;
                amount += extra;

                // Scale by loss quality: close losses need less easing, blowouts need more
                float avgLossEfficiency = GetRecentLossEfficiency(context, lossStreak);
                float qualityScale = avgLossEfficiency > 0.5f
                    ? Mathf.Lerp(1.0f, 0.5f, (avgLossEfficiency - 0.5f) * 2f)  // Close: 50-100%
                    : Mathf.Lerp(1.0f, 1.3f, (0.5f - avgLossEfficiency) * 2f); // Blowout: 100-130%
                amount *= qualityScale;
            }
            else if (winStreak >= winThreshold)
            {
                // Upward adjustment blocked for Breather-type levels or at-risk archetypes
                if (context.LevelTypeConfig != null && !context.LevelTypeConfig.AllowUpwardAdjustment)
                    return;
                if (ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                    context.ArchetypeReading.Primary))
                    return;
                direction = 1f;
                amount = _config != null ? _config.WinStreakHardenAmount : 0.05f;
                float extra = Mathf.Max(0, winStreak - winThreshold) * 0.01f;
                amount += extra;
            }
            else
            {
                return;
            }

            // Scale by level type adjustment scale
            float typeScale = context.LevelTypeConfig != null
                ? context.LevelTypeConfig.AdjustmentScale : 1f;
            amount *= typeScale;

            // Scale by player archetype modifier
            amount *= ArchetypeAdjustmentStrategy.GetAdjustmentScaleModifier(
                context.ArchetypeReading.Primary);

            if (context.LevelParameters == null) return;

            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                if (ParameterAdjustmentUtility.TryCreateDelta(context, kvp.Key, current,
                    amount * direction, RuleName, out var delta))
                {
                    proposal.Deltas.Add(delta);
                }
            }
        }

        private static float GetRecentLossEfficiency(AdjustmentContext context, int streakLength)
        {
            if (context.RecentHistory == null || context.RecentHistory.Count == 0)
                return 0.5f;

            float sum = 0f;
            int count = 0;
            for (int i = context.RecentHistory.Count - 1; i >= 0 && count < streakLength; i--)
            {
                var entry = context.RecentHistory[i];
                if (entry.Outcome < 0.5f) // Loss
                {
                    sum += entry.Efficiency;
                    count++;
                }
                else
                {
                    break; // Streak ended
                }
            }

            return count > 0 ? sum / count : 0.5f;
        }
    }
}
