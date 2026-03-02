using System;
using System.Collections.Generic;

namespace Cadence
{
    [Serializable]
    public class SignalBatch
    {
        public string LevelId;
        public float SessionStartTime;
        public List<SignalEntry> Entries = new List<SignalEntry>();

        public int Count => Entries.Count;

        public void Add(SignalEntry entry)
        {
            Entries.Add(entry);
        }

        public void Clear()
        {
            LevelId = null;
            SessionStartTime = 0f;
            Entries.Clear();
        }

        public IReadOnlyList<SignalEntry> GetEntries() => Entries;
    }
}
