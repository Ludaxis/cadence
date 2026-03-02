using System;

namespace Cadence
{
    [Serializable]
    public struct ParameterDelta
    {
        public string ParameterKey;
        public float CurrentValue;
        public float ProposedValue;
        public string RuleName;

        public float Delta => ProposedValue - CurrentValue;
    }
}
