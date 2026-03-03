using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// A single recorded signal in the ring buffer.
    /// Created by <c>IDDAService.RecordSignal()</c> and processed by FlowDetector and SessionAnalyzer.
    /// </summary>
    [Serializable]
    public struct SignalEntry
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Signal key from SignalKeys constants (e.g., \"move.executed\", \"tempo.pause\").")]
#endif
        public string Key;

#if ODIN_INSPECTOR
        [PropertyTooltip("Signal value. Meaning depends on the key:\n" +
                          "move.executed = 1.0 (event occurred)\n" +
                          "move.waste = 0-1 (waste magnitude)\n" +
                          "tempo.interval = seconds between moves")]
#endif
        public float Value;

#if ODIN_INSPECTOR
        [PropertyTooltip("Priority tier of this signal. Tier 0 = critical, Tier 5 = enrichment.")]
#endif
        public SignalTier Tier;

#if ODIN_INSPECTOR
        [PropertyTooltip("Sequential move index (1-based) when this signal was recorded. 0 = not move-related.")]
#endif
        public int MoveIndex;

#if ODIN_INSPECTOR
        [PropertyTooltip("When this signal was recorded (session time + frame number).")]
#endif
        public SignalTimestamp Timestamp;
    }

    /// <summary>
    /// Timestamp for a recorded signal.
    /// </summary>
    [Serializable]
    public struct SignalTimestamp
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Elapsed seconds since session start.")]
        [SuffixLabel("sec", Overlay = true)]
#endif
        public float SessionTime;

#if ODIN_INSPECTOR
        [PropertyTooltip("Unity frame number when the signal was recorded.")]
#endif
        public int FrameNumber;
    }
}
