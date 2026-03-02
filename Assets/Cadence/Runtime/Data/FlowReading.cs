using System;

namespace Cadence
{
    [Serializable]
    public struct FlowReading
    {
        public FlowState State;
        public float Confidence;
        public float TempoScore;
        public float EfficiencyScore;
        public float EngagementScore;
        public float SessionTime;
    }
}
