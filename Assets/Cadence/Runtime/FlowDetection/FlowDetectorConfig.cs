using UnityEngine;

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Flow Detector Config")]
    public class FlowDetectorConfig : ScriptableObject
    {
        [Header("Windows")]
        public int TempoWindowSize = 8;
        public int EfficiencyWindowSize = 12;
        public int EngagementWindowSize = 20;

        [Header("Thresholds")]
        [Range(0f, 1f)] public float BoredomEfficiencyMin = 0.85f;
        [Range(0f, 1f)] public float BoredomTempoMin = 0.7f;
        [Range(0f, 1f)] public float AnxietyEfficiencyMax = 0.3f;
        [Range(0f, 1f)] public float AnxietyTempoMax = 0.2f;
        [Range(0f, 1f)] public float FrustrationThreshold = 0.7f;

        [Header("Stability")]
        public int HysteresisCount = 3;
        public int WarmupMoves = 5;
        [Range(0.01f, 1f)] public float ExponentialAlpha = 0.3f;
    }
}
