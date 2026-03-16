using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Aggregated summary of a completed session. Computed by SessionAnalyzer from raw signals.
    /// Feeds Glicko-2 player model updates and adjustment rule evaluation.
    /// </summary>
    [Serializable]
    public struct SessionSummary
    {
        // ───────────────────── Outcome ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Outcome")]
        [PropertyTooltip("Win, Lose, or Abandoned. Win = outcome > 0.5, Abandoned = outcome < -0.5.")]
#endif
        public SessionOutcome Outcome;

#if ODIN_INSPECTOR
        [BoxGroup("Outcome")]
        [PropertyTooltip("Level type played. Determines which LevelTypeConfig to apply.")]
#endif
        public LevelType LevelType;

#if ODIN_INSPECTOR
        [BoxGroup("Outcome")]
        [PropertyTooltip("Total session duration in seconds.")]
        [SuffixLabel("sec", Overlay = true)]
#endif
        public float Duration;

#if ODIN_INSPECTOR
        [BoxGroup("Outcome")]
        [PropertyTooltip("Total moves executed during this session.")]
#endif
        public int TotalMoves;

#if ODIN_INSPECTOR
        [BoxGroup("Outcome")]
        [PropertyTooltip("Total raw signals collected (all tiers).")]
#endif
        public int TotalSignals;

        // ───────────────────── Tier 0: Decision Quality ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [InfoBox("Core gameplay performance metrics derived from move signals.")]
        [PropertyTooltip("Ratio of optimal moves to total moves (0-1).\n" +
                          "Formula: optimal_moves / total_moves\n" +
                          "Feeds: SkillScore (70% weight), Glicko-2 opponent rating adjustment.")]
        [ProgressBar(0f, 1f, 0.2f, 0.8f, 0.2f)]
#endif
        public float MoveEfficiency;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("Optional raw-input accuracy aggregate (0-1).\n" +
                          "Averaged from input.accuracy signals when present.\n" +
                          "Feeds: SkillScore enrichment, FrustrationScore enrichment.")]
        [ProgressBar(0f, 1f)]
#endif
        public float InputAccuracy01;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("True when one or more input.accuracy signals were recorded this session.")]
#endif
        public bool HasInputAccuracy;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("Optional resource-efficiency aggregate (0-1).\n" +
                          "Averaged from resource.efficiency signals when present.\n" +
                          "Feeds: EffectiveEfficiency01 -> SkillScore, Glicko-2, profiler trends.")]
        [ProgressBar(0f, 1f)]
#endif
        public float ResourceEfficiency01;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("True when one or more resource.efficiency signals were recorded this session.")]
#endif
        public bool HasResourceEfficiency;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("Effective decision efficiency used by the player model and derived skill score.\n" +
                          "If resource.efficiency is present: (MoveEfficiency + ResourceEfficiency01) * 0.5.\n" +
                          "Otherwise: MoveEfficiency.")]
        [ProgressBar(0f, 1f)]
#endif
        public float EffectiveEfficiency01;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("Ratio of wasted moves to total moves (0-1).\n" +
                          "Formula: total_waste / total_moves\n" +
                          "Feeds: FrustrationScore (30% weight).")]
        [ProgressBar(0f, 1f, 1f, 0.3f, 0.2f)]
#endif
        public float WasteRatio;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 0 — Decision Quality")]
        [PropertyTooltip("Micro-progress per move (0-1).\n" +
                          "Formula: total_progress_delta / total_moves\n" +
                          "Feeds: Flow Detector engagement window.")]
#endif
        public float ProgressRate;

        // ───────────────────── Tier 1: Behavioral Tempo ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 1 — Behavioral Tempo")]
        [InfoBox("Pacing and timing metrics derived from move timestamps.")]
        [PropertyTooltip("Average time between consecutive moves (Welford running mean).\n" +
                          "Feeds: EngagementScore (via TempoConsistency).")]
        [SuffixLabel("sec", Overlay = true)]
#endif
        public float MeanInterMoveInterval;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 1 — Behavioral Tempo")]
        [PropertyTooltip("Variance of inter-move intervals (Welford running variance).\n" +
                          "High variance = erratic pacing = struggling player.\n" +
                          "Feeds: FrustrationScore (25% weight), EngagementScore (60% weight via TempoConsistency).")]
#endif
        public float InterMoveVariance;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 1 — Behavioral Tempo")]
        [PropertyTooltip("Time before first action in the session.\n" +
                          "Long hesitation = confusion or anxiety.")]
        [SuffixLabel("sec", Overlay = true)]
#endif
        public float HesitationTime;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 1 — Behavioral Tempo")]
        [PropertyTooltip("Number of pause events during the session.\n" +
                          "Feeds: EngagementScore (40% weight via PausePenalty), FrustrationScore (20% weight).")]
#endif
        public int PauseCount;

        // ───────────────────── Tier 2: Strategic Pattern ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 2 — Strategic Pattern")]
        [InfoBox("Booster and strategy usage patterns.")]
        [PropertyTooltip("Total power-ups / boosters used this session.\n" +
                          "High usage may indicate BoosterDependent archetype.\n" +
                          "Feeds: Player Profiler archetype classification.")]
#endif
        public int PowerUpsUsed;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 2 — Strategic Pattern")]
        [PropertyTooltip("Ratio of successful sequence matches to total checks (0-1).\n" +
                          "Feeds: SkillScore (30% weight).")]
        [ProgressBar(0f, 1f)]
#endif
        public float SequenceMatchRate;

        // ───────────────────── Tier 3: Retry & Meta ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 3 — Retry & Meta")]
        [InfoBox("Cross-session context: attempts, gaps, abandonment.")]
        [PropertyTooltip("How many times the player has attempted this level.\n" +
                          "High count = player stuck. Should feed FrustrationReliefRule scaling (planned).")]
#endif
        public int AttemptNumber;

#if ODIN_INSPECTOR
        [FoldoutGroup("Tier 3 — Retry & Meta")]
        [PropertyTooltip("Days since the player's last session.\n" +
                          "Used by Glicko-2 time decay (deviation grows with inactivity).\n" +
                          "Also feeds ChurnRisk archetype score.")]
        [SuffixLabel("days", Overlay = true)]
#endif
        public float SessionGapDays;

        // ───────────────────── Derived Scores ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Derived Scores")]
        [InfoBox(
            "Computed by SessionAnalyzer from the raw aggregates above.\n" +
            "These scores drive Glicko-2 updates and adjustment rules.")]
        [PropertyTooltip("SkillScore = MoveEfficiency * 0.7 + SequenceMatchRate * 0.3\n\n" +
                          "Uses EffectiveEfficiency01 as the efficiency input, and blends in input.accuracy\n" +
                          "when present before applying the 70/30 sequence split.\n\n" +
                          "How well the player solves levels relative to optimal play.\n" +
                          "Feeds: Glicko-2 update (via opponent rating shift), Flow Channel Rule.")]
        [ProgressBar(0f, 1f, 0.2f, 0.9f, 0.3f)]
        [LabelText("Skill Score")]
#endif
        public float SkillScore;

#if ODIN_INSPECTOR
        [TitleGroup("Derived Scores")]
        [PropertyTooltip("EngagementScore = TempoConsistency * 0.6 + PausePenalty * 0.4\n\n" +
                          "Where:\n" +
                          "  TempoConsistency = 1 - sqrt(InterMoveVariance) / 5\n" +
                          "  PausePenalty = 1 - PauseCount * 0.15\n\n" +
                          "How focused and engaged the player is during play.\n" +
                          "Feeds: Glicko-2 update, Flow Detection.")]
        [ProgressBar(0f, 1f, 0.3f, 0.6f, 1f)]
        [LabelText("Engagement Score")]
#endif
        public float EngagementScore;

#if ODIN_INSPECTOR
        [TitleGroup("Derived Scores")]
        [PropertyTooltip("FrustrationScore uses the default blend of waste, tempo variance, pauses,\n" +
                          "and low MoveEfficiency. If input.accuracy is present, the formula adds a\n" +
                          "10% inverse input-accuracy component and slightly rebalances the other weights.\n\n" +
                          "How overwhelmed or stuck the player is.\n" +
                          "Triggers FrustrationReliefRule when > 0.7.")]
        [ProgressBar(0f, 1f, 1f, 0.3f, 0.2f)]
        [LabelText("Frustration Score")]
#endif
        public float FrustrationScore;

        public float ModelEfficiency01 => HasResourceEfficiency ? EffectiveEfficiency01 : MoveEfficiency;
    }
}
