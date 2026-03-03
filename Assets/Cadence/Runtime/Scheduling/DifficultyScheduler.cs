using UnityEngine;

namespace Cadence
{
    public sealed class DifficultyScheduler : IDifficultyScheduler
    {
        private readonly SawtoothCurveConfig _config;

        public DifficultyScheduler(SawtoothCurveConfig config)
        {
            _config = config;
        }

        public float GetTargetMultiplier(int levelIndex)
        {
            if (_config == null || levelIndex < 0) return 1f;

            int period = Mathf.Max(1, _config.Period);
            int cycle = levelIndex / period;
            int posInCycle = levelIndex % period;

            float baselineDrift = cycle * _config.BaselineDriftPerCycle;
            var suggestedType = GetSuggestedTypeInternal(posInCycle, period);

            float multiplier;
            switch (suggestedType)
            {
                case LevelType.Boss:
                    // Boss gets peak: 1.0 + amplitude
                    multiplier = 1f + _config.Amplitude;
                    break;
                case LevelType.Breather:
                    // Breather gets trough: 1.0 - reliefDepth
                    multiplier = 1f - _config.ReliefDepth;
                    break;
                default:
                    // Ramp from low to high within the cycle
                    float t = GetRampProgress(posInCycle, period);
                    // Ramp from (1 - reliefDepth) toward (1 + amplitude * 0.8)
                    float low = 1f - _config.ReliefDepth * 0.5f;
                    float high = 1f + _config.Amplitude * 0.8f;
                    multiplier = Mathf.Lerp(low, high, t);
                    break;
            }

            multiplier += baselineDrift;
            return multiplier;
        }

        public LevelType GetSuggestedLevelType(int levelIndex)
        {
            if (_config == null || levelIndex < 0) return LevelType.Standard;

            int period = Mathf.Max(1, _config.Period);
            int posInCycle = levelIndex % period;
            return GetSuggestedTypeInternal(posInCycle, period);
        }

        public DifficultyPoint[] GetCurvePreview(int start, int count)
        {
            if (count <= 0) return new DifficultyPoint[0];

            var points = new DifficultyPoint[count];
            for (int i = 0; i < count; i++)
            {
                int idx = start + i;
                points[i] = new DifficultyPoint
                {
                    LevelIndex = idx,
                    Multiplier = GetTargetMultiplier(idx),
                    SuggestedType = GetSuggestedLevelType(idx)
                };
            }
            return points;
        }

        private LevelType GetSuggestedTypeInternal(int posInCycle, int period)
        {
            // Boss position: offset from end of cycle
            int bossPos = period + _config.BossLevelOffset;
            if (bossPos < 0) bossPos = 0;
            if (bossPos >= period) bossPos = period - 1;

            // Breather position: offset from start of cycle
            int breatherPos = _config.BreatherLevelOffset;
            if (breatherPos < 0) breatherPos = 0;
            if (breatherPos >= period) breatherPos = 0;

            if (posInCycle == bossPos)
                return LevelType.Boss;
            if (posInCycle == breatherPos)
                return LevelType.Breather;
            return LevelType.Standard;
        }

        private float GetRampProgress(int posInCycle, int period)
        {
            // Normalize position to 0-1 within the cycle
            float t = period > 1 ? posInCycle / (float)(period - 1) : 0f;

            // Check custom curve first
            if (_config.CurveShape != null && _config.CurveShape.length > 1)
                return _config.CurveShape.Evaluate(t);

            // Apply ramp style
            switch (_config.RampStyle)
            {
                case RampStyle.EaseIn:
                    return t * t;
                case RampStyle.EaseOut:
                    return 1f - (1f - t) * (1f - t);
                case RampStyle.SCurve:
                    return t * t * (3f - 2f * t); // Smoothstep
                default: // Linear
                    return t;
            }
        }
    }
}
