using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Per-parameter adjustment metadata used to translate semantic difficulty
    /// changes into the correct numeric direction and optional bounds.
    /// </summary>
    [Serializable]
    public sealed class ParameterSemantics
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Level parameter key this metadata applies to.")]
#endif
        public string ParameterKey;

#if ODIN_INSPECTOR
        [PropertyTooltip("Whether larger numeric values make the level harder or easier.")]
#endif
        public ParameterPolarity Polarity = ParameterPolarity.HigherIsHarder;

#if ODIN_INSPECTOR
        [PropertyTooltip("If false, adjustment rules skip this parameter entirely.")]
        [ToggleLeft]
#endif
        public bool Adjustable = true;

#if ODIN_INSPECTOR
        [PropertyTooltip("Clamp proposed values to a minimum bound after rule evaluation.")]
        [ToggleLeft]
#endif
        public bool HasMinValue;

#if ODIN_INSPECTOR
        [ShowIf("HasMinValue")]
#endif
        public float MinValue;

#if ODIN_INSPECTOR
        [PropertyTooltip("Clamp proposed values to a maximum bound after rule evaluation.")]
        [ToggleLeft]
#endif
        public bool HasMaxValue;

#if ODIN_INSPECTOR
        [ShowIf("HasMaxValue")]
#endif
        public float MaxValue;

        public float DifficultyToNumericDirection =>
            Polarity == ParameterPolarity.HigherIsEasier ? -1f : 1f;

        public float Clamp(float value)
        {
            if (HasMinValue)
                value = Mathf.Max(value, MinValue);
            if (HasMaxValue)
                value = Mathf.Min(value, MaxValue);
            return value;
        }
    }
}
