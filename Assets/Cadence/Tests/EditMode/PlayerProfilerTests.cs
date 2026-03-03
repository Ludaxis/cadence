using System.Collections.Generic;
using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class PlayerProfilerTests
    {
        private PlayerProfiler _profiler;

        [SetUp]
        public void SetUp()
        {
            _profiler = new PlayerProfiler();
        }

        [Test]
        public void Classify_EmptyProfile_ReturnsUnknown()
        {
            var profile = new PlayerSkillProfile();
            var session = new SessionSummary();

            var reading = _profiler.Classify(profile, session);

            Assert.AreEqual(PlayerArchetype.Unknown, reading.Primary);
            Assert.AreEqual(0f, reading.PrimaryConfidence);
        }

        [Test]
        public void Classify_NullProfile_ReturnsUnknown()
        {
            var reading = _profiler.Classify(null, new SessionSummary());
            Assert.AreEqual(PlayerArchetype.Unknown, reading.Primary);
        }

        [Test]
        public void Classify_InsufficientHistory_ReturnsUnknown()
        {
            var profile = new PlayerSkillProfile();
            profile.RecentHistory.Add(new SessionHistoryEntry { Efficiency = 0.9f, Outcome = 1f });
            profile.RecentHistory.Add(new SessionHistoryEntry { Efficiency = 0.8f, Outcome = 1f });

            var reading = _profiler.Classify(profile, new SessionSummary());
            Assert.AreEqual(PlayerArchetype.Unknown, reading.Primary);
        }

        [Test]
        public void Classify_SpeedRunner_HighEfficiencyFastWins()
        {
            var profile = CreateProfile(sessions: 10, efficiency: 0.9f, winRate: 0.9f,
                avgDuration: 30f, avgMoves: 15);

            var reading = _profiler.Classify(profile, new SessionSummary());

            Assert.AreEqual(PlayerArchetype.SpeedRunner, reading.Primary,
                $"Expected SpeedRunner but got {reading.Primary} " +
                $"(SR:{reading.SpeedRunnerScore:F2} CT:{reading.CarefulThinkerScore:F2} " +
                $"SL:{reading.StrugglingLearnerScore:F2})");
            Assert.Greater(reading.SpeedRunnerScore, 0.3f);
        }

        [Test]
        public void Classify_CarefulThinker_ModerateEfficiencyLongSessions()
        {
            var profile = CreateProfile(sessions: 10, efficiency: 0.55f, winRate: 0.6f,
                avgDuration: 150f, avgMoves: 45);

            var reading = _profiler.Classify(profile, new SessionSummary());

            Assert.AreEqual(PlayerArchetype.CarefulThinker, reading.Primary,
                $"Expected CarefulThinker but got {reading.Primary} " +
                $"(SR:{reading.SpeedRunnerScore:F2} CT:{reading.CarefulThinkerScore:F2} " +
                $"SL:{reading.StrugglingLearnerScore:F2})");
            Assert.Greater(reading.CarefulThinkerScore, 0.3f);
        }

        [Test]
        public void Classify_StrugglingLearner_LowEfficiencyLowWinRate()
        {
            var profile = CreateProfile(sessions: 8, efficiency: 0.2f, winRate: 0.15f,
                avgDuration: 90f, avgMoves: 30);
            profile.SessionsCompleted = 8;

            var reading = _profiler.Classify(profile, new SessionSummary());

            Assert.AreEqual(PlayerArchetype.StrugglingLearner, reading.Primary,
                $"Expected StrugglingLearner but got {reading.Primary} " +
                $"(SR:{reading.SpeedRunnerScore:F2} CT:{reading.CarefulThinkerScore:F2} " +
                $"SL:{reading.StrugglingLearnerScore:F2} CR:{reading.ChurnRiskScore:F2})");
            Assert.Greater(reading.StrugglingLearnerScore, 0.3f);
        }

        [Test]
        public void Classify_BoosterDependent_HighBoosterUsageLowEfficiency()
        {
            var profile = CreateProfile(sessions: 10, efficiency: 0.3f, winRate: 0.6f,
                avgDuration: 60f, avgMoves: 25);
            profile.SessionsCompleted = 20;

            var session = new SessionSummary
            {
                PowerUpsUsed = 8,
                WasteRatio = 0.5f
            };

            var reading = _profiler.Classify(profile, session);

            Assert.AreEqual(PlayerArchetype.BoosterDependent, reading.Primary,
                $"Expected BoosterDependent but got {reading.Primary} " +
                $"(BD:{reading.BoosterDependentScore:F2} SR:{reading.SpeedRunnerScore:F2})");
            Assert.Greater(reading.BoosterDependentScore, 0.3f);
        }

        [Test]
        public void Classify_ChurnRisk_DecliningMetrics()
        {
            // Create history with declining efficiency and duration
            var profile = new PlayerSkillProfile();
            profile.SessionsCompleted = 10;
            for (int i = 0; i < 10; i++)
            {
                float declineT = 1f - i / 9f;
                profile.RecentHistory.Add(new SessionHistoryEntry
                {
                    Efficiency = 0.6f * declineT + 0.05f, // 0.65 -> 0.05
                    Outcome = i < 3 ? 1f : 0f,            // Early wins, then all losses
                    Duration = 120f * declineT + 10f,      // 130 -> 10
                    Moves = (int)(40 * declineT) + 5
                });
            }

            var reading = _profiler.Classify(profile, new SessionSummary());

            Assert.AreEqual(PlayerArchetype.ChurnRisk, reading.Primary,
                $"Expected ChurnRisk but got {reading.Primary} " +
                $"(CR:{reading.ChurnRiskScore:F2} SL:{reading.StrugglingLearnerScore:F2})");
            Assert.Greater(reading.ChurnRiskScore, 0.3f);
        }

        [Test]
        public void Classify_AllScoresInValidRange()
        {
            var profile = CreateProfile(sessions: 10, efficiency: 0.5f, winRate: 0.5f,
                avgDuration: 60f, avgMoves: 20);

            var reading = _profiler.Classify(profile, new SessionSummary());

            Assert.GreaterOrEqual(reading.SpeedRunnerScore, 0f);
            Assert.LessOrEqual(reading.SpeedRunnerScore, 1f);
            Assert.GreaterOrEqual(reading.CarefulThinkerScore, 0f);
            Assert.LessOrEqual(reading.CarefulThinkerScore, 1f);
            Assert.GreaterOrEqual(reading.StrugglingLearnerScore, 0f);
            Assert.LessOrEqual(reading.StrugglingLearnerScore, 1f);
            Assert.GreaterOrEqual(reading.BoosterDependentScore, 0f);
            Assert.LessOrEqual(reading.BoosterDependentScore, 1f);
            Assert.GreaterOrEqual(reading.ChurnRiskScore, 0f);
            Assert.LessOrEqual(reading.ChurnRiskScore, 1f);
        }

        [Test]
        public void ArchetypeStrategy_SpeedRunnerGetsHigherScale()
        {
            float sr = ArchetypeAdjustmentStrategy.GetAdjustmentScaleModifier(
                PlayerArchetype.SpeedRunner);
            float std = ArchetypeAdjustmentStrategy.GetAdjustmentScaleModifier(
                PlayerArchetype.Unknown);
            Assert.Greater(sr, std);
        }

        [Test]
        public void ArchetypeStrategy_ChurnRiskBlocksUpward()
        {
            Assert.IsTrue(ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                PlayerArchetype.ChurnRisk));
            Assert.IsTrue(ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                PlayerArchetype.StrugglingLearner));
            Assert.IsFalse(ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                PlayerArchetype.SpeedRunner));
            Assert.IsFalse(ArchetypeAdjustmentStrategy.ShouldBlockUpwardAdjustment(
                PlayerArchetype.Unknown));
        }

        // --- Helpers ---

        private static PlayerSkillProfile CreateProfile(int sessions, float efficiency,
            float winRate, float avgDuration, int avgMoves)
        {
            var profile = new PlayerSkillProfile();
            profile.SessionsCompleted = sessions;
            profile.AverageEfficiency = efficiency;
            profile.AverageOutcome = winRate;

            int wins = Mathf.RoundToInt(sessions * winRate);
            for (int i = 0; i < sessions; i++)
            {
                profile.RecentHistory.Add(new SessionHistoryEntry
                {
                    Efficiency = efficiency + Random.Range(-0.05f, 0.05f),
                    Outcome = i < wins ? 1f : 0f,
                    Duration = avgDuration + Random.Range(-5f, 5f),
                    Moves = avgMoves + Random.Range(-3, 3)
                });
            }

            return profile;
        }
    }
}
