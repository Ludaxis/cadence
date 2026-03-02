using System;

namespace Cadence
{
    public interface ISignalCollector
    {
        void Record(string key, float value, SignalTier tier, int moveIndex = -1);
        SignalBatch CurrentBatch { get; }
        SignalRingBuffer RecentSignals { get; }
        int TotalSignalCount { get; }
        void Reset(string levelId, float sessionStartTime);
        event Action<SignalEntry> OnSignalRecorded;
    }
}
