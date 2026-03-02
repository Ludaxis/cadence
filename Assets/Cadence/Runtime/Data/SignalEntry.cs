using System;

namespace Cadence
{
    [Serializable]
    public struct SignalEntry
    {
        public string Key;
        public float Value;
        public SignalTier Tier;
        public int MoveIndex;
        public SignalTimestamp Timestamp;
    }

    [Serializable]
    public struct SignalTimestamp
    {
        public float SessionTime;
        public int FrameNumber;
    }
}
