using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    public sealed class AdjustmentEngine
    {
        private readonly AdjustmentEngineConfig _config;
        private readonly List<IAdjustmentRule> _rules = new List<IAdjustmentRule>();
        private readonly Dictionary<string, float> _lastAdjustmentTime = new Dictionary<string, float>();
        private float _lastGlobalAdjustmentTime = float.NegativeInfinity;

        public AdjustmentEngine(AdjustmentEngineConfig config)
        {
            _config = config;

            // Register built-in rules
            _rules.Add(new Rules.FlowChannelRule(config));
            _rules.Add(new Rules.StreakDamperRule(config));
            _rules.Add(new Rules.FrustrationReliefRule(config));
            _rules.Add(new Rules.CooldownRule(config));
        }

        public void AddRule(IAdjustmentRule rule)
        {
            _rules.Add(rule);
        }

        public AdjustmentProposal Evaluate(AdjustmentContext context)
        {
            var proposal = new AdjustmentProposal
            {
                Timing = AdjustmentTiming.BeforeNextLevel,
                DetectedState = context.LastFlowReading.State
            };

            // Let each rule add deltas
            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule.IsApplicable(context))
                {
                    rule.Evaluate(context, proposal);
                }
            }

            // Clamp all deltas
            float maxDelta = _config != null ? _config.MaxDeltaPerAdjustment : 0.15f;
            ClampDeltas(proposal, maxDelta);

            // Compute confidence from profile data quality
            proposal.Confidence = context.Profile != null
                ? context.Profile.Confidence01
                : 0f;

            // Build reason string
            if (proposal.Deltas.Count > 0)
            {
                proposal.Reason = $"Flow: {context.LastFlowReading.State}, " +
                    $"Deltas: {proposal.Deltas.Count}, " +
                    $"WinRate: {context.Profile?.AverageOutcome:F2}";
            }

            return proposal;
        }

        public void RecordAdjustment(AdjustmentProposal proposal, float currentTime)
        {
            _lastGlobalAdjustmentTime = currentTime;
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                _lastAdjustmentTime[proposal.Deltas[i].ParameterKey] = currentTime;
            }
        }

        public bool IsOnGlobalCooldown(float currentTime)
        {
            float cooldown = _config != null ? _config.GlobalCooldownSeconds : 60f;
            return currentTime - _lastGlobalAdjustmentTime < cooldown;
        }

        public bool IsParameterOnCooldown(string paramKey, float currentTime)
        {
            float cooldown = _config != null ? _config.PerParameterCooldownSeconds : 120f;
            if (_lastAdjustmentTime.TryGetValue(paramKey, out float lastTime))
                return currentTime - lastTime < cooldown;
            return false;
        }

        private static void ClampDeltas(AdjustmentProposal proposal, float maxDelta)
        {
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                var delta = proposal.Deltas[i];
                float absDelta = Mathf.Abs(delta.Delta);
                if (absDelta > maxDelta && delta.CurrentValue != 0f)
                {
                    float maxAbsChange = Mathf.Abs(delta.CurrentValue) * maxDelta;
                    float clampedValue = delta.CurrentValue +
                        Mathf.Sign(delta.Delta) * maxAbsChange;
                    delta.ProposedValue = clampedValue;
                    proposal.Deltas[i] = delta;
                }
            }
        }
    }
}
