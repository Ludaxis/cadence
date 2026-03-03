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
            if (context.Profile == null || !context.Profile.HasSufficientData)
                return false;
            if (context.LevelTypeConfig != null && !context.LevelTypeConfig.DDAEnabled)
                return false;
            return true;
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
                distance = minWR - winRate;
                direction = -1f;
            }
            else
            {
                // Upward adjustment blocked for Breather-type levels or at-risk archetypes
                if (context.LevelTypeConfig != null && !context.LevelTypeConfig.AllowUpwardAdjustment)
                    return;
                if (ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                    context.ArchetypeReading.Primary))
                    return;
                distance = winRate - maxWR;
                direction = 1f;
            }

            // Use animation curve to map distance to adjustment magnitude
            float magnitude = _config != null && _config.DifficultyAdjustmentCurve != null
                ? _config.DifficultyAdjustmentCurve.Evaluate(Mathf.Clamp01(distance))
                : distance;

            // Scale by level type adjustment scale (Boss=0.3x, Breather=0.5x, etc.)
            float typeScale = context.LevelTypeConfig != null
                ? context.LevelTypeConfig.AdjustmentScale : 1f;
            magnitude *= typeScale;

            // Scale by player archetype modifier
            magnitude *= ArchetypeAdjustmentStrategy.GetAdjustmentScaleModifier(
                context.ArchetypeReading.Primary);

            if (context.LevelParameters == null) return;

            // Determine if we should prioritize the primary parameter
            string primaryKey = context.LevelTypeConfig?.PrimaryParameterKey;

            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                float paramMagnitude = magnitude;
                // Primary parameter gets full adjustment, others get half
                if (primaryKey != null && kvp.Key != primaryKey)
                    paramMagnitude *= 0.5f;

                float delta = current * paramMagnitude * direction * 0.1f;
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
