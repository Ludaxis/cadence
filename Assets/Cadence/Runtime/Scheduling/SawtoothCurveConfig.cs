using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Sawtooth Curve Config")]
#if ODIN_INSPECTOR
    [Title("Sawtooth Difficulty Curve", "Periodic difficulty waves for level progression", TitleAlignments.Centered)]
    [InfoBox(
        "Creates repeating difficulty waves modeled after top puzzle games (Candy Crush, Royal Match, Toon Blast).\n\n" +
        "Each cycle of N levels follows this pattern:\n" +
        "  Levels 0..N-2  — Ramp up difficulty (multiplier rises from baseline to baseline + amplitude)\n" +
        "  Level N-1       — Boss spike (hardest level in the cycle)\n" +
        "  Level N         — Breather dip (easiest, relief after boss)\n\n" +
        "The multiplier scales ALL proposed parameter deltas. A multiplier of 1.3 means 30% harder than baseline.\n" +
        "Baseline itself drifts upward each cycle to create long-term progression.",
        InfoMessageType.None)]
#endif
    public class SawtoothCurveConfig : ScriptableObject
    {
        // ───────────────────── Cycle Shape ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Cycle Shape")]
        [InfoBox("Defines the shape and intensity of each difficulty wave.")]
        [PropertyTooltip("Number of levels in one complete sawtooth cycle.\n" +
                          "10 = every 10 levels, difficulty resets with a boss at level 9 and breather at level 10.\n" +
                          "Shorter periods (5-8) create more frequent tension/relief. Longer (15-25) for slower pacing.")]
        [SuffixLabel("levels", Overlay = true)]
#else
        [Header("Cycle Shape — shape and intensity of each difficulty wave")]
        [Tooltip("Number of levels in one complete sawtooth cycle. 10 = every 10 levels, difficulty resets with boss at level 9, breather at level 10. Shorter (5-8) = more frequent tension/relief. Longer (15-25) = slower pacing.")]
#endif
        [Range(5, 50)] public int Period = 10;

#if ODIN_INSPECTOR
        [BoxGroup("Cycle Shape")]
        [PropertyTooltip("Peak-to-trough range of the difficulty multiplier.\n\n" +
                          "0.3 = multiplier varies from 0.7x (easy) to 1.3x (hard).\n" +
                          "0.1 = subtle variation (0.9x to 1.1x).\n" +
                          "0.5 = dramatic swings (0.5x to 1.5x).")]
        [SuffixLabel("+/- from baseline", Overlay = true)]
#else
        [Tooltip("Amplitude. Peak-to-trough range of the difficulty multiplier. 0.3 = varies 0.7x (easy) to 1.3x (hard). 0.1 = subtle variation. 0.5 = dramatic swings.")]
#endif
        [Range(0.05f, 0.5f)] public float Amplitude = 0.3f;

#if ODIN_INSPECTOR
        [BoxGroup("Cycle Shape")]
        [PropertyTooltip("How deep the relief dip goes after the boss spike.\n\n" +
                          "0.15 = breather level is 15% easier than the cycle baseline.\n" +
                          "Set to 0 for no special relief. Higher = more noticeable breather.")]
        [SuffixLabel("below baseline", Overlay = true)]
#else
        [Tooltip("Relief Depth. How deep the breather dip goes after the boss spike. 0.15 = breather is 15% easier than cycle baseline. 0 = no special relief. Higher = more noticeable breather.")]
#endif
        [Range(0f, 0.3f)] public float ReliefDepth = 0.15f;

#if ODIN_INSPECTOR
        [BoxGroup("Cycle Shape")]
        [PropertyTooltip("How difficulty ramps up within each cycle:\n\n" +
                          "Linear   — Constant rate of increase.\n" +
                          "EaseIn   — Gentle start, steep finish (builds tension).\n" +
                          "EaseOut  — Steep start, gentle finish (front-loaded).\n" +
                          "SCurve   — Gentle start and finish, steep middle (recommended).")]
#else
        [Tooltip("Ramp Style. How difficulty ramps within each cycle. Linear = constant rate. EaseIn = gentle start, steep finish. EaseOut = steep start, gentle finish. SCurve = gentle start and finish (recommended).")]
#endif
        public RampStyle RampStyle = RampStyle.SCurve;

#if ODIN_INSPECTOR
        [BoxGroup("Cycle Shape")]
        [PropertyTooltip("Optional custom AnimationCurve that overrides RampStyle.\n" +
                          "X axis: 0 (cycle start) to 1 (cycle end).\n" +
                          "Y axis: 0 (minimum multiplier) to 1 (maximum multiplier).\n" +
                          "Leave empty to use the RampStyle enum.")]
#else
        [Tooltip("Custom Curve Shape. Optional AnimationCurve that overrides RampStyle. X: 0 (cycle start) to 1 (cycle end). Y: 0 (min multiplier) to 1 (max multiplier). Leave empty to use the RampStyle enum instead.")]
#endif
        public AnimationCurve CurveShape = new AnimationCurve();

        // ───────────────────── Boss / Breather Positions ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Boss / Breather Positions")]
        [InfoBox("Where the boss spike and breather dip occur within each cycle.\n" +
                 "Boss is offset from the END of the cycle. Breather is offset from the START of the next cycle.")]
        [LabelText("Boss Offset from End")]
        [PropertyTooltip("Boss level offset from the end of the cycle.\n" +
                          "-1 = second-to-last level is the boss.\n" +
                          "0 = last level of the cycle is the boss.")]
        [SuffixLabel("from cycle end", Overlay = true)]
#else
        [Space(10)]
        [Header("Boss / Breather Positions — spike and dip placement in each cycle")]
        [Tooltip("Boss Offset from End. Boss level offset from the end of the cycle. -1 = second-to-last level is the boss. 0 = last level is the boss.")]
#endif
        [Range(-5, 0)] public int BossLevelOffset = -1;

#if ODIN_INSPECTOR
        [BoxGroup("Boss / Breather Positions")]
        [LabelText("Breather Offset from Start")]
        [PropertyTooltip("Breather level offset from the start of the NEXT cycle.\n" +
                          "0 = first level of the next cycle is the breather.\n" +
                          "1 = second level is the breather.")]
        [SuffixLabel("from next cycle start", Overlay = true)]
#else
        [Tooltip("Breather Offset from Start. Breather level offset from the start of the NEXT cycle. 0 = first level of next cycle is the breather. 1 = second level is the breather.")]
#endif
        [Range(0, 3)] public int BreatherLevelOffset = 0;

        // ───────────────────── Long-Term Progression ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Long-Term Progression")]
        [InfoBox("Baseline difficulty creeps upward each cycle, creating long-term progression.\n" +
                 "After 10 cycles with drift 0.02, baseline is 0.2 higher than it started.")]
        [PropertyTooltip("Baseline multiplier increase per completed cycle.\n" +
                          "0.02 = 2% harder baseline each cycle.\n" +
                          "0 = no long-term progression (each cycle is identical difficulty-wise).")]
        [SuffixLabel("per cycle", Overlay = true)]
#else
        [Space(10)]
        [Header("Long-Term Progression — baseline creeps upward each cycle")]
        [Tooltip("Baseline Drift per Cycle. Baseline multiplier increase per completed cycle. 0.02 = 2% harder each cycle. After 10 cycles = 20% harder baseline. 0 = no long-term progression (each cycle identical).")]
#endif
        [Range(0f, 0.1f)] public float BaselineDriftPerCycle = 0.02f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Boss offset must be negative or zero (offset from end)
            if (BossLevelOffset > 0) BossLevelOffset = 0;

            // Breather offset must be non-negative (offset from start of next cycle)
            if (BreatherLevelOffset < 0) BreatherLevelOffset = 0;

            // Ensure breather fits within the cycle
            if (BreatherLevelOffset >= Period) BreatherLevelOffset = Period - 1;
        }
#endif
    }

    public enum RampStyle : byte
    {
#if ODIN_INSPECTOR
        [LabelText("Linear — Constant rate")]
#endif
        Linear = 0,

#if ODIN_INSPECTOR
        [LabelText("Ease In — Gentle start, steep finish")]
#endif
        EaseIn = 1,

#if ODIN_INSPECTOR
        [LabelText("Ease Out — Steep start, gentle finish")]
#endif
        EaseOut = 2,

#if ODIN_INSPECTOR
        [LabelText("S-Curve — Gentle start and finish (recommended)")]
#endif
        SCurve = 3
    }
}
