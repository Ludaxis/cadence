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

        [Test]
        public void Analyze_ExplicitIntervalsOverrideDerivedTimestamps()
        {
            var batch = new SignalBatch();

            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 0f, 0);
            AddSignal(batch, SignalKeys.MoveOptimal, 1f, 0f, 0);

            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 5f, 1);
            AddSignal(batch, SignalKeys.MoveOptimal, 1f, 5f, 1);
            AddSignal(batch, SignalKeys.InterMoveInterval, 1f, 5f, 1);

            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 11f, 2);
            AddSignal(batch, SignalKeys.MoveOptimal, 1f, 11f, 2);
            AddSignal(batch, SignalKeys.InterMoveInterval, 1f, 11f, 2);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(1f, summary.MeanInterMoveInterval, 0.01f);
            Assert.AreEqual(0f, summary.InterMoveVariance, 0.01f);
        }

        [Test]
        public void Analyze_WithoutExplicitIntervals_UsesMoveTimestamps()
        {
            var batch = new SignalBatch();

            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 0f, 0);
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 2f, 1);
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 5f, 2);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(2.5f, summary.MeanInterMoveInterval, 0.01f);
            Assert.Greater(summary.InterMoveVariance, 0f);
        }

        [Test]
        public void Analyze_InputAccuracy_ChangesSkillAndFrustration()
        {
            var batch = new SignalBatch();
            for (int i = 0; i < 4; i++)
            {
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i, i);
                AddSignal(batch, SignalKeys.MoveOptimal, i < 2 ? 1f : 0f, i, i);
                AddSignal(batch, SignalKeys.MoveWaste, 0.5f, i, i);
                AddSignal(batch, SignalKeys.InputAccuracy, 0.2f, i, i);
            }
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 4f);
            AddSignal(batch, SignalKeys.PauseTriggered, 1f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.IsTrue(summary.HasInputAccuracy);
            Assert.AreEqual(0.2f, summary.InputAccuracy01, 0.01f);
            Assert.AreEqual(0.30f, summary.SkillScore, 0.02f);
            Assert.AreEqual(0.379f, summary.FrustrationScore, 0.02f);
        }

        [Test]
        public void Analyze_ResourceEfficiency_ChangesEffectiveEfficiency()
        {
            var batch = new SignalBatch();
            for (int i = 0; i < 4; i++)
            {
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i, i);
                AddSignal(batch, SignalKeys.MoveOptimal, i < 2 ? 1f : 0f, i, i);
            }
            AddSignal(batch, SignalKeys.ResourceEfficiency, 0.9f, 1f);
            AddSignal(batch, SignalKeys.ResourceEfficiency, 0.7f, 2f);

            var summary = _analyzer.Analyze(batch);

            Assert.IsTrue(summary.HasResourceEfficiency);
            Assert.AreEqual(0.8f, summary.ResourceEfficiency01, 0.01f);
            Assert.AreEqual(0.65f, summary.EffectiveEfficiency01, 0.01f);
            Assert.AreEqual(0.455f, summary.SkillScore, 0.02f);
        }

        [Test]
        public void Analyze_LevelAbandoned_OverridesSessionOutcome()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 1f);
            AddSignal(batch, SignalKeys.LevelAbandoned, 1f, 2f);
            AddSignal(batch, SignalKeys.SessionOutcome, 1f, 3f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(SessionOutcome.Abandoned, summary.Outcome);
        }

        [Test]
        public void Analyze_WithParMoves_ComputesSkillIndex()
        {
            var batch = TestFixtureHelper.CreateBatchWithLevelParams(
                new System.Collections.Generic.Dictionary<string, float> { { "par_moves", 20f } });
            for (int i = 0; i < 20; i++)
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i * 1f, i);

            var summary = _analyzer.Analyze(batch);

            Assert.IsTrue(summary.HasSkillIndex);
            Assert.AreEqual(20, summary.ParMoves);
            Assert.AreEqual(1.0f, summary.SkillIndex, 0.01f);
        }

        [Test]
        public void Analyze_WithParMoves_MoreMoves_LowIndex()
        {
            var batch = TestFixtureHelper.CreateBatchWithLevelParams(
                new System.Collections.Generic.Dictionary<string, float> { { "par_moves", 20f } });
            for (int i = 0; i < 40; i++)
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i * 1f, i);

            var summary = _analyzer.Analyze(batch);

            Assert.IsTrue(summary.HasSkillIndex);
            Assert.AreEqual(0.5f, summary.SkillIndex, 0.01f);
        }

        [Test]
        public void Analyze_WithoutParMoves_NoSkillIndex()
        {
            var batch = new SignalBatch();
            for (int i = 0; i < 10; i++)
                AddSignal(batch, SignalKeys.MoveExecuted, 1f, i * 1f, i);

            var summary = _analyzer.Analyze(batch);

            Assert.IsFalse(summary.HasSkillIndex);
            Assert.AreEqual(0f, summary.SkillIndex, 0.01f);
        }

        [Test]
        public void Analyze_CountsUndos_ResetOnForwardMove()
        {
            var batch = new SignalBatch();
            // 3 undos in a row
            AddSignal(batch, SignalKeys.Undo, 1f, 1f);
            AddSignal(batch, SignalKeys.Undo, 1f, 2f);
            AddSignal(batch, SignalKeys.Undo, 1f, 3f);
            // Forward move resets streak
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 4f, 0);
            // 1 more undo
            AddSignal(batch, SignalKeys.Undo, 1f, 5f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(4, summary.UndoCount);
            Assert.AreEqual(3, summary.PeakUndoStreak);
        }

        [Test]
        public void Analyze_CountsFrustrationTriggers()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 0f, 0);
            AddSignal(batch, SignalKeys.FrustrationTrigger, 1f, 1f);
            AddSignal(batch, SignalKeys.FrustrationTrigger, 1f, 2f);
            AddSignal(batch, SignalKeys.FrustrationTrigger, 1f, 3f);

            var summary = _analyzer.Analyze(batch);

            Assert.AreEqual(3, summary.FrustrationTriggerCount);
        }

        [Test]
        public void Analyze_DetectsReplayPlayType()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.PlayType, SignalKeys.PlayTypeReplay, 0f);
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 1f, 0);

            var summary = _analyzer.Analyze(batch);

            Assert.IsTrue(summary.IsReplay);
        }

        [Test]
        public void Analyze_StartPlayType_NotReplay()
        {
            var batch = new SignalBatch();
            AddSignal(batch, SignalKeys.PlayType, SignalKeys.PlayTypeStart, 0f);
            AddSignal(batch, SignalKeys.MoveExecuted, 1f, 1f, 0);

            var summary = _analyzer.Analyze(batch);

            Assert.IsFalse(summary.IsReplay);
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
