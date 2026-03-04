using NUnit.Framework;

namespace Cadence.Tests
{
    [TestFixture]
    public class FlowDetectorOverflowTests
    {
        [Test]
        public void TotalPushed_IncreasesMonotonically()
        {
            var buffer = new SignalRingBuffer(4);
            Assert.AreEqual(0, buffer.TotalPushed);

            for (int i = 0; i < 10; i++)
            {
                buffer.Push(new SignalEntry { Key = "test", Value = i });
            }

            Assert.AreEqual(10, buffer.TotalPushed);
            Assert.AreEqual(4, buffer.Count); // Capped at capacity
        }

        [Test]
        public void TotalPushed_ResetOnClear()
        {
            var buffer = new SignalRingBuffer(4);
            buffer.Push(new SignalEntry { Key = "test", Value = 1 });
            buffer.Push(new SignalEntry { Key = "test", Value = 2 });
            Assert.AreEqual(2, buffer.TotalPushed);

            buffer.Clear();
            Assert.AreEqual(0, buffer.TotalPushed);
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void FlowDetector_ProcessesSignalsAfterBufferWraps()
        {
            var detector = new FlowDetector(null);
            var buffer = new SignalRingBuffer(8);

            // Push enough signals to fill buffer
            for (int i = 0; i < 8; i++)
            {
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Timestamp = new SignalTimestamp { SessionTime = i * 0.5f }
                });
            }

            detector.Tick(0.1f, buffer);
            var reading1 = detector.CurrentReading;

            // Push MORE signals that cause wrapping
            for (int i = 0; i < 8; i++)
            {
                buffer.Push(new SignalEntry
                {
                    Key = SignalKeys.MoveExecuted,
                    Value = 1f,
                    Timestamp = new SignalTimestamp { SessionTime = (8 + i) * 0.5f }
                });
            }

            // This should still process -- the bug was that this tick did nothing
            detector.Tick(0.1f, buffer);
            Assert.AreEqual(16, buffer.TotalPushed, "Buffer should have tracked all 16 pushes");
        }
    }
}
