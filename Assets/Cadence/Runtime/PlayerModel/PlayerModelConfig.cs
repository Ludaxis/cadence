using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Player Model Config")]
#if ODIN_INSPECTOR
    [Title("Player Model", "Glicko-2 skill rating system", TitleAlignments.Centered)]
    [InfoBox(
        "Maintains a Glicko-2 skill rating for each player.\n\n" +
        "Rating  = estimated skill (1500 = average)\n" +
        "Deviation = uncertainty (350 = brand new, <100 = confident)\n" +
        "Volatility = consistency of performance over time\n\n" +
        "After each session, the system updates these using the full Glicko-2 algorithm " +
        "(5-step: scale -> variance/delta -> volatility search -> update -> convert back).",
        InfoMessageType.None)]
#endif
    public class PlayerModelConfig : ScriptableObject
    {
        // ───────────────────── Initial Values ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Initial Values")]
        [InfoBox("Starting values for a brand-new player with no history.")]
        [PropertyTooltip("Starting Glicko-2 rating. 1500 is the universal default. " +
                          "Higher starting value = system assumes the player is better initially.")]
        [SuffixLabel("rating points", Overlay = true)]
#else
        [Header("Initial Values — starting profile for a new player")]
        [Tooltip("Starting Glicko-2 rating. 1500 = average. Higher = system assumes player is better initially. Updates after each session based on performance vs expected.")]
#endif
        public float InitialRating = 1500f;

#if ODIN_INSPECTOR
        [BoxGroup("Initial Values")]
        [PropertyTooltip("Starting uncertainty. 350 = maximum uncertainty (brand new player). " +
                          "System converges toward true skill as sessions accumulate.\n\n" +
                          "Confidence = 1 - (Deviation / 350):\n" +
                          "  350 = 0% confident\n" +
                          "  175 = 50% confident\n" +
                          "  35  = 90% confident")]
        [SuffixLabel("RD", Overlay = true)]
#else
        [Tooltip("Starting uncertainty (Rating Deviation). 350 = brand new, maximum uncertainty. Confidence = 1-(RD/350): 350=0%, 175=50%, 35=90%. Shrinks as more sessions are played.")]
#endif
        public float InitialDeviation = 350f;

#if ODIN_INSPECTOR
        [BoxGroup("Initial Values")]
        [PropertyTooltip("Starting volatility. Controls how much the rating can fluctuate between sessions.\n" +
                          "0.06 is the Glicko-2 standard default.\n" +
                          "Higher = rating swings more. Lower = more stable ratings.")]
        [PropertyRange(0.01f, 0.15f)]
#else
        [Tooltip("How much the rating can fluctuate between sessions. 0.06 = Glicko-2 standard. Higher = more swing, lower = more stable. Range: 0.01-0.15.")]
        [Range(0.01f, 0.15f)]
#endif
        public float InitialVolatility = 0.06f;

        // ───────────────────── System Constants ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("System Constants")]
        [InfoBox("Advanced Glicko-2 algorithm parameters. Change with care.")]
        [PropertyTooltip("Tau (τ) constrains how much volatility can change per update.\n\n" +
                          "Lower (0.3-0.5) = conservative, ratings change slowly.\n" +
                          "Higher (0.8-1.2) = aggressive, ratings swing more.\n\n" +
                          "0.5 is recommended for puzzle games where skill is relatively stable.")]
        [LabelText("Tau (τ)")]
        [PropertyRange(0.2f, 1.5f)]
#else
        [Space(10)]
        [Header("System Constants — advanced Glicko-2 parameters")]
        [Tooltip("Tau constrains volatility change speed. Lower (0.3-0.5) = conservative, ratings change slowly. Higher (0.8-1.2) = aggressive. 0.5 recommended for puzzle games.")]
        [Range(0.2f, 1.5f)]
#endif
        public float Tau = 0.5f;

#if ODIN_INSPECTOR
        [BoxGroup("System Constants")]
        [PropertyTooltip("Convergence tolerance for the Illinois algorithm volatility search.\n" +
                          "Smaller = more precise but more iterations. Default 0.000001 is standard.")]
        [LabelText("Convergence Epsilon (ε)")]
#else
        [Tooltip("Convergence tolerance for the volatility iteration algorithm. 0.000001 is the standard default. No need to change this.")]
#endif
        public float ConvergenceEpsilon = 0.000001f;

        // ───────────────────── Time Decay ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Inactivity Decay")]
        [InfoBox("When a player is away, uncertainty increases (deviation grows). " +
                 "This prevents stale ratings from producing overconfident adjustments for returning players.")]
        [PropertyTooltip("Deviation increase per day of inactivity.\n\n" +
                          "5 RD/day means after 1 week away, deviation grows by 35.\n" +
                          "After 70 days, deviation caps at MaxDeviation (full reset to uncertain).")]
        [SuffixLabel("RD / day", Overlay = true)]
        [PropertyRange(0f, 20f)]
#else
        [Space(10)]
        [Header("Inactivity Decay — uncertainty grows when player is away")]
        [Tooltip("Deviation increase per day of inactivity (in RD units). At 5/day, after 1 week away deviation grows by 35. Prevents stale ratings from producing overconfident adjustments for returning players.")]
        [Range(0f, 20f)]
#endif
        public float DeviationDecayPerDay = 5f;

#if ODIN_INSPECTOR
        [BoxGroup("Inactivity Decay")]
        [PropertyTooltip("Upper bound for deviation after time decay. 350 = fully uncertain " +
                          "(same as a new player). Prevents deviation from growing unbounded.")]
        [SuffixLabel("RD max", Overlay = true)]
        [PropertyRange(100f, 500f)]
#else
        [Tooltip("Upper cap for deviation after time decay. 350 = fully uncertain (same as a brand new player).")]
        [Range(100f, 500f)]
#endif
        public float MaxDeviation = 350f;

        // ───────────────────── Session History ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Session History")]
        [InfoBox("Rolling history of recent sessions. Used by Player Profiler for archetype classification " +
                 "and by Streak Damper for win/loss streak detection.")]
        [PropertyTooltip("Maximum sessions stored in the rolling history buffer. " +
                          "Older entries are evicted. 20 covers about 1-2 weeks of daily play.")]
        [SuffixLabel("entries", Overlay = true)]
        [PropertyRange(5, 100)]
#else
        [Space(10)]
        [Header("Session History — rolling buffer for archetype & streak detection")]
        [Tooltip("Max sessions in the rolling history buffer. Used by Player Profiler (archetype classification) and Streak Damper (win/loss streak detection). 20 covers about 1-2 weeks of daily play.")]
        [Range(5, 100)]
#endif
        public int MaxHistoryEntries = 20;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rating must be positive
            if (InitialRating < 0f) InitialRating = 0f;

            // Deviation must be positive and <= MaxDeviation
            if (InitialDeviation < 1f) InitialDeviation = 1f;
            if (InitialDeviation > MaxDeviation) InitialDeviation = MaxDeviation;

            // MaxDeviation must be >= InitialDeviation
            if (MaxDeviation < InitialDeviation) MaxDeviation = InitialDeviation;

            // History buffer must hold at least a few sessions
            if (MaxHistoryEntries < 5) MaxHistoryEntries = 5;
        }
#endif
    }
}
