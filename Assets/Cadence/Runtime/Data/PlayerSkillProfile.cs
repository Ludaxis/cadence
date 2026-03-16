using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Persistent player skill profile using the Glicko-2 rating system.
    /// Updated after each session. Drives adjustment rule decisions.
    /// </summary>
    [Serializable]
    public class PlayerSkillProfile
    {
        // ───────────────────── Glicko-2 State ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Glicko-2 Rating")]
        [InfoBox(
            "Glicko-2 rating system tracks player skill with uncertainty.\n" +
            "Rating 1500 = average. Deviation shrinks as confidence grows.")]
        [PropertyTooltip("Estimated skill rating on the Glicko-2 scale.\n" +
                          "1500 = average new player.\n" +
                          "<1500 = below average, >1500 = above average.\n" +
                          "Updated after each session via the full 5-step Glicko-2 algorithm.")]
        [SuffixLabel("rating", Overlay = true)]
#endif
        public float Rating = DefaultRating;

#if ODIN_INSPECTOR
        [BoxGroup("Glicko-2 Rating")]
        [PropertyTooltip("Rating deviation (uncertainty). Lower = more confident.\n" +
                          "350 = brand new (0% confident).\n" +
                          "<100 = well-established player (>70% confident).\n\n" +
                          "Grows over time when player is inactive (time decay).")]
        [SuffixLabel("RD", Overlay = true)]
        [ProgressBar(0f, 350f, 1f, 0.5f, 0f, Segmented = false)]
#endif
        public float Deviation = DefaultDeviation;

#if ODIN_INSPECTOR
        [BoxGroup("Glicko-2 Rating")]
        [PropertyTooltip("Volatility measures how consistently the player performs.\n" +
                          "Low (0.03) = very consistent session-to-session.\n" +
                          "High (0.1+) = erratic performance, results swing wildly.")]
#endif
        public float Volatility = DefaultVolatility;

        // ───────────────────── Session Stats ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Session Statistics")]
        [PropertyTooltip("Total sessions the player has completed. Used for HasSufficientData check (>= 5).")]
#endif
        public int SessionsCompleted;

#if ODIN_INSPECTOR
        [BoxGroup("Session Statistics")]
        [PropertyTooltip("Cumulative total moves across all sessions.")]
#endif
        public int TotalMoves;

#if ODIN_INSPECTOR
        [BoxGroup("Session Statistics")]
        [PropertyTooltip("Running average of the model-facing session efficiency across all sessions.\n" +
                          "Uses EffectiveEfficiency01 when resource.efficiency is present, otherwise MoveEfficiency.\n" +
                          "0.0 = never efficient, 1.0 = always efficient.\n" +
                          "Used by FlowChannelRule and StreakDamperRule.")]
        [ProgressBar(0f, 1f, 0.2f, 0.8f, 0.2f)]
#endif
        public float AverageEfficiency;

#if ODIN_INSPECTOR
        [BoxGroup("Session Statistics")]
        [PropertyTooltip("Running average of session outcomes (0 = always lose, 1 = always win).\n" +
                          "This IS the win rate used by FlowChannelRule to check against the target band.\n" +
                          "Win = 1.0, Lose = 0.0.")]
        [ProgressBar(0f, 1f, 0.3f, 0.6f, 1f)]
        [LabelText("Win Rate (AverageOutcome)")]
#endif
        public float AverageOutcome;

#if ODIN_INSPECTOR
        [BoxGroup("Session Statistics")]
        [PropertyTooltip("UTC ticks of the last completed session.\n" +
                          "Used by Glicko-2 time decay: deviation grows by DeviationDecayPerDay for each day since.")]
        [ReadOnly]
#endif
        public long LastSessionUtcTicks;

        // ───────────────────── History ─────────────────────

#if ODIN_INSPECTOR
        [FoldoutGroup("Session History")]
        [InfoBox("Rolling buffer of recent sessions. Newest first.\n" +
                 "Used by StreakDamperRule (win/loss streaks) and PlayerProfiler (archetype classification).")]
        [PropertyTooltip("Recent session history entries (max " + nameof(MaxHistoryEntries) + " = 20).")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false)]
#endif
        public List<SessionHistoryEntry> RecentHistory = new List<SessionHistoryEntry>();

        // ───────────────────── Computed ─────────────────────

#if ODIN_INSPECTOR
        [BoxGroup("Computed")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("Confidence = 1 - (Deviation / 350). Range 0-1.\n" +
                          "0.0 = no confidence (Deviation = 350).\n" +
                          "1.0 = full confidence (Deviation = 0).")]
        [ProgressBar(0f, 1f, 0.2f, 0.9f, 0.3f)]
        [LabelText("Confidence")]
#endif
        public float Confidence01 => 1f - Mathf.Clamp01(Deviation / DefaultDeviation);

#if ODIN_INSPECTOR
        [BoxGroup("Computed")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("True when SessionsCompleted >= 5. FlowChannelRule requires this to activate.")]
        [LabelText("Has Sufficient Data")]
#endif
        public bool HasSufficientData => SessionsCompleted >= MinSessionsForConfidence;

        // ───────────────────── Constants ─────────────────────

        public const int MinSessionsForConfidence = 5;
        public const int MaxHistoryEntries = 20;
        public const float DefaultRating = 1500f;
        public const float DefaultDeviation = 350f;
        public const float DefaultVolatility = 0.06f;
    }

    /// <summary>
    /// Compact record of a single completed session, stored in PlayerSkillProfile.RecentHistory.
    /// </summary>
    [Serializable]
    public struct SessionHistoryEntry
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Model-facing efficiency for this session. Usually MoveEfficiency, or EffectiveEfficiency01 when resource.efficiency enriches the session.")]
        [ProgressBar(0f, 1f, 0.2f, 0.8f, 0.2f)]
#endif
        public float Efficiency;

#if ODIN_INSPECTOR
        [PropertyTooltip("Session outcome: 1.0 = Win, 0.0 = Lose.")]
#endif
        public float Outcome;

#if ODIN_INSPECTOR
        [PropertyTooltip("Session duration in seconds.")]
        [SuffixLabel("sec", Overlay = true)]
#endif
        public float Duration;

#if ODIN_INSPECTOR
        [PropertyTooltip("Total moves executed in this session.")]
#endif
        public int Moves;

#if ODIN_INSPECTOR
        [PropertyTooltip("UTC timestamp of this session (DateTime.UtcNow.Ticks).")]
        [ReadOnly]
#endif
        public long TimestampUtcTicks;

#if ODIN_INSPECTOR
        [PropertyTooltip("LevelType as byte. Cast to (LevelType) for the enum value.")]
#endif
        public byte LevelTypeByte;
    }
}
