using System;

namespace Cadence
{
    /// <summary>
    /// Collects and stores gameplay signals during an active session.
    /// Signals are appended to the current <see cref="SignalBatch"/> and pushed into the <see cref="SignalRingBuffer"/>
    /// for real-time flow detection.
    /// </summary>
    public interface ISignalCollector
    {
        /// <summary>
        /// Records a signal with the given key, value, tier, and optional move index.
        /// The signal is timestamped, added to the batch, and pushed into the ring buffer.
        /// </summary>
        /// <param name="key">Signal key from <see cref="SignalKeys"/> constants.</param>
        /// <param name="value">Signal value; meaning varies by key.</param>
        /// <param name="tier">Priority tier for processing order.</param>
        /// <param name="moveIndex">Sequential move index (1-based), or -1 if not move-related.</param>
        void Record(string key, float value, SignalTier tier, int moveIndex = -1);

        /// <summary>
        /// Returns the signal batch for the current session, containing all recorded entries.
        /// </summary>
        SignalBatch CurrentBatch { get; }

        /// <summary>
        /// Returns the fixed-size ring buffer holding the most recent signals for real-time analysis.
        /// </summary>
        SignalRingBuffer RecentSignals { get; }

        /// <summary>
        /// Total number of signals recorded in the current session.
        /// </summary>
        int TotalSignalCount { get; }

        /// <summary>
        /// Resets the collector for a new session, clearing the batch and ring buffer.
        /// </summary>
        /// <param name="levelId">Level identifier for the new session.</param>
        /// <param name="sessionStartTime">Start time for timestamp calculation.</param>
        void Reset(string levelId, float sessionStartTime);

        /// <summary>
        /// Raised immediately after a signal is recorded, before any further processing.
        /// </summary>
        event Action<SignalEntry> OnSignalRecorded;
    }
}
