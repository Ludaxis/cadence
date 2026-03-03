using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Adjustment Engine Config")]
#if ODIN_INSPECTOR
    [Title("Adjustment Engine", "Rules that modify difficulty parameters", TitleAlignments.Centered)]
    [InfoBox(
        "Four rules are evaluated in order after each session:\n\n" +
        "1. Flow Channel Rule — Nudges win rate toward the target band\n" +
        "2. Streak Damper Rule — Eases after loss streaks, hardens after win streaks\n" +
        "3. Frustration Relief Rule — Emergency easing when FrustrationScore > threshold\n" +
        "4. Cooldown Rule — Enforces minimum time between adjustments\n\n" +
        "All deltas are clamped by MaxDeltaPerAdjustment, then scaled by the sawtooth multiplier (if configured).",
        InfoMessageType.None)]
#endif
    public class AdjustmentEngineConfig : ScriptableObject
    {
        // ───────────────────── Flow Channel ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow Channel Rule")]
        [DetailedInfoBox("How Flow Channel Works",
            "Activates when the player's win rate drifts outside the target band.\n\n" +
            "  IF AverageOutcome < TargetWinRateMin (too hard):\n" +
            "    -> Ease difficulty (negative delta)\n" +
            "  IF AverageOutcome > TargetWinRateMax (too easy):\n" +
            "    -> Harden difficulty (positive delta)\n\n" +
            "Delta magnitude = DifficultyAdjustmentCurve(distance from band edge)\n" +
            "                 * LevelTypeConfig.AdjustmentScale\n" +
            "                 * ArchetypeAdjustmentStrategy.ScaleModifier\n\n" +
            "Primary parameter gets full adjustment. Secondary parameters get 50%.\n" +
            "Requires at least 5 completed sessions (HasSufficientData).")]
        [LabelText("Target Win Rate — Minimum")]
        [PropertyTooltip("Lower bound of the healthy win rate band. Below this, difficulty eases.\n" +
                          "0.3 = 30% win rate. Puzzle games typically use 0.3-0.4.")]
        [PropertyRange(0.1f, 0.5f)]
        [GUIColor(0.85f, 1f, 0.85f)]
#else
        [Header("Flow Channel")]
#endif
        [Range(0f, 1f)] public float TargetWinRateMin = 0.3f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow Channel Rule")]
        [LabelText("Target Win Rate — Maximum")]
        [PropertyTooltip("Upper bound of the healthy win rate band. Above this, difficulty hardens.\n" +
                          "0.7 = 70% win rate. Puzzle games typically use 0.6-0.75.")]
        [PropertyRange(0.5f, 0.95f)]
        [GUIColor(0.85f, 1f, 0.85f)]
#endif
        [Range(0f, 1f)] public float TargetWinRateMax = 0.7f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Flow Channel Rule")]
        [PropertyTooltip("Maps distance from the win rate band edge (X: 0-1) to adjustment magnitude (Y: 0-1).\n\n" +
                          "Linear = proportional response.\n" +
                          "Ease-in = gentle near the band, aggressive far from it.\n" +
                          "Ease-out = aggressive near the band, gentle far from it.")]
#endif
        public AnimationCurve DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // ───────────────────── Streak Damping ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Streak Damper Rule")]
        [DetailedInfoBox("How Streak Damper Works",
            "Detects consecutive win/loss streaks from RecentHistory.\n\n" +
            "  Loss streak >= LossStreakThreshold:\n" +
            "    Ease = LossStreakEaseAmount + (streak - threshold) * 0.02\n\n" +
            "  Win streak >= WinStreakThreshold:\n" +
            "    Harden = WinStreakHardenAmount + (streak - threshold) * 0.01\n\n" +
            "Both scaled by LevelType.AdjustmentScale and Archetype modifier.\n" +
            "Upward adjustment blocked for ChurnRisk and StrugglingLearner archetypes.")]
        [LabelText("Loss Streak Threshold")]
        [PropertyTooltip("Consecutive losses before easing kicks in. 3 = standard (lose 3 in a row -> ease).")]
        [SuffixLabel("losses", Overlay = true)]
        [PropertyRange(2, 10)]
#else
        [Header("Streak Damping")]
#endif
        public int LossStreakThreshold = 3;

#if ODIN_INSPECTOR
        [FoldoutGroup("Streak Damper Rule")]
        [LabelText("Win Streak Threshold")]
        [PropertyTooltip("Consecutive wins before hardening kicks in. 5 = generous (lets players enjoy success).")]
        [SuffixLabel("wins", Overlay = true)]
        [PropertyRange(2, 15)]
#endif
        public int WinStreakThreshold = 5;

#if ODIN_INSPECTOR
        [FoldoutGroup("Streak Damper Rule")]
        [LabelText("Loss Ease Amount")]
        [PropertyTooltip("Base easing percentage when loss streak triggers.\n" +
                          "0.10 = 10% reduction per parameter. Escalates +2% per additional loss beyond threshold.")]
        [SuffixLabel("per param", Overlay = true)]
#endif
        [Range(0f, 0.3f)] public float LossStreakEaseAmount = 0.10f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Streak Damper Rule")]
        [LabelText("Win Harden Amount")]
        [PropertyTooltip("Base hardening percentage when win streak triggers.\n" +
                          "0.05 = 5% increase per parameter. Intentionally gentler than easing. " +
                          "Escalates +1% per additional win beyond threshold.")]
        [SuffixLabel("per param", Overlay = true)]
#endif
        [Range(0f, 0.3f)] public float WinStreakHardenAmount = 0.05f;

        // ───────────────────── Frustration Relief ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Frustration Relief Rule")]
        [DetailedInfoBox("How Frustration Relief Works",
            "Emergency easing when the player is overwhelmed.\n\n" +
            "  Triggers when:\n" +
            "    FrustrationScore > Threshold  OR  FlowState == Frustration\n\n" +
            "  FrustrationScore (Session Analyzer):\n" +
            "    = WasteRatio * 0.30\n" +
            "    + InterMoveVariance * 0.25\n" +
            "    + PauseCount * 0.20\n" +
            "    + (1 - MoveEfficiency) * 0.25\n\n" +
            "  Severity = (score - threshold) / (1 - threshold)\n" +
            "  EaseAmount = Lerp(5%, 15%, severity)\n" +
            "             * LevelType.AdjustmentScale\n" +
            "             * 1.5x for ChurnRisk\n" +
            "             * 1.3x for StrugglingLearner\n\n" +
            "  If FlowState == Frustration: Timing = MidSession (immediate)")]
        [LabelText("Frustration Threshold")]
        [PropertyTooltip("FrustrationScore above this triggers relief.\n" +
                          "0.7 = high bar (reduces false positives). Lower = more sensitive to frustration signals.")]
        [PropertyRange(0.3f, 0.95f)]
        [GUIColor(1f, 0.85f, 0.85f)]
#else
        [Header("Frustration Relief")]
#endif
        [Range(0f, 1f)] public float FrustrationReliefThreshold = 0.7f;

        // ───────────────────── Cooldown ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Cooldown Rule")]
        [InfoBox("Prevents adjustment spam. Two layers:\n" +
                 "- Global: no adjustments within N seconds of the last one.\n" +
                 "- Per-parameter: each parameter has its own cooldown.")]
        [LabelText("Global Cooldown")]
        [PropertyTooltip("Minimum seconds between any two adjustment proposals. " +
                          "60s prevents back-to-back adjustments across session retries.")]
        [SuffixLabel("seconds", Overlay = true)]
        [PropertyRange(0f, 300f)]
#else
        [Header("Cooldown")]
#endif
        public float GlobalCooldownSeconds = 60f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Cooldown Rule")]
        [LabelText("Per-Parameter Cooldown")]
        [PropertyTooltip("Minimum seconds before the same parameter can be adjusted again.\n" +
                          "120s ensures a parameter isn't whipsawed between opposing rules.")]
        [SuffixLabel("seconds", Overlay = true)]
        [PropertyRange(0f, 600f)]
#endif
        public float PerParameterCooldownSeconds = 120f;

        // ───────────────────── Clamping ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Safety Clamping")]
        [InfoBox("Hard limit on how much any single adjustment can change a parameter. " +
                 "Prevents runaway adjustments from dramatically altering level difficulty in one step.")]
        [LabelText("Max Delta per Adjustment")]
        [PropertyTooltip("Maximum change as a fraction of the current parameter value.\n" +
                          "0.15 = 15%. A parameter at 20 can change by at most +/- 3 in one proposal.")]
        [SuffixLabel("of current value", Overlay = true)]
        [PropertyRange(0.01f, 0.5f)]
#else
        [Header("Clamping")]
#endif
        [Range(0f, 0.5f)] public float MaxDeltaPerAdjustment = 0.15f;
    }
}
