using System;

namespace Cadence
{
    /// <summary>
    /// Fixed-capacity circular buffer of <see cref="SignalEntry"/> instances.
    /// Used by <see cref="FlowDetector"/> for real-time sliding-window analysis.
    /// Index 0 is the most recent entry; all iteration methods are zero-allocation.
    /// </summary>
    public sealed class SignalRingBuffer
    {
        private readonly SignalEntry[] _buffer;
        private int _head;
        private int _count;
        private long _totalPushed;

        /// <summary>Maximum number of entries the buffer can hold.</summary>
        public int Capacity { get; }

        /// <summary>Current number of entries in the buffer (up to <see cref="Capacity"/>).</summary>
        public int Count => _count;

        /// <summary>Total number of entries ever pushed into the buffer (monotonically increasing).</summary>
        public long TotalPushed => _totalPushed;

        /// <summary>
        /// Creates a new ring buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum entries to retain. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is zero or negative.</exception>
        public SignalRingBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _buffer = new SignalEntry[capacity];
        }

        /// <summary>
        /// Pushes an entry into the buffer. If at capacity, the oldest entry is overwritten.
        /// </summary>
        /// <param name="entry">The signal entry to add.</param>
        public void Push(SignalEntry entry)
        {
            _buffer[_head] = entry;
            _totalPushed++;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        /// <summary>
        /// Get entry by age: index 0 = most recent, index Count-1 = oldest.
        /// </summary>
        public SignalEntry this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();
                int actualIndex = (_head - 1 - index + Capacity) % Capacity;
                return _buffer[actualIndex];
            }
        }

        /// <summary>
        /// Get the most recent entry.
        /// </summary>
        public SignalEntry Peek()
        {
            if (_count == 0) throw new InvalidOperationException("Buffer is empty.");
            return _buffer[(_head - 1 + Capacity) % Capacity];
        }

        /// <summary>
        /// Iterate from oldest to newest, invoking the callback for each entry.
        /// Zero-allocation iteration.
        /// </summary>
        public void ForEachOldestFirst(Action<SignalEntry> action)
        {
            if (_count == 0) return;
            int start = (_head - _count + Capacity) % Capacity;
            for (int i = 0; i < _count; i++)
            {
                action(_buffer[(start + i) % Capacity]);
            }
        }

        /// <summary>
        /// Count entries matching a key within the buffer. Zero-allocation.
        /// </summary>
        public int CountByKey(string key)
        {
            int result = 0;
            int start = (_head - _count + Capacity) % Capacity;
            for (int i = 0; i < _count; i++)
            {
                if (_buffer[(start + i) % Capacity].Key == key)
                    result++;
            }
            return result;
        }

        /// <summary>
        /// Sum values for entries matching a key. Zero-allocation.
        /// </summary>
        public float SumByKey(string key)
        {
            float sum = 0f;
            int start = (_head - _count + Capacity) % Capacity;
            for (int i = 0; i < _count; i++)
            {
                ref var entry = ref _buffer[(start + i) % Capacity];
                if (entry.Key == key)
                    sum += entry.Value;
            }
            return sum;
        }

        /// <summary>
        /// Removes all entries from the buffer.
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _count = 0;
            _totalPushed = 0;
        }
    }
}
