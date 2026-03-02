using System;
using UnityEngine;

namespace Cadence
{
    public sealed class SignalCollector : ISignalCollector
    {
        private readonly SignalBatch _batch = new SignalBatch();
        private readonly SignalRingBuffer _ringBuffer;
        private float _sessionStartTime;

        public SignalBatch CurrentBatch => _batch;
        public SignalRingBuffer RecentSignals => _ringBuffer;
        public int TotalSignalCount => _batch.Count;

        public event Action<SignalEntry> OnSignalRecorded;

        public SignalCollector(int ringBufferCapacity = 512)
        {
            _ringBuffer = new SignalRingBuffer(ringBufferCapacity);
        }

        public void Reset(string levelId, float sessionStartTime)
        {
            _batch.Clear();
            _ringBuffer.Clear();
            _batch.LevelId = levelId;
            _batch.SessionStartTime = sessionStartTime;
            _sessionStartTime = sessionStartTime;
        }

        public void Record(string key, float value, SignalTier tier, int moveIndex = -1)
        {
            var entry = new SignalEntry
            {
                Key = key,
                Value = value,
                Tier = tier,
                MoveIndex = moveIndex,
                Timestamp = new SignalTimestamp
                {
                    SessionTime = Time.unscaledTime - _sessionStartTime,
                    FrameNumber = Time.frameCount
                }
            };

            _batch.Add(entry);
            _ringBuffer.Push(entry);
            OnSignalRecorded?.Invoke(entry);
        }
    }
}
