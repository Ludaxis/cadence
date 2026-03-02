using System.Collections.Generic;
using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Keeps the predicted win rate within the target band (30-70%).
    /// If the player's predicted win rate is outside the band, adjust
    /// difficulty parameters toward the band center.
    /// </summary>
    public sealed class FlowChannelRule : IAdjustmentRule
    {
        private readonly AdjustmentEngineConfig _config;

        public string RuleName => "FlowChannel";

        public FlowChannelRule(AdjustmentEngineConfig config) => _config = config;

        public bool IsApplicable(AdjustmentContext context)
        {
            return context.Profile != null && context.Profile.HasSufficientData;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            float winRate = context.Profile.AverageOutcome;
            float minWR = _config != null ? _config.TargetWinRateMin : 0.3f;
            float maxWR = _config != null ? _config.TargetWinRateMax : 0.7f;

            if (winRate >= minWR && winRate <= maxWR) return;

            // Compute how far outside the band we are
            float distance;
            float direction;
            if (winRate < minWR)
            {
                // Player losing too much — make easier (reduce difficulty params)
                distance = minWR - winRate;
                direction = -1f;
            }
            else
            {
                // Player winning too much — make harder (increase difficulty params)
                distance = winRate - maxWR;
                direction = 1f;
            }

            // Use animation curve to map distance to adjustment magnitude
            float magnitude = _config != null && _config.DifficultyAdjustmentCurve != null
                ? _config.DifficultyAdjustmentCurve.Evaluate(Mathf.Clamp01(distance))
                : distance;

            if (context.LevelParameters == null) return;

            // Apply proportional delta to all level parameters
            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                float delta = current * magnitude * direction * 0.1f;
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
