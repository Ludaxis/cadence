using UnityEngine;

namespace Cadence
{
    internal static class ParameterAdjustmentUtility
    {
        /// <summary>
        /// Creates a parameter delta from a semantic difficulty change where
        /// positive means harder and negative means easier.
        /// </summary>
        public static bool TryCreateDelta(AdjustmentContext context, string parameterKey,
            float currentValue, float signedDifficultyFraction, string ruleName,
            out ParameterDelta delta)
        {
            delta = default;

            if (string.IsNullOrEmpty(parameterKey) ||
                currentValue <= 0f ||
                Mathf.Approximately(signedDifficultyFraction, 0f))
            {
                return false;
            }

            if (context?.LevelTypeConfig != null &&
                !context.LevelTypeConfig.IsParameterAdjustable(parameterKey))
            {
                return false;
            }

            float numericDirection = context?.LevelTypeConfig != null
                ? context.LevelTypeConfig.GetNumericDirection(parameterKey)
                : 1f;

            float numericDelta = currentValue * signedDifficultyFraction * numericDirection;
            float proposedValue = currentValue + numericDelta;

            if (context?.LevelTypeConfig != null)
                proposedValue = context.LevelTypeConfig.ClampProposedValue(parameterKey, proposedValue);

            if (Mathf.Approximately(proposedValue, currentValue))
                return false;

            delta = new ParameterDelta
            {
                ParameterKey = parameterKey,
                CurrentValue = currentValue,
                ProposedValue = proposedValue,
                RuleName = ruleName
            };
            return true;
        }
    }
}
