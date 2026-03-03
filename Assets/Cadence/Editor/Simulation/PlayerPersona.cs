using System;
using UnityEngine;

namespace Cadence.Editor
{
    [Serializable]
    public class PlayerPersona
    {
        public string Name;
        public Color Color;

        // Session behavior
        public float BaseWinRate;
        public float MeanMoveCount;
        public float MoveCountVariance;
        public float OptimalMoveRatio;
        public float MeanSessionTime;
        public float MeanInterMoveTime;
        public float BoosterUseRate;
        public float PauseRate;
        public float SkillGrowthRate;

        /// <summary>
        /// Effective win rate at a given level, accounting for skill growth over time.
        /// </summary>
        public float EffectiveWinRate(int levelIndex)
        {
            float growth = SkillGrowthRate * (levelIndex / 10f);
            return Mathf.Clamp01(BaseWinRate + growth);
        }

        public static PlayerPersona NewPlayer() => new PlayerPersona
        {
            Name = "New Player",
            Color = new Color(0.3f, 0.8f, 0.4f),
            BaseWinRate = 0.40f,
            MeanMoveCount = 20f,
            MoveCountVariance = 5f,
            OptimalMoveRatio = 0.35f,
            MeanSessionTime = 90f,
            MeanInterMoveTime = 3.0f,
            BoosterUseRate = 0.1f,
            PauseRate = 0.15f,
            SkillGrowthRate = 0.03f
        };

        public static PlayerPersona AverageJoe() => new PlayerPersona
        {
            Name = "Average Joe",
            Color = new Color(0.3f, 0.6f, 1f),
            BaseWinRate = 0.55f,
            MeanMoveCount = 18f,
            MoveCountVariance = 4f,
            OptimalMoveRatio = 0.50f,
            MeanSessionTime = 60f,
            MeanInterMoveTime = 2.0f,
            BoosterUseRate = 0.15f,
            PauseRate = 0.10f,
            SkillGrowthRate = 0.01f
        };

        public static PlayerPersona SkilledVeteran() => new PlayerPersona
        {
            Name = "Skilled Veteran",
            Color = new Color(1f, 0.85f, 0.2f),
            BaseWinRate = 0.85f,
            MeanMoveCount = 12f,
            MoveCountVariance = 2f,
            OptimalMoveRatio = 0.80f,
            MeanSessionTime = 40f,
            MeanInterMoveTime = 1.2f,
            BoosterUseRate = 0.02f,
            PauseRate = 0.02f,
            SkillGrowthRate = 0.002f
        };

        public static PlayerPersona SpeedRunner() => new PlayerPersona
        {
            Name = "Speed Runner",
            Color = new Color(1f, 0.4f, 0.8f),
            BaseWinRate = 0.70f,
            MeanMoveCount = 10f,
            MoveCountVariance = 2f,
            OptimalMoveRatio = 0.60f,
            MeanSessionTime = 25f,
            MeanInterMoveTime = 0.8f,
            BoosterUseRate = 0.0f,
            PauseRate = 0.0f,
            SkillGrowthRate = 0.005f
        };

        public static PlayerPersona StrugglingLearner() => new PlayerPersona
        {
            Name = "Struggling Learner",
            Color = new Color(1f, 0.5f, 0.2f),
            BaseWinRate = 0.25f,
            MeanMoveCount = 25f,
            MoveCountVariance = 8f,
            OptimalMoveRatio = 0.20f,
            MeanSessionTime = 120f,
            MeanInterMoveTime = 4.0f,
            BoosterUseRate = 0.30f,
            PauseRate = 0.25f,
            SkillGrowthRate = 0.015f
        };

        public static PlayerPersona ChurningPlayer() => new PlayerPersona
        {
            Name = "Churning Player",
            Color = new Color(0.7f, 0.2f, 0.2f),
            BaseWinRate = 0.20f,
            MeanMoveCount = 8f,
            MoveCountVariance = 3f,
            OptimalMoveRatio = 0.15f,
            MeanSessionTime = 30f,
            MeanInterMoveTime = 5.0f,
            BoosterUseRate = 0.05f,
            PauseRate = 0.30f,
            SkillGrowthRate = -0.01f
        };

        public static PlayerPersona[] AllPresets() => new[]
        {
            NewPlayer(),
            AverageJoe(),
            SkilledVeteran(),
            SpeedRunner(),
            StrugglingLearner(),
            ChurningPlayer()
        };
    }
}
