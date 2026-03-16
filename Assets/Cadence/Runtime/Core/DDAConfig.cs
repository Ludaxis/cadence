using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/DDA Config")]
#if ODIN_INSPECTOR
    [Title("Cadence DDA", "Master Configuration", TitleAlignments.Centered)]
    [InfoBox(
        "Root configuration for the Dynamic Difficulty Adjustment system.\n\n" +
        "Wire all sub-configs here. The Setup Wizard (Cadence > Setup Wizard) can auto-create and wire everything.",
        InfoMessageType.None)]
#endif
    public class DDAConfig : ScriptableObject
    {
        // ───────────────────── Sub-Configs ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Sub-Configurations")]
        [InfoBox(
            "Each sub-config controls a major subsystem. " +
            "Click the foldout arrow to edit inline, or double-click the asset to open it separately.",
            InfoMessageType.Info)]
        [Required("PlayerModelConfig is required. Drives Glicko-2 skill rating and player history.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
#else
        [Header("Sub-Configurations — wire all 3 required configs here")]
        [Tooltip("Glicko-2 player skill model. Controls rating, deviation, volatility, and time decay. Create via Assets > Create > Cadence > Player Model Config.")]
#endif
        public PlayerModelConfig PlayerModelConfig;

#if ODIN_INSPECTOR
        [TitleGroup("Sub-Configurations")]
        [Required("FlowDetectorConfig is required. Controls real-time flow state detection.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
#else
        [Tooltip("Real-time flow state detector. Classifies player as Flow/Boredom/Anxiety/Frustration each frame. Create via Assets > Create > Cadence > Flow Detector Config.")]
#endif
        public FlowDetectorConfig FlowDetectorConfig;

#if ODIN_INSPECTOR
        [TitleGroup("Sub-Configurations")]
        [Required("AdjustmentEngineConfig is required. Controls all built-in adjustment rules, including session fatigue and cooldowns.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
#else
        [Tooltip("Adjustment rules that propose difficulty changes. Controls win-rate targeting, streak damping, frustration relief, new-player easing, session fatigue, and cooldowns. Create via Assets > Create > Cadence > Adjustment Engine Config.")]
#endif
        public AdjustmentEngineConfig AdjustmentEngineConfig;

        // ───────────────────── Signal Collection ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Signal Collection")]
        [InfoBox(
            "Controls the signal ring buffer and optional persistent storage.\n" +
            "Signals are gameplay events (moves, pauses, boosters) collected during a session.")]
        [PropertyTooltip("Circular buffer capacity for in-memory signals. 512 is sufficient for most levels.")]
        [SuffixLabel("signals", Overlay = true)]
#else
        [Space(10)]
        [Header("Signal Collection — ring buffer and disk storage for gameplay signals")]
        [Tooltip("Circular buffer capacity for in-memory signals per session. 512 is enough for most levels. Signals are gameplay events like moves, pauses, and booster uses.")]
#endif
        public int RingBufferCapacity = 512;

#if ODIN_INSPECTOR
        [TitleGroup("Signal Collection")]
        [PropertyTooltip("When enabled, signals are persisted to disk for offline analysis and replay.")]
        [ToggleLeft]
#else
        [Tooltip("Save signal batches to disk as JSON files. Enables Signal Replay tool for debugging. Files stored in Application.persistentDataPath/Cadence/Signals/.")]
#endif
        public bool EnableSignalStorage = true;

#if ODIN_INSPECTOR
        [TitleGroup("Signal Collection")]
        [PropertyTooltip("Maximum number of session signal files kept on disk. Oldest are pruned first.")]
        [SuffixLabel("sessions", Overlay = true)]
        [PropertyRange(5, 200)]
        [ShowIf("EnableSignalStorage")]
#else
        [Tooltip("Maximum signal files kept on disk per level. Oldest are pruned first. Only used when Enable Signal Storage is on.")]
        [Range(5, 200)]
#endif
        public int MaxStoredSessions = 50;

        // ───────────────────── Scheduling ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Difficulty Scheduling")]
        [InfoBox(
            "Optional sawtooth curve that creates periodic difficulty waves.\n" +
            "Ramps up -> Boss spike -> Breather dip -> Repeat.\n" +
            "Leave null for flat difficulty (no scheduled variation).")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
#else
        [Space(10)]
        [Header("Scheduling — optional sawtooth difficulty waves")]
        [Tooltip("Optional. Creates periodic difficulty waves: ramp up → Boss spike → Breather dip → repeat. Leave empty for flat difficulty (no scheduled variation). Create via Assets > Create > Cadence > Sawtooth Curve Config.")]
#endif
        public SawtoothCurveConfig SawtoothCurveConfig;

        // ───────────────────── Global Toggles ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Global Toggles")]
        [InfoBox(
            "Master switches for the two DDA modes:\n" +
            "- Mid-Session: Flow detector runs during gameplay, can trigger immediate relief.\n" +
            "- Between-Session: Proposals generated after level end for the next level.")]
        [PropertyTooltip("Run the flow detector every tick during gameplay. Enables mid-session frustration relief.")]
        [ToggleLeft]
#else
        [Space(10)]
        [Header("Global Toggles — master switches for DDA modes")]
        [Tooltip("Run the flow detector every frame during gameplay. Classifies player state (Flow/Boredom/Anxiety/Frustration) in real-time. Enables mid-session frustration relief. Disable to save CPU if you only need between-session proposals.")]
#endif
        public bool EnableMidSessionDetection = true;

#if ODIN_INSPECTOR
        [TitleGroup("Global Toggles")]
        [PropertyTooltip("Generate adjustment proposals after each session ends. This is the primary DDA mechanism.")]
        [ToggleLeft]
#else
        [Tooltip("Generate difficulty adjustment proposals after each EndSession() call. This is the primary DDA mechanism. GetProposal() returns empty results when disabled.")]
#endif
        public bool EnableBetweenSessionAdjustment = true;

        public static event Action OnConfigChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnConfigChanged?.Invoke();
        }
#endif
    }
}
