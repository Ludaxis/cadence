using System;
using System.Collections.Generic;

namespace Cadence
{
    [Serializable]
    public class AdjustmentProposal
    {
        public List<ParameterDelta> Deltas = new List<ParameterDelta>();
        public float Confidence;
        public string Reason;
        public FlowState DetectedState;
        public AdjustmentTiming Timing;
    }

    public enum AdjustmentTiming : byte
    {
        BeforeNextLevel = 0,
        MidSession = 1
    }
}
