using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Flow Detector Config")]
#if ODIN_INSPECTOR
    [Title("Flow Detector", "Real-time player state classification", TitleAlignments.Centered)]
    [InfoBox(
        "Classifies the player into one of 5 flow states every tick:\n\n" +
        "  Flow        — Moderate efficiency, steady tempo (default)\n" +
        "  Boredom     — Efficiency > 0.85 AND Tempo > 0.7 (too easy)\n" +
        "  Anxiety     — Efficiency < 0.3 AND Tempo < 0.2 (too hard)\n" +
        "  Frustration — FrustrationScore > 0.7 (overwhelmed)\n" +
        "  Unknown     — Less than WarmupMoves recorded\n\n" +
        "States require HysteresisCount consecutive ticks to confirm, preventing rapid oscillation.",
        InfoMessageType.None)]
#endif
    public class FlowDetectorConfig : ScriptableObject
    {
        // ───────────────────── Sliding Windows ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Sliding Windows")]
        [InfoBox(
            "Each score is computed from a sliding window of recent signals.\n" +
            "Larger windows = smoother but slower to react. Smaller = more responsive but noisy.")]
        [PropertyTooltip("Window size for inter-move interval tracking. Feeds TempoScore (consistency of pacing).")]
        [SuffixLabel("signals", Overlay = true)]
        [PropertyRange(4, 32)]
#else
        [Header("Sliding Windows — recent signal windows for score computation")]
        [Tooltip("Window size for inter-move interval tracking. Feeds TempoScore (pacing consistency). Larger = smoother but slower to react. Smaller = responsive but noisy. Range: 4-32.")]
        [Range(4, 32)]
#endif
        public int TempoWindowSize = 8;

#if ODIN_INSPECTOR
        [BoxGroup("Sliding Windows")]
        [PropertyTooltip("Window size for move optimality tracking. Feeds EfficiencyScore (ratio of optimal moves).")]
        [SuffixLabel("signals", Overlay = true)]
        [PropertyRange(4, 48)]
#else
        [Tooltip("Window size for move optimality tracking. Feeds EfficiencyScore (ratio of optimal moves). Larger = smoother, smaller = more responsive. Range: 4-48.")]
        [Range(4, 48)]
#endif
        public int EfficiencyWindowSize = 12;

#if ODIN_INSPECTOR
        [BoxGroup("Sliding Windows")]
        [PropertyTooltip("Window size for engagement events (progress, pauses, rejected inputs). Feeds EngagementScore.")]
        [SuffixLabel("signals", Overlay = true)]
        [PropertyRange(4, 64)]
#else
        [Tooltip("Window size for engagement events (progress, pauses, rejected inputs). Feeds EngagementScore. Larger = more stable engagement reading. Range: 4-64.")]
        [Range(4, 64)]
#endif
        public int EngagementWindowSize = 20;

        // ───────────────────── Thresholds ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow State Thresholds")]
        [DetailedInfoBox("Boredom Detection Formula",
            "Triggers when the player is breezing through:\n\n" +
            "  IF EfficiencyScore > BoredomEfficiencyMin\n" +
            "  AND TempoScore > BoredomTempoMin\n" +
            "    -> FlowState.Boredom\n\n" +
            "Game response: Increase challenge.")]
        [LabelText("Boredom: Min Efficiency")]
        [PropertyTooltip("Player efficiency must exceed this to be considered bored. 0.85 = 85% optimal moves.")]
        [PropertyRange(0.5f, 1f)]
#else
        [Space(10)]
        [Header("Flow State Thresholds — when each state triggers")]
        [Tooltip("Boredom: Min Efficiency. Player efficiency must exceed this AND tempo must exceed BoredomTempoMin to be classified as 'Bored' (too easy). 0.85 = 85% optimal moves.")]
#endif
        [Range(0f, 1f)] public float BoredomEfficiencyMin = 0.85f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow State Thresholds")]
        [LabelText("Boredom: Min Tempo")]
        [PropertyTooltip("Player tempo consistency must exceed this to be considered bored. 0.7 = very consistent pacing.")]
        [PropertyRange(0.3f, 1f)]
#else
        [Tooltip("Boredom: Min Tempo. Pacing consistency must also exceed this to trigger Boredom. 0.7 = very consistent, fast play. Both efficiency AND tempo must pass their thresholds.")]
#endif
        [Range(0f, 1f)] public float BoredomTempoMin = 0.7f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow State Thresholds")]
        [PropertySpace(10)]
        [DetailedInfoBox("Anxiety Detection Formula",
            "Triggers when the player shows erratic, struggling behavior:\n\n" +
            "  IF EfficiencyScore < AnxietyEfficiencyMax\n" +
            "  AND TempoScore < AnxietyTempoMax\n" +
            "    -> FlowState.Anxiety\n\n" +
            "Game response: Reduce pressure.")]
        [LabelText("Anxiety: Max Efficiency")]
        [PropertyTooltip("Efficiency below this (with low tempo) indicates anxiety. 0.3 = only 30% optimal moves.")]
        [PropertyRange(0f, 0.5f)]
#else
        [Space(5)]
        [Tooltip("Anxiety: Max Efficiency. Efficiency below this AND tempo below AnxietyTempoMax = Anxiety state (player is struggling). 0.3 = only 30% of moves are optimal.")]
#endif
        [Range(0f, 1f)] public float AnxietyEfficiencyMax = 0.3f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow State Thresholds")]
        [LabelText("Anxiety: Max Tempo")]
        [PropertyTooltip("Tempo below this (with low efficiency) indicates anxiety. 0.2 = very erratic pacing.")]
        [PropertyRange(0f, 0.5f)]
#else
        [Tooltip("Anxiety: Max Tempo. Tempo below this (with low efficiency) = Anxiety. 0.2 = very erratic, inconsistent pacing indicating the player is lost or overwhelmed.")]
#endif
        [Range(0f, 1f)] public float AnxietyTempoMax = 0.2f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow State Thresholds")]
        [PropertySpace(10)]
        [DetailedInfoBox("Frustration Detection Formula",
            "Highest priority state. Checked BEFORE Boredom/Anxiety:\n\n" +
            "  FrustrationScore = (1 - Efficiency) * 0.5\n" +
            "                   + (1 - Tempo) * 0.3\n" +
            "                   + (1 - Engagement) * 0.2\n\n" +
            "  IF FrustrationScore > FrustrationThreshold\n" +
            "    -> FlowState.Frustration\n\n" +
            "Game response: Provide relief immediately.")]
        [LabelText("Frustration Threshold")]
        [PropertyTooltip("FrustrationScore above this triggers the Frustration state. 0.7 = high threshold (fewer false positives).")]
        [PropertyRange(0.4f, 0.95f)]
        [GUIColor(1f, 0.85f, 0.85f)]
#else
        [Space(5)]
        [Tooltip("Frustration Threshold. HIGHEST PRIORITY — checked before Boredom/Anxiety. Score = weighted combo of low efficiency (50%), erratic tempo (30%), low engagement (20%). 0.7 = high bar, fewer false positives.")]
#endif
        [Range(0f, 1f)] public float FrustrationThreshold = 0.7f;

        // ───────────────────── Stability ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Stability & Smoothing")]
        [InfoBox(
            "Prevents rapid state oscillation and ensures enough data before classifying.\n" +
            "EMA (Exponential Moving Average) smooths raw scores to reduce noise.")]
        [PropertyTooltip("Consecutive ticks at the same classified state before confirming the transition. " +
                          "Prevents flickering between states.")]
        [SuffixLabel("ticks", Overlay = true)]
        [PropertyRange(1, 10)]
#else
        [Space(10)]
        [Header("Stability & Smoothing — prevents rapid state oscillation")]
        [Tooltip("Hysteresis Count. Consecutive ticks at the same classified state before confirming the transition. Prevents flickering between states. 3 = standard. Higher = more stable but slower to react.")]
        [Range(1, 10)]
#endif
        public int HysteresisCount = 3;

#if ODIN_INSPECTOR
        [BoxGroup("Stability & Smoothing")]
        [PropertyTooltip("Minimum moves before the detector returns a valid state. " +
                          "Returns FlowState.Unknown until this threshold. " +
                          "Prevents early misclassification.")]
        [SuffixLabel("moves", Overlay = true)]
        [PropertyRange(2, 20)]
#else
        [Tooltip("Warmup Moves. Minimum moves before the detector returns a valid state. Returns 'Unknown' until this count is reached. Prevents early misclassification when data is sparse.")]
        [Range(2, 20)]
#endif
        public int WarmupMoves = 5;

#if ODIN_INSPECTOR
        [BoxGroup("Stability & Smoothing")]
        [PropertyTooltip("EMA smoothing factor (alpha). Higher = faster response to changes, more noise. " +
                          "Lower = smoother but slower. 0.3 is a good balance.")]
        [LabelText("EMA Alpha")]
        [PropertyRange(0.01f, 1f)]
#else
        [Tooltip("EMA Alpha (Exponential Moving Average). Smoothing factor for raw scores. Higher = faster response but noisier. Lower = smoother but slower. 0.3 = good balance for real-time classification.")]
#endif
        [Range(0.01f, 1f)] public float ExponentialAlpha = 0.3f;
    }
}
