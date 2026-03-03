using System;
using UnityEngine;

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/DDA Config")]
    public class DDAConfig : ScriptableObject
    {
        public PlayerModelConfig PlayerModelConfig;
        public FlowDetectorConfig FlowDetectorConfig;
        public AdjustmentEngineConfig AdjustmentEngineConfig;

        [Header("Signal Collection")]
        public int RingBufferCapacity = 512;
        public bool EnableSignalStorage = true;
        public int MaxStoredSessions = 50;

        [Header("Scheduling")]
        public SawtoothCurveConfig SawtoothCurveConfig;

        [Header("Global")]
        public bool EnableMidSessionDetection = true;
        public bool EnableBetweenSessionAdjustment = true;

        public static event Action OnConfigChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnConfigChanged?.Invoke();
        }
#endif
    }
}
