using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class GlickoSafetyTests
    {
        [Test]
        public void Deserialize_CorruptJson_DoesNotThrow()
        {
            var model = new GlickoPlayerModel(null);
            Assert.DoesNotThrow(() => model.Deserialize("{corrupt json data!!!}"));
            // Profile should be reset to defaults
            Assert.AreEqual(PlayerSkillProfile.DefaultRating, model.Profile.Rating, 0.01f);
        }

        [Test]
        public void Deserialize_EmptyString_DoesNotThrow()
        {
            var model = new GlickoPlayerModel(null);
            float originalRating = model.Profile.Rating;
            Assert.DoesNotThrow(() => model.Deserialize(""));
            Assert.AreEqual(originalRating, model.Profile.Rating, 0.01f);
        }

        [Test]
        public void Deserialize_NullString_DoesNotThrow()
        {
            var model = new GlickoPlayerModel(null);
            Assert.DoesNotThrow(() => model.Deserialize(null));
        }

        [Test]
        public void UpdateFromSession_NormalValues_DoNotProduceNaN()
        {
            var model = new GlickoPlayerModel(null);
            var summary = new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 0.8f,
                TotalMoves = 20,
                Duration = 60f
            };

            model.UpdateFromSession(summary);

            Assert.IsFalse(float.IsNaN(model.Profile.Rating), "Rating should not be NaN");
            Assert.IsFalse(float.IsNaN(model.Profile.Deviation), "Deviation should not be NaN");
            Assert.IsFalse(float.IsNaN(model.Profile.Volatility), "Volatility should not be NaN");
        }

        [Test]
        public void GlickoLoop_DoesNotInfiniteLoop()
        {
            // Extreme values that could cause convergence issues
            var config = ScriptableObject.CreateInstance<PlayerModelConfig>();
            config.InitialRating = 3000f;
            config.InitialDeviation = 350f;
            config.InitialVolatility = 0.5f;
            var model = new GlickoPlayerModel(config);

            var summary = new SessionSummary
            {
                Outcome = SessionOutcome.Win,
                MoveEfficiency = 1f,
                TotalMoves = 100
            };

            // Should complete without hanging
            Assert.DoesNotThrow(() => model.UpdateFromSession(summary));
        }
    }
}
