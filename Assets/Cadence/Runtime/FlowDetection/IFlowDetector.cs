using System;

namespace Cadence
{
    /// <summary>
    /// Performs real-time flow-state detection by analyzing recent signals each frame.
    /// Produces a <see cref="FlowReading"/> indicating whether the player is in flow, bored, or frustrated.
    /// </summary>
    public interface IFlowDetector
    {
        /// <summary>
        /// Returns the most recent flow reading computed by <see cref="Tick"/>.
        /// </summary>
        FlowReading CurrentReading { get; }

        /// <summary>
        /// Advances the flow detector by one frame, analyzing recent signals from the ring buffer.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        /// <param name="recentSignals">Ring buffer of the most recent signals from the current session.</param>
        void Tick(float deltaTime, SignalRingBuffer recentSignals);

        /// <summary>
        /// Resets all internal windows and state. Called when a new session begins.
        /// </summary>
        void Reset();

        /// <summary>
        /// Raised when the flow state transitions (e.g., from Flow to Frustrated).
        /// Not raised for sub-state score changes within the same state.
        /// </summary>
        event Action<FlowReading> OnFlowStateChanged;
    }
}
