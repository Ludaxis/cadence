using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Per-level-type DDA configuration that controls adjustment scaling, parameter targeting, and constraints.
    /// Each <see cref="LevelType"/> can have different adjustment behavior (e.g., Tutorial disables DDA entirely,
    /// Boss levels receive minimal adjustments). Use <see cref="LevelTypeDefaults.GetDefaults"/> for standard presets.
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [InfoBox(
        "Per-level-type DDA configuration. Each level type has different adjustment behavior.\n" +
        "Use LevelTypeDefaults.GetDefaults() for standard presets, or customize per level.",
        InfoMessageType.None)]
#endif
    public sealed class LevelTypeConfig
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("The level type this config applies to.")]
#endif
        public LevelType Type;

#if ODIN_INSPECTOR
        [PropertyTooltip("The primary parameter that this level type adjusts.\n\n" +
                          "MoveLimited -> \"move_limit\" (primary adjustment target)\n" +
                          "TimeLimited -> \"time_limit\"\n" +
                          "GoalCollection -> \"goal_count\"\n" +
                          "Others -> null (all parameters treated equally)\n\n" +
                          "Primary parameter gets full adjustment magnitude. Others get 50%.")]
        [LabelText("Primary Parameter Key")]
#endif
        public string PrimaryParameterKey;

#if ODIN_INSPECTOR
        [PropertyTooltip("Secondary parameters that receive reduced adjustments (50% of primary).\n" +
                          "These provide supporting difficulty changes alongside the primary parameter.")]
#endif
        public System.Collections.Generic.List<string> SecondaryParameterKeys;

#if ODIN_INSPECTOR
        [PropertyTooltip("Multiplier for all adjustment deltas on this level type.\n\n" +
                          "1.0 = Standard (full adjustment)\n" +
                          "0.8 = GoalCollection (slightly conservative)\n" +
                          "0.5 = Breather (gentle adjustments only)\n" +
                          "0.3 = Boss (minimal adjustments — handcrafted difficulty)\n" +
                          "0.0 = Tutorial (no adjustments)")]
        [PropertyRange(0f, 2f)]
        [SuffixLabel("multiplier", Overlay = true)]
#endif
        public float AdjustmentScale;

#if ODIN_INSPECTOR
        [PropertyTooltip("Whether difficulty can be increased (made harder) for this level type.\n\n" +
                          "false for Breather = never harden breather levels.\n" +
                          "false for Tutorial = tutorial should always be easy.\n" +
                          "Also blocked by ChurnRisk and StrugglingLearner archetypes.")]
        [ToggleLeft]
#endif
        public bool AllowUpwardAdjustment;

#if ODIN_INSPECTOR
        [PropertyTooltip("Master switch for DDA on this level type.\n\n" +
                          "false for Tutorial = DDA is completely disabled.\n" +
                          "When disabled, GetProposal returns null and no rules are evaluated.")]
        [ToggleLeft]
        [GUIColor("@DDAEnabled ? Color.white : new Color(1f, 0.7f, 0.7f)")]
#endif
        public bool DDAEnabled;

        public LevelTypeConfig()
        {
            Type = LevelType.Standard;
            PrimaryParameterKey = null;
            AdjustmentScale = 1f;
            AllowUpwardAdjustment = true;
            DDAEnabled = true;
        }

        public LevelTypeConfig(LevelType type, string primaryKey, float scale,
            bool allowUpward, bool enabled)
        {
            Type = type;
            PrimaryParameterKey = primaryKey;
            AdjustmentScale = scale;
            AllowUpwardAdjustment = allowUpward;
            DDAEnabled = enabled;
        }
    }
}
