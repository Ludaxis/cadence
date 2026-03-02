using UnityEngine;

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Player Model Config")]
    public class PlayerModelConfig : ScriptableObject
    {
        [Header("Glicko-2 Defaults")]
        public float InitialRating = 1500f;
        public float InitialDeviation = 350f;
        public float InitialVolatility = 0.06f;

        [Header("System Constants")]
        [Tooltip("Controls the size of the rating change. Typical: 0.5 to 1.2")]
        public float Tau = 0.5f;

        [Tooltip("Convergence tolerance for volatility iteration")]
        public float ConvergenceEpsilon = 0.000001f;

        [Header("Time Decay")]
        [Tooltip("Deviation increase per day of inactivity")]
        public float DeviationDecayPerDay = 5f;

        [Tooltip("Maximum deviation after decay")]
        public float MaxDeviation = 350f;

        [Header("Session History")]
        public int MaxHistoryEntries = 20;
    }
}
