using UnityEngine;

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Adjustment Engine Config")]
    public class AdjustmentEngineConfig : ScriptableObject
    {
        [Header("Flow Channel")]
        [Range(0f, 1f)] public float TargetWinRateMin = 0.3f;
        [Range(0f, 1f)] public float TargetWinRateMax = 0.7f;
        public AnimationCurve DifficultyAdjustmentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Streak Damping")]
        public int LossStreakThreshold = 3;
        public int WinStreakThreshold = 5;
        [Range(0f, 0.3f)] public float LossStreakEaseAmount = 0.10f;
        [Range(0f, 0.3f)] public float WinStreakHardenAmount = 0.05f;

        [Header("Frustration Relief")]
        [Range(0f, 1f)] public float FrustrationReliefThreshold = 0.7f;

        [Header("Cooldown")]
        public float GlobalCooldownSeconds = 60f;
        public float PerParameterCooldownSeconds = 120f;

        [Header("Clamping")]
        [Range(0f, 0.5f)] public float MaxDeltaPerAdjustment = 0.15f;
    }
}
