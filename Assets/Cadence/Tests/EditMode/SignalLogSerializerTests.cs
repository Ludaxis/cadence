using System.Collections.Generic;
using NUnit.Framework;

namespace Cadence.Tests
{
    [TestFixture]
    public class SignalLogSerializerTests
    {
        [Test]
        public void Serialize_RoundTripsLevelParametersAndConfidence()
        {
            var batch = new SignalBatch
            {
                LevelId = "level_6_v5",
                SessionStartTime = 123f,
                LevelParameters = new Dictionary<string, float>
                {
                    { "difficulty_score", 5f },
                    { "par_moves", 24f }
                }
            };
            batch.Add(new SignalEntry
            {
                Key = SignalKeys.MoveOptimal,
                Value = 0f,
                Confidence = 0.25f,
                HasConfidence = true,
                Tier = SignalTier.DecisionQuality,
                MoveIndex = 3,
                Timestamp = new SignalTimestamp { SessionTime = 4.5f, FrameNumber = 17 }
            });

            string json = SignalLogSerializer.Serialize(batch);
            var roundTrip = SignalLogSerializer.Deserialize(json);

            Assert.AreEqual(batch.LevelId, roundTrip.LevelId);
            Assert.AreEqual(5f, roundTrip.LevelParameters["difficulty_score"], 0.01f);
            Assert.AreEqual(24f, roundTrip.LevelParameters["par_moves"], 0.01f);
            Assert.AreEqual(1, roundTrip.Entries.Count);
            Assert.IsTrue(roundTrip.Entries[0].HasConfidence);
            Assert.AreEqual(0.25f, roundTrip.Entries[0].Confidence, 0.01f);
        }

        [Test]
        public void Deserialize_OldLogWithoutConfidence_DefaultsToFullConfidence()
        {
            const string json =
                "{\"LevelId\":\"legacy\",\"SessionStartTime\":1,\"Entries\":[{\"K\":\"move.optimal\",\"V\":0,\"T\":0,\"M\":1,\"ST\":2,\"F\":3}]}";

            var batch = SignalLogSerializer.Deserialize(json);

            Assert.AreEqual(1, batch.Entries.Count);
            Assert.IsFalse(batch.Entries[0].HasConfidence);
            Assert.AreEqual(1f, batch.Entries[0].Confidence, 0.01f);
        }
    }
}
