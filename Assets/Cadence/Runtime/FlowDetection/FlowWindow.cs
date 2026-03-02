namespace Cadence
{
    /// <summary>
    /// Fixed-size sliding window that computes mean and variance
    /// over the most recent N values. Zero-allocation after construction.
    /// </summary>
    public sealed class FlowWindow
    {
        private readonly float[] _values;
        private int _head;
        private int _count;
        private double _sum;
        private double _sumSq;

        public int Capacity { get; }
        public int Count => _count;
        public bool IsFull => _count >= Capacity;

        public float Mean => _count > 0 ? (float)(_sum / _count) : 0f;
        public float Variance
        {
            get
            {
                if (_count < 2) return 0f;
                double mean = _sum / _count;
                return (float)(_sumSq / _count - mean * mean);
            }
        }

        public FlowWindow(int capacity)
        {
            Capacity = capacity;
            _values = new float[capacity];
        }

        public void Push(float value)
        {
            if (_count >= Capacity)
            {
                // Remove oldest value from running sums
                float oldest = _values[_head];
                _sum -= oldest;
                _sumSq -= (double)oldest * oldest;
            }
            else
            {
                _count++;
            }

            _values[_head] = value;
            _sum += value;
            _sumSq += (double)value * value;
            _head = (_head + 1) % Capacity;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            _sum = 0;
            _sumSq = 0;
        }

        /// <summary>
        /// Compute a normalized score: 1.0 = consistent, 0.0 = erratic.
        /// Based on coefficient of variation.
        /// </summary>
        public float ConsistencyScore()
        {
            if (_count < 2) return 0.5f;
            float mean = Mean;
            if (mean <= 0.0001f) return 0.5f;
            float stdDev = UnityEngine.Mathf.Sqrt(UnityEngine.Mathf.Max(0f, Variance));
            float cv = stdDev / mean;
            // cv of 0 = perfect consistency (1.0), cv of 2+ = very erratic (0.0)
            return UnityEngine.Mathf.Clamp01(1f - cv * 0.5f);
        }
    }
}
