using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// JSON serialization for signal batches.
    /// Uses JsonUtility-compatible wrapper types.
    /// </summary>
    public static class SignalLogSerializer
    {
        [Serializable]
        private class SerializedBatch
        {
            public string LevelId;
            public float SessionStartTime;
            public List<SerializedEntry> Entries = new List<SerializedEntry>();
        }

        [Serializable]
        private struct SerializedEntry
        {
            public string K;   // Key
            public float V;    // Value
            public int T;      // Tier
            public int M;      // MoveIndex
            public float ST;   // SessionTime
            public int F;      // FrameNumber
        }

        public static string Serialize(SignalBatch batch)
        {
            var sb = new SerializedBatch
            {
                LevelId = batch.LevelId,
                SessionStartTime = batch.SessionStartTime
            };

            var entries = batch.Entries;
            sb.Entries.Capacity = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.Entries.Add(new SerializedEntry
                {
                    K = e.Key,
                    V = e.Value,
                    T = (int)e.Tier,
                    M = e.MoveIndex,
                    ST = e.Timestamp.SessionTime,
                    F = e.Timestamp.FrameNumber
                });
            }

            return JsonUtility.ToJson(sb);
        }

        public static SignalBatch Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return new SignalBatch();

            var sb = JsonUtility.FromJson<SerializedBatch>(json);
            var batch = new SignalBatch
            {
                LevelId = sb.LevelId,
                SessionStartTime = sb.SessionStartTime
            };

            for (int i = 0; i < sb.Entries.Count; i++)
            {
                var se = sb.Entries[i];
                batch.Add(new SignalEntry
                {
                    Key = se.K,
                    Value = se.V,
                    Tier = (SignalTier)se.T,
                    MoveIndex = se.M,
                    Timestamp = new SignalTimestamp
                    {
                        SessionTime = se.ST,
                        FrameNumber = se.F
                    }
                });
            }

            return batch;
        }
    }
}
