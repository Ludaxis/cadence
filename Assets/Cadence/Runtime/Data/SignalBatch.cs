using System;
using System.Collections.Generic;

namespace Cadence
{
    /// <summary>
    /// Accumulates all <see cref="SignalEntry"/> instances recorded during a single gameplay session.
    /// Passed to <see cref="ISessionAnalyzer.Analyze"/> at session end and to <see cref="ISignalStorage"/> for persistence.
    /// </summary>
    [Serializable]
    public class SignalBatch
    {
        public string LevelId;
        public float SessionStartTime;
        public List<SignalEntry> Entries = new List<SignalEntry>();
        public Dictionary<string, float> LevelParameters;

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
            LevelParameters = null;
        }
    }
}
