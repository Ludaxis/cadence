using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    /// <summary>
    /// Shared color palette and style constants for all Cadence editor tools.
    /// Centralizes flow state and level type color mappings to ensure visual consistency.
    /// </summary>
    public static class CadenceEditorStyles
    {
        // ───────────────────── Flow State Colors ─────────────────────

        public static Color GetFlowColor(FlowState state)
        {
            switch (state)
            {
                case FlowState.Boredom:     return new Color(0.3f, 0.6f, 1f);
                case FlowState.Flow:        return new Color(0.2f, 0.9f, 0.3f);
                case FlowState.Anxiety:     return new Color(1f, 0.8f, 0.2f);
                case FlowState.Frustration: return new Color(1f, 0.3f, 0.2f);
                default:                    return Color.gray;
            }
        }

        // ───────────────────── Level Type Colors ─────────────────────

        public static Color GetLevelTypeColor(LevelType type)
        {
            switch (type)
            {
                case LevelType.Boss:     return new Color(1f, 0.3f, 0.2f);
                case LevelType.Breather: return new Color(0.3f, 0.8f, 1f);
                case LevelType.Tutorial: return new Color(0.7f, 0.7f, 0.7f);
                default:                 return new Color(0.4f, 0.9f, 0.4f);
            }
        }

        // ───────────────────── Shared Data Types ─────────────────────

        /// <summary>
        /// Key-value pair for editable level parameters in editor tools.
        /// Used by SandboxDashboard and ScenarioSimulator.
        /// </summary>
        public struct ParamEntry
        {
            public string Key;
            public float Value;
        }

        // ───────────────────── Shared Utilities ─────────────────────

        /// <summary>
        /// Converts a ParamEntry array to a Dictionary for use with DDAService APIs.
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, float> BuildParamDict(
            ParamEntry[] entries)
        {
            var dict = new System.Collections.Generic.Dictionary<string, float>();
            if (entries == null) return dict;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!string.IsNullOrEmpty(entries[i].Key))
                    dict[entries[i].Key] = entries[i].Value;
            }
            return dict;
        }

        /// <summary>
        /// Applies an AdjustmentProposal's deltas to a parameter dictionary in-place.
        /// </summary>
        public static void ApplyProposal(AdjustmentProposal proposal,
            System.Collections.Generic.Dictionary<string, float> parameters)
        {
            if (proposal?.Deltas == null || parameters == null) return;
            for (int i = 0; i < proposal.Deltas.Count; i++)
            {
                var delta = proposal.Deltas[i];
                if (parameters.ContainsKey(delta.ParameterKey))
                    parameters[delta.ParameterKey] = delta.ProposedValue;
            }
        }
    }
}
