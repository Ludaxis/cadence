using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Provides gentle difficulty easing for new players (first 5 sessions).
    /// Easing amount decreases as the player gains experience.
    /// Registered before CooldownRule so cooldown can still filter output.
    /// </summary>
    public sealed class NewPlayerRule : IAdjustmentRule
    {
        private const int NewPlayerSessionCap = 5;
        private readonly AdjustmentEngineConfig _config;

        public string RuleName => "NewPlayer";

        public NewPlayerRule(AdjustmentEngineConfig config) => _config = config;

        public bool IsApplicable(AdjustmentContext context)
        {
            if (context.Profile == null) return false;
            return context.Profile.SessionsCompleted < NewPlayerSessionCap;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            // Only ease when player is losing (below 50% win rate)
            if (context.Profile.AverageOutcome >= 0.5f && context.Profile.SessionsCompleted > 0)
                return;

            float sessions = context.Profile.SessionsCompleted;
            // Easing decreases as player gains experience: 15% -> 2% over 5 sessions
            float easeAmount = Mathf.Lerp(0.15f, 0.02f, sessions / NewPlayerSessionCap);

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
