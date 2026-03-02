using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class GlickoPlayerModelTests
    {
        private GlickoPlayerModel _model;

        [SetUp]
        public void SetUp()
        {
            var config = ScriptableObject.CreateInstance<PlayerModelConfig>();
            _model = new GlickoPlayerModel(config);
        }

        [Test]
        public void InitialProfile_HasDefaultValues()
        {
            Assert.AreEqual(1500f, _model.Profile.Rating, 0.01f);
            Assert.AreEqual(350f, _model.Profile.Deviation, 0.01f);
            Assert.AreEqual(0.06f, _model.Profile.Volatility, 0.001f);
            Assert.AreEqual(0, _model.Profile.SessionsCompleted);
        }

        [Test]
        public void UpdateFromSession_Win_IncreasesRating()
        {
            float initialRating = _model.Profile.Rating;

            var summary = new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.7f,
                TotalMoves = 20,
                Duration = 60f
            };

            _model.UpdateFromSession(summary);

            Assert.Greater(_model.Profile.Rating, initialRating);
        }

        [Test]
        public void UpdateFromSession_Loss_DecreasesRating()
        {
            float initialRating = _model.Profile.Rating;

            var summary = new SessionSummary
            {
                Outcome = SessionOutcome.Lose,
                MoveEfficiency = 0.3f,
                TotalMoves = 15,
                Duration = 45f
            };

            _model.UpdateFromSession(summary);

            Assert.Less(_model.Profile.Rating, initialRating);
        }

        [Test]
        public void UpdateFromSession_DecreasesDeviation()
        {
            float initialDeviation = _model.Profile.Deviation;

            var summary = new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.5f,
                TotalMoves = 10
            };

            _model.UpdateFromSession(summary);

            Assert.Less(_model.Profile.Deviation, initialDeviation);
        }

        [Test]
        public void UpdateFromSession_IncrementsSessionCount()
        {
            var summary = new SessionSummary { Outcome = SessionOutcome.Win, TotalMoves = 5 };

            _model.UpdateFromSession(summary);
            _model.UpdateFromSession(summary);

            Assert.AreEqual(2, _model.Profile.SessionsCompleted);
        }

        [Test]
        public void UpdateFromSession_TracksAverageEfficiency()
        {
            _model.UpdateFromSession(new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.8f
            });
            _model.UpdateFromSession(new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.6f
            });

            Assert.AreEqual(0.7f, _model.Profile.AverageEfficiency, 0.01f);
        }

        [Test]
        public void UpdateFromSession_RecordsHistory()
        {
            _model.UpdateFromSession(new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.8f,
                Duration = 30f,
                TotalMoves = 15
            });

            Assert.AreEqual(1, _model.Profile.RecentHistory.Count);
            Assert.AreEqual(0.8f, _model.Profile.RecentHistory[0].Efficiency, 0.01f);
        }

        [Test]
        public void PredictWinRate_HigherRatingMeansHigherPrediction()
        {
            // Default rating 1500 vs level difficulty 1500 → ~50%
            float balanced = _model.PredictWinRate(1500f);
            Assert.AreEqual(0.5f, balanced, 0.05f);

            // Easy level (low difficulty)
            float easy = _model.PredictWinRate(1200f);
            Assert.Greater(easy, 0.5f);

            // Hard level (high difficulty)
            float hard = _model.PredictWinRate(1800f);
            Assert.Less(hard, 0.5f);
        }

        [Test]
        public void ApplyTimeDecay_IncreasesDeviation()
        {
            // First reduce deviation via sessions
            for (int i = 0; i < 5; i++)
            {
                _model.UpdateFromSession(new SessionSummary
                {
                    Outcome = SessionOutcome.Win,
                    MoveEfficiency = 0.7f
                });
            }

            float deviationAfterSessions = _model.Profile.Deviation;

            _model.ApplyTimeDecay(10f); // 10 days inactive

            Assert.Greater(_model.Profile.Deviation, deviationAfterSessions);
        }

        [Test]
        public void ApplyTimeDecay_ClampedToMax()
        {
            _model.ApplyTimeDecay(1000f); // Extreme decay

            Assert.LessOrEqual(_model.Profile.Deviation, 350f);
        }

        [Test]
        public void Serialize_Deserialize_Roundtrip()
        {
            _model.UpdateFromSession(new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.75f,
                TotalMoves = 20,
                Duration = 60f
            });

            string json = _model.Serialize();

            var config = ScriptableObject.CreateInstance<PlayerModelConfig>();
            var model2 = new GlickoPlayerModel(config);
            model2.Deserialize(json);

            Assert.AreEqual(_model.Profile.Rating, model2.Profile.Rating, 0.01f);
            Assert.AreEqual(_model.Profile.Deviation, model2.Profile.Deviation, 0.01f);
            Assert.AreEqual(_model.Profile.SessionsCompleted, model2.Profile.SessionsCompleted);
        }

        [Test]
        public void Confidence_IncreasesWithSessions()
        {
            float initialConfidence = _model.Profile.Confidence01;

            for (int i = 0; i < 10; i++)
            {
                _model.UpdateFromSession(new SessionSummary
                {
                    Outcome = i % 2 == 0 ? SessionOutcome.Win : SessionOutcome.Lose,
                    MoveEfficiency = 0.5f
                });
            }

            Assert.Greater(_model.Profile.Confidence01, initialConfidence);
        }
    }
}
