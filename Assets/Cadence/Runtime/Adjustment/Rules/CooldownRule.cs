using System.Collections.Generic;

namespace Cadence.Rules
{
    /// <summary>
    /// Prevents parameter oscillation by enforcing minimum intervals between adjustments.
    /// This rule runs last and removes deltas that are on cooldown.
    /// </summary>
    public sealed class CooldownRule : IAdjustmentRule
    {
        private readonly AdjustmentEngineConfig _config;
        private readonly Dictionary<string, float> _lastAdjustedTime = new Dictionary<string, float>();
        private float _lastGlobalTime = float.NegativeInfinity;

        public string RuleName => "Cooldown";

        public CooldownRule(AdjustmentEngineConfig config) => _config = config;

        public bool IsApplicable(AdjustmentContext context)
        {
            // Always applicable — it filters other rules' output
            return true;
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            float globalCooldown = _config != null ? _config.GlobalCooldownSeconds : 60f;
            float perParamCooldown = _config != null ? _config.PerParameterCooldownSeconds : 120f;
            float now = context.TimeSinceLastAdjustment;

            // Check global cooldown
            if (now - _lastGlobalTime < globalCooldown)
            {
                proposal.Deltas.Clear();
                return;
            }

            // Remove individual parameters that are on cooldown
            for (int i = proposal.Deltas.Count - 1; i >= 0; i--)
            {
                var delta = proposal.Deltas[i];
                if (_lastAdjustedTime.TryGetValue(delta.ParameterKey, out float lastTime))
                {
                    if (now - lastTime < perParamCooldown)
                    {
                        proposal.Deltas.RemoveAt(i);
                    }
                }
            }
        }

        public void RecordAdjustment(AdjustmentProposal proposal, float time)
        {
            _lastGlobalTime = time;
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                _lastAdjustedTime[proposal.Deltas[i].ParameterKey] = time;
            }
        }
    }
}
