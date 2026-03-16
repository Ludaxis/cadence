using UnityEngine;

namespace Cadence.Rules
{
    /// <summary>
    /// Applies gentle difficulty easing when the player has completed many levels in a contiguous play session.
    /// Thresholds, scaling, and reset behavior come from <see cref="AdjustmentEngineConfig"/>.
    /// </summary>
    public sealed class SessionFatigueRule : IAdjustmentRule
    {
        private readonly AdjustmentEngineConfig _config;

        public string RuleName => "SessionFatigue";

        public SessionFatigueRule(AdjustmentEngineConfig config)
        {
            _config = config;
        }

        public bool IsApplicable(AdjustmentContext context)
        {
            return IsEnabled() && context.LevelsThisSession >= GetThresholdLevels();
        }

        public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
        {
            int thresholdLevels = GetThresholdLevels();
            int fatigueLevels = context.LevelsThisSession - thresholdLevels + 1;
            float easeAmount = Mathf.Min(fatigueLevels * GetEasePerLevel(), GetMaxEase());

            if (context.LevelParameters == null) return;

            foreach (var kvp in context.LevelParameters)
            {
                float current = kvp.Value;
                if (current <= 0f) continue;

                if (ParameterAdjustmentUtility.TryCreateDelta(context, kvp.Key, current,
                    -easeAmount, RuleName, out var delta))
                {
                    proposal.Deltas.Add(delta);
                }
            }
        }

        private bool IsEnabled()
        {
            return _config == null || _config.EnableSessionFatigueRule;
        }

        private int GetThresholdLevels()
        {
            return Mathf.Max(1, _config != null
                ? _config.SessionFatigueThresholdLevels
                : AdjustmentEngineConfig.DefaultSessionFatigueThresholdLevels);
        }

        private float GetEasePerLevel()
        {
            return _config != null
                ? Mathf.Max(0f, _config.SessionFatigueEasePerLevel)
                : AdjustmentEngineConfig.DefaultSessionFatigueEasePerLevel;
        }

        private float GetMaxEase()
        {
            return _config != null
                ? Mathf.Max(0f, _config.SessionFatigueMaxEase)
                : AdjustmentEngineConfig.DefaultSessionFatigueMaxEase;
        }
    }
}
