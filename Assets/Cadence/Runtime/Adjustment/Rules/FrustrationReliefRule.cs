using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Detects frustration and proposes easing adjustments.
    /// Triggers when the session's FrustrationScore exceeds the threshold.
    /// </summary>
    public sealed class FrustrationReliefRule : IAdjustmentRule
    {
        private readonly AdjustmentEngineConfig _config;

        public string RuleName => "FrustrationRelief";

        public FrustrationReliefRule(AdjustmentEngineConfig config) => _config = config;

        public bool IsApplicable(AdjustmentContext context)
        {
            float threshold = _config != null ? _config.FrustrationReliefThreshold : 0.7f;
            return context.LastSession.FrustrationScore > threshold ||
                   context.LastFlowReading.State == FlowState.Frustration;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            float threshold = _config != null ? _config.FrustrationReliefThreshold : 0.7f;
            float frustration = context.LastSession.FrustrationScore;
            float severity = Mathf.Clamp01((frustration - threshold) / (1f - threshold));

            // Base ease amount scales with severity
            float easeAmount = Mathf.Lerp(0.05f, 0.15f, severity);

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

            // Mark as mid-session if frustration is from flow detector
            if (context.LastFlowReading.State == FlowState.Frustration)
            {
                proposal.Timing = AdjustmentTiming.MidSession;
            }
        }
    }
}
