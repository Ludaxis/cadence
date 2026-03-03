using System;

namespace Cadence
{
    /// <summary>
    /// Replays a recorded <see cref="SignalBatch"/> in real-time for debug visualization and sandbox testing.
    /// Signals are emitted in chronological order at their original session timestamps.
    /// </summary>
    public interface ISignalReplay
    {
        /// <summary>
        /// Loads a signal batch for playback. Resets playback position to the beginning.
        /// </summary>
        /// <param name="batch">The recorded signal batch to replay.</param>
        void Load(SignalBatch batch);

        /// <summary>
        /// Returns <c>true</c> if playback is currently advancing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Current playback position in session-time seconds.
        /// </summary>
        float PlaybackTime { get; }

        /// <summary>
        /// Total duration of the loaded batch in session-time seconds.
        /// </summary>
        float TotalDuration { get; }

        /// <summary>
        /// Zero-based index of the next signal entry to be replayed.
        /// </summary>
        int CurrentIndex { get; }

        /// <summary>
        /// Total number of signal entries in the loaded batch.
        /// </summary>
        int TotalEntries { get; }

        /// <summary>
        /// Begins or resumes playback at the given speed multiplier.
        /// </summary>
        /// <param name="speed">Playback speed multiplier (1.0 = real-time).</param>
        void Play(float speed = 1f);

        /// <summary>
        /// Pauses playback at the current position without resetting.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops playback and resets the position to the beginning.
        /// </summary>
        void Stop();

        /// <summary>
        /// Jumps playback to the specified session time. Signals before this time are skipped.
        /// </summary>
        /// <param name="sessionTime">Target position in session-time seconds.</param>
        void SeekTo(float sessionTime);

        /// <summary>
        /// Raised each time a signal entry is replayed during playback.
        /// </summary>
        event Action<SignalEntry> OnSignalReplayed;

        /// <summary>
        /// Raised when playback reaches the end of the loaded batch.
        /// </summary>
        event Action OnPlaybackComplete;
    }
}
