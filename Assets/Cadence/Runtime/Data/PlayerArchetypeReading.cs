using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Result of player behavior classification. Contains primary/secondary archetypes
    /// with confidence scores, plus raw per-archetype scores (0-1).
    /// </summary>
    [Serializable]
    public struct PlayerArchetypeReading
    {
#if ODIN_INSPECTOR
        [BoxGroup("Classification")]
        [PropertyTooltip("Highest-scoring archetype. Determines primary adjustment modulation.")]
#endif
        public PlayerArchetype Primary;

#if ODIN_INSPECTOR
        [BoxGroup("Classification")]
        [PropertyTooltip("Confidence in the primary classification (0-1). Based on score magnitude and margin over secondary.")]
        [ProgressBar(0f, 1f, ColorGetter = "GetPrimaryColor")]
#endif
        public float PrimaryConfidence;

#if ODIN_INSPECTOR
        [BoxGroup("Classification")]
        [PropertyTooltip("Second-highest archetype. Provides secondary modulation hints.")]
#endif
        public PlayerArchetype Secondary;

#if ODIN_INSPECTOR
        [BoxGroup("Classification")]
        [ProgressBar(0f, 1f, 0.7f, 0.7f, 0.7f)]
#endif
        public float SecondaryConfidence;

        // ───────────────────── Raw Scores ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Raw Archetype Scores")]
        [InfoBox(
            "Each score is computed from session history patterns (0 = no match, 1 = strong match).\n" +
            "Highest score becomes Primary archetype. Minimum threshold: 0.3.")]
        [PropertyTooltip("High efficiency + fast completion + high win rate.\n" +
                          "Score = AvgEfficiency*0.3 + WinRate*0.25 + EfficiencyTrend*0.15 + SpeedBonus*0.3")]
        [ProgressBar(0f, 1f, 0.2f, 0.9f, 0.3f)]
        [LabelText("Speed Runner")]
#endif
        public float SpeedRunnerScore;

#if ODIN_INSPECTOR
        [FoldoutGroup("Raw Archetype Scores")]
        [PropertyTooltip("Moderate efficiency (~55%), longer sessions, steady win rate (~60%).\n" +
                          "Score = EfficiencyPeak*0.3 + DurationBonus*0.3 + WinRatePeak*0.25 + MoveCount*0.15")]
        [ProgressBar(0f, 1f, 0.3f, 0.6f, 1f)]
        [LabelText("Careful Thinker")]
#endif
        public float CarefulThinkerScore;

#if ODIN_INSPECTOR
        [FoldoutGroup("Raw Archetype Scores")]
        [PropertyTooltip("Low efficiency, low win rate, but possibly improving over time.\n" +
                          "Score = (1-AvgEff)*0.3 + (1-WinRate)*0.3 + EffTrend*0.2 + SessionPenalty*0.2")]
        [ProgressBar(0f, 1f, 1f, 0.8f, 0.2f)]
        [LabelText("Struggling Learner")]
#endif
        public float StrugglingLearnerScore;

#if ODIN_INSPECTOR
        [FoldoutGroup("Raw Archetype Scores")]
        [PropertyTooltip("High booster usage with low efficiency. Wins through items, not skill.\n" +
                          "Score = BoosterRate*0.5 + WasteSignal*0.25 + (WinRate>0.5 && Eff<0.4)*0.25")]
        [ProgressBar(0f, 1f, 0.9f, 0.5f, 0.9f)]
        [LabelText("Booster Dependent")]
#endif
        public float BoosterDependentScore;

#if ODIN_INSPECTOR
        [FoldoutGroup("Raw Archetype Scores")]
        [PropertyTooltip("Declining session duration, declining efficiency, low recent win rate, long gaps.\n" +
                          "Score = -DurationTrend*0.3 + -EffTrend*0.3 + (1-RecentWR)*0.25 + DaysSince*0.15")]
        [ProgressBar(0f, 1f, 1f, 0.3f, 0.2f)]
        [LabelText("Churn Risk")]
#endif
        public float ChurnRiskScore;

#if ODIN_INSPECTOR
        private Color GetPrimaryColor()
        {
            switch (Primary)
            {
                case PlayerArchetype.SpeedRunner: return new Color(0.2f, 0.9f, 0.3f);
                case PlayerArchetype.CarefulThinker: return new Color(0.3f, 0.6f, 1f);
                case PlayerArchetype.StrugglingLearner: return new Color(1f, 0.8f, 0.2f);
                case PlayerArchetype.BoosterDependent: return new Color(0.9f, 0.5f, 0.9f);
                case PlayerArchetype.ChurnRisk: return new Color(1f, 0.3f, 0.2f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }
#endif
    }
}
