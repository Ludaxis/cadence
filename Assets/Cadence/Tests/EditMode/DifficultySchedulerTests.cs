using NUnit.Framework;
using Cadence;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class DifficultySchedulerTests
    {
        private SawtoothCurveConfig _config;
        private DifficultyScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SawtoothCurveConfig>();
            _config.Period = 10;
            _config.Amplitude = 0.3f;
            _config.ReliefDepth = 0.15f;
            _config.RampStyle = RampStyle.Linear;
            _config.BossLevelOffset = -1; // Boss at position 9
            _config.BreatherLevelOffset = 0; // Breather at position 0
            _config.BaselineDriftPerCycle = 0.02f;
            _config.CurveShape = new AnimationCurve(); // Empty = use RampStyle

            _scheduler = new DifficultyScheduler(_config);
        }

        [Test]
        public void BossPosition_DetectedCorrectly()
        {
            // Boss at position 9 (period 10, offset -1)
            var type = _scheduler.GetSuggestedLevelType(9);
            Assert.AreEqual(LevelType.Boss, type);

            // Second cycle boss at position 19
            type = _scheduler.GetSuggestedLevelType(19);
            Assert.AreEqual(LevelType.Boss, type);
        }

        [Test]
        public void BreatherPosition_DetectedCorrectly()
        {
            // Breather at position 0 (offset 0)
            var type = _scheduler.GetSuggestedLevelType(0);
            Assert.AreEqual(LevelType.Breather, type);

            // Second cycle breather at position 10
            type = _scheduler.GetSuggestedLevelType(10);
            Assert.AreEqual(LevelType.Breather, type);
        }

        [Test]
        public void StandardPositions_ReturnStandard()
        {
            for (int i = 1; i <= 8; i++)
            {
                var type = _scheduler.GetSuggestedLevelType(i);
                Assert.AreEqual(LevelType.Standard, type, $"Position {i} should be Standard");
            }
        }

        [Test]
        public void BossMultiplier_IsHighest()
        {
            float bossMultiplier = _scheduler.GetTargetMultiplier(9);
            float expectedBoss = 1f + _config.Amplitude; // 1.3
            Assert.AreEqual(expectedBoss, bossMultiplier, 0.01f,
                "Boss should get peak multiplier");
        }

        [Test]
        public void BreatherMultiplier_IsLowest()
        {
            float breatherMultiplier = _scheduler.GetTargetMultiplier(0);
            float expectedBreather = 1f - _config.ReliefDepth; // 0.85
            Assert.AreEqual(expectedBreather, breatherMultiplier, 0.01f,
                "Breather should get trough multiplier");
        }

        [Test]
        public void Multiplier_MonotonicallyIncreases_DuringRamp()
        {
            // Positions 1-8 should generally increase (linear ramp)
            float prev = _scheduler.GetTargetMultiplier(1);
            for (int i = 2; i <= 8; i++)
            {
                float curr = _scheduler.GetTargetMultiplier(i);
                Assert.GreaterOrEqual(curr, prev - 0.001f,
                    $"Multiplier at {i} should be >= multiplier at {i - 1}");
                prev = curr;
            }
        }

        [Test]
        public void BaselineDrift_IncreasesPerCycle()
        {
            // Same position in cycle 0 vs cycle 1
            float cycle0 = _scheduler.GetTargetMultiplier(5);
            float cycle1 = _scheduler.GetTargetMultiplier(15);
            float diff = cycle1 - cycle0;

            Assert.Greater(diff, 0f, "Second cycle should have higher baseline");
            Assert.AreEqual(_config.BaselineDriftPerCycle, diff, 0.01f,
                "Drift should be approximately BaselineDriftPerCycle");
        }

        [Test]
        public void PeriodWrapping_WorksCorrectly()
        {
            // Level 0 and level 10 should both be breather
            Assert.AreEqual(LevelType.Breather, _scheduler.GetSuggestedLevelType(0));
            Assert.AreEqual(LevelType.Breather, _scheduler.GetSuggestedLevelType(10));

            // Level 9 and level 19 should both be boss
            Assert.AreEqual(LevelType.Boss, _scheduler.GetSuggestedLevelType(9));
            Assert.AreEqual(LevelType.Boss, _scheduler.GetSuggestedLevelType(19));
        }

        [Test]
        public void AmplitudeBounds_Respected()
        {
            // All multipliers should be within a reasonable range
            for (int i = 0; i < 30; i++)
            {
                float m = _scheduler.GetTargetMultiplier(i);
                Assert.Greater(m, 0.5f, $"Multiplier at {i} too low: {m}");
                Assert.Less(m, 2f, $"Multiplier at {i} too high: {m}");
            }
        }

        [Test]
        public void GetCurvePreview_ReturnsCorrectCount()
        {
            var points = _scheduler.GetCurvePreview(0, 20);
            Assert.AreEqual(20, points.Length);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(i, points[i].LevelIndex);
                Assert.AreEqual(_scheduler.GetTargetMultiplier(i), points[i].Multiplier, 0.001f);
                Assert.AreEqual(_scheduler.GetSuggestedLevelType(i), points[i].SuggestedType);
            }
        }

        [Test]
        public void SCurve_RampStyle()
        {
            _config.RampStyle = RampStyle.SCurve;
            var scheduler = new DifficultyScheduler(_config);

            // SCurve should produce values between breather and boss
            float mid = scheduler.GetTargetMultiplier(5);
            float breather = scheduler.GetTargetMultiplier(0);
            Assert.Greater(mid, breather, "Mid-cycle should be higher than breather");
        }

        [Test]
        public void NullConfig_ReturnsDefaultMultiplier()
        {
            var scheduler = new DifficultyScheduler(null);
            Assert.AreEqual(1f, scheduler.GetTargetMultiplier(0));
            Assert.AreEqual(1f, scheduler.GetTargetMultiplier(100));
        }

        [Test]
        public void NegativeIndex_ReturnsDefaultMultiplier()
        {
            Assert.AreEqual(1f, _scheduler.GetTargetMultiplier(-1));
        }
    }
}
