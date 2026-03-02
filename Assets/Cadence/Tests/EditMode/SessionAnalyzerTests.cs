using NUnit.Framework;
using Cadence;

namespace Cadence.Tests
{
    [TestFixture]
    public class SessionAnalyzerTests
    {
        private SessionAnalyzer _analyzer;

        [SetUp]
        public void SetUp()
        {
            _analyzer = new SessionAnalyzer();
        }

        [Test]
        public void Analyze_EmptyBatch_ReturnsDefaultSummary()
        {
            var batch = new SignalBatch();
            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(0, summary.TotalMoves);
            Assert.AreEqual(0, summary.TotalSignals);
        }

        [Test]
        public void Analyze_CountsMoves()
        {
            var batch = CreateBatchWithMoves(10, 7);
            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(10, summary.TotalMoves);
        }

        [Test]
        public void Analyze_CalculatesEfficiency()
        {
            var batch = CreateBatchWithMoves(10, 7);
            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(0.7f, summary.MoveEfficiency, 0.01f);
        }

        [Test]
        public void Analyze_CalculatesWasteRatio()
        {
            var batch = new SignalBatch();
            for (int i = 0; i < 5; i++)
            {
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i * 1f);
                AddSignal(batch, SignalKeys.MoveWaste, 0.5f, i * 1f);
            }

            var summary = _analyzer.Analyze(batch);

            // Total waste = 2.5, Total resources = 5 moves
            Assert.AreEqual(0.5f, summary.WasteRatio, 0.01f);
        }

        [Test]
        public void Analyze_DetectsWinOutcome()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 0f);
            AddSignal(batch, SignalKeys.SessionOutcome, 1f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(SessionOutcome.Win, summary.Outcome);
        }

        [Test]
        public void Analyze_DetectsLoseOutcome()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.SessionOutcome, 0f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(SessionOutcome.Lose, summary.Outcome);
        }

        [Test]
        public void Analyze_DetectsAbandonOutcome()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.SessionOutcome, -1f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(SessionOutcome.Abandoned, summary.Outcome);
        }

        [Test]
        public void Analyze_CountsPauses()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 1f);
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 3f);
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(3, summary.PauseCount);
        }

        [Test]
        public void Analyze_CountsPowerUps()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.PowerUpUsed, 1f, 1f);
            AddSignal(batch, SignalKeys.PowerUpUsed, 1f, 3f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(2, summary.PowerUpsUsed);
        }

        [Test]
        public void Analyze_CalculatesDuration()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 0f);
            AddSignal(batch, SignalKeys.SessionEnded, 1f, 45.5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(45.5f, summary.Duration, 0.01f);
        }

        [Test]
        public void Analyze_DerivedScores_InRange()
        {
            var batch = CreateBatchWithMoves(20, 14);
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 10f);
            AddSignal(batch, SignalKeys.MoveWaste, 0.2f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.GreaterOrEqual(summary.SkillScore, 0f);
            Assert.LessOrEqual(summary.SkillScore, 1f);
            Assert.GreaterOrEqual(summary.EngagementScore, 0f);
            Assert.LessOrEqual(summary.EngagementScore, 1f);
            Assert.GreaterOrEqual(summary.FrustrationScore, 0f);
            Assert.LessOrEqual(summary.FrustrationScore, 1f);
        }

        // --- Helpers ---

        private static SignalBatch CreateBatchWithMoves(int totalMoves, int optimalMoves)
        {
            var batch = new SignalBatch();
            for (int i = 0; i < totalMoves; i++)
            {
                float time = i * 1.5f;
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, time, i);
                AddSignal(batch, SignalKeys.MoveOptimal, i < optimalMoves ? 1f : 0f, time, i);
            }
            return batch;
        }

        private static void AddSignal(SignalBatch batch, string key, float value,
            float sessionTime, int moveIndex = -1)
        {
            batch.Add(new SignalEntry
            {
                Key = key,
                Value = value,
                Tier = SignalTier.DecisionQuality,
                MoveIndex = moveIndex,
                Timestamp = new SignalTimestamp { SessionTime = sessionTime, FrameNumber = 0 }
            });
        }
    }
}
