namespace Cadence
{
    /// <summary>
    /// Streaming mean + variance calculator using Welford's algorithm.
    /// Zero allocations, O(1) per sample.
    /// </summary>
    public struct RunningAverage
    {
        private int _count;
        private double _mean;
        private double _m2;

        /// <summary>Number of samples added so far.</summary>
        public int Count => _count;

        /// <summary>Current running mean of all added samples.</summary>
        public float Mean => (float)_mean;

        /// <summary>Sample variance (Bessel-corrected). Returns 0 if fewer than 2 samples.</summary>
        public float Variance => _count < 2 ? 0f : (float)(_m2 / (_count - 1));

        /// <summary>
        /// Adds a new sample and updates the running mean and variance in O(1).
        /// </summary>
        /// <param name="value">The sample value to incorporate.</param>
        public void Add(float value)
        {
            _count++;
            double delta = value - _mean;
            _mean += delta / _count;
            double delta2 = value - _mean;
            _m2 += delta * delta2;
        }

        /// <summary>
        /// Resets the calculator to its initial state (zero samples).
        /// </summary>
        public void Reset()
        {
            _count = 0;
            _mean = 0;
            _m2 = 0;
        }
    }
}
