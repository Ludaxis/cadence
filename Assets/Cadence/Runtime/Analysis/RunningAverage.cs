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

        public int Count => _count;
        public float Mean => (float)_mean;
        public float Variance => _count < 2 ? 0f : (float)(_m2 / (_count - 1));

        public void Add(float value)
        {
            _count++;
            double delta = value - _mean;
            _mean += delta / _count;
            double delta2 = value - _mean;
            _m2 += delta * delta2;
        }

        public void Reset()
        {
            _count = 0;
            _mean = 0;
            _m2 = 0;
        }
    }
}
