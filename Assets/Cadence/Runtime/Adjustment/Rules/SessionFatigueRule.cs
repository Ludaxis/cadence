using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Applies gentle difficulty easing when the player has played many levels in a single session.
    /// Eases 2% per level after the 8th, capped at 10%.
    /// Opt-in: register via AdjustmentEngine.AddRule() — not included in default rules.
    /// </summary>
    public sealed class SessionFatigueRule : IAdjustmentRule
    {
        private const int FatigueThreshold = 8;
        private const float EasePerLevel = 0.02f;
        private const float MaxEase = 0.10f;

        public string RuleName => "SessionFatigue";

        public bool IsApplicable(AdjustmentContext context)
        {
            return context.LevelsThisSession >= FatigueThreshold;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            int levelsOver = context.LevelsThisSession - FatigueThreshold;
            float easeAmount = Mathf.Min(levelsOver * EasePerLevel, MaxEase);

            if (context.LevelParameters == null) return;

            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                float delta = -current * easeAmount;
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
