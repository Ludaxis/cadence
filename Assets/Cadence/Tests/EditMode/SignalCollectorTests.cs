using NUnit.Framework;
using Cadence;

namespace Cadence.Tests
{
    [TestFixture]
    public class SignalCollectorTests
    {
        [Test]
        public void Record_AddsEntryToBatch()
        {
            var collector = new SignalCollector(64);
            collector.Reset("level_1", 0f);

            collector.Record(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);

            Assert.AreEqual(1, collector.TotalSignalCount);
            Assert.AreEqual(1, collector.CurrentBatch.Count);
        }

        [Test]
        public void Record_PushesToRingBuffer()
        {
            var collector = new SignalCollector(64);
            collector.Reset("level_1", 0f);

            collector.Record(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality);
            collector.Record(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality);

            Assert.AreEqual(2, collector.RecentSignals.Count);
        }

        [Test]
        public void Reset_ClearsBatchAndBuffer()
        {
            var collector = new SignalCollector(64);
            collector.Reset("level_1", 0f);
            collector.Record(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality);

            collector.Reset("level_2", 100f);

            Assert.AreEqual(0, collector.TotalSignalCount);
            Assert.AreEqual(0, collector.RecentSignals.Count);
            Assert.AreEqual("level_2", collector.CurrentBatch.LevelId);
        }

        [Test]
        public void OnSignalRecorded_FiresEvent()
        {
            var collector = new SignalCollector(64);
            collector.Reset("test", 0f);

            SignalEntry received = default;
            collector.OnSignalRecorded += e => received = e;

            collector.Record(SignalKeys.MoveWaste, 3.5f, SignalTier.DecisionQuality, 7);

            Assert.AreEqual(SignalKeys.MoveWaste, received.Key);
            Assert.AreEqual(3.5f, received.Value);
            Assert.AreEqual(7, received.MoveIndex);
        }

        [Test]
        public void RingBuffer_Wraps_WhenFull()
        {
            var buffer = new SignalRingBuffer(4);

            for (int i = 0; i < 6; i++)
            {
                buffer.Push(new SignalEntry { Key = $"s{i}", Value = i });
            }

            Assert.AreEqual(4, buffer.Count);
            // Most recent = s5 (index 0)
            Assert.AreEqual("s5", buffer[0].Key);
            Assert.AreEqual(5f, buffer[0].Value);
            // Oldest = s2 (index 3)
            Assert.AreEqual("s2", buffer[3].Key);
        }

        [Test]
        public void RingBuffer_CountByKey_ReturnsCorrectCount()
        {
            var buffer = new SignalRingBuffer(32);
            buffer.Push(new SignalEntry { Key = "a" });
            buffer.Push(new SignalEntry { Key = "b" });
            buffer.Push(new SignalEntry { Key = "a" });
            buffer.Push(new SignalEntry { Key = "c" });
            buffer.Push(new SignalEntry { Key = "a" });

            Assert.AreEqual(3, buffer.CountByKey("a"));
            Assert.AreEqual(1, buffer.CountByKey("b"));
            Assert.AreEqual(0, buffer.CountByKey("x"));
        }

        [Test]
        public void RingBuffer_SumByKey_ReturnsSumOfValues()
        {
            var buffer = new SignalRingBuffer(32);
            buffer.Push(new SignalEntry { Key = "w", Value = 1.5f });
            buffer.Push(new SignalEntry { Key = "w", Value = 2.5f });
            buffer.Push(new SignalEntry { Key = "x", Value = 100f });

            Assert.AreEqual(4f, buffer.SumByKey("w"), 0.001f);
        }

        [Test]
        public void RingBuffer_ForEachOldestFirst_CorrectOrder()
        {
            var buffer = new SignalRingBuffer(4);
            buffer.Push(new SignalEntry { Value = 1f });
            buffer.Push(new SignalEntry { Value = 2f });
            buffer.Push(new SignalEntry { Value = 3f });

            var values = new System.Collections.Generic.List<float>();
            buffer.ForEachOldestFirst(e => values.Add(e.Value));

            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(1f, values[0]);
            Assert.AreEqual(2f, values[1]);
            Assert.AreEqual(3f, values[2]);
        }
    }
}
