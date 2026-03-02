using System;
using System.Collections.Generic;

namespace Cadence
{
    [Serializable]
    public class DDADebugData
    {
        public bool SessionActive;
        public string CurrentLevelId;
        public float SessionTime;
        public int TotalSignals;
        public int RingBufferCount;
        public FlowReading CurrentFlow;
        public PlayerSkillProfile Profile;
        public SessionSummary LastSessionSummary;
        public AdjustmentProposal LastProposal;
        public Dictionary<string, float> CurrentLevelParams;
    }
}
