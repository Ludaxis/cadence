using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Snapshot of the current flow detection state. Updated every tick by the Flow Detector.
    /// </summary>
    [Serializable]
    public struct FlowReading
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Current classified flow state. See FlowState enum for thresholds.")]
        [EnumToggleButtons]
#endif
        public FlowState State;

#if ODIN_INSPECTOR
        [PropertyTooltip("Confidence in the current state classification (0-1).\n" +
                          "During warmup (< WarmupMoves): max 0.5.\n" +
                          "After warmup: 0.5 + HysteresisCount * 0.1, capped at 0.8.")]
        [ProgressBar(0f, 1f, 0.5f, 0.5f, 0.5f)]
#endif
        public float Confidence;

#if ODIN_INSPECTOR
        [PropertyTooltip("Tempo consistency score (0-1). High = steady pacing, Low = erratic.\n" +
                          "Formula: 1 - (StdDev / Mean) * 0.5 (coefficient of variation).")]
        [ProgressBar(0f, 1f, 0.3f, 0.6f, 1f)]
        [LabelText("Tempo")]
#endif
        public float TempoScore;

#if ODIN_INSPECTOR
        [PropertyTooltip("Move efficiency score (0-1). Ratio of optimal moves in the sliding window.\n" +
                          "Directly feeds Boredom (> 0.85) and Anxiety (< 0.3) detection.")]
        [ProgressBar(0f, 1f, 0.2f, 0.9f, 0.3f)]
        [LabelText("Efficiency")]
#endif
        public float EfficiencyScore;

#if ODIN_INSPECTOR
        [PropertyTooltip("Engagement score (0-1). High = active progress, Low = many pauses/rejections.\n" +
                          "Fed by progress deltas (1.0), pauses (0.0), and rejected inputs (0.0).")]
        [ProgressBar(0f, 1f, 0.9f, 0.5f, 0.2f)]
        [LabelText("Engagement")]
#endif
        public float EngagementScore;

#if ODIN_INSPECTOR
        [PropertyTooltip("Elapsed time since session start, in seconds.")]
        [SuffixLabel("sec", Overlay = true)]
        [ReadOnly]
#endif
        public float SessionTime;
    }
}
