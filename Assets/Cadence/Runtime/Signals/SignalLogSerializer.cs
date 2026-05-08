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
            public List<SerializedParameter> LevelParameters = new List<SerializedParameter>();
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
            public float C;     // Confidence
            public int HC;      // HasConfidence, for old-log compatibility
        }

        [Serializable]
        private struct SerializedParameter
        {
            public string K;
            public float V;
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
                    F = e.Timestamp.FrameNumber,
                    C = e.HasConfidence ? Mathf.Clamp01(e.Confidence) : 1f,
                    HC = e.HasConfidence ? 1 : 0
                });
            }

            if (batch.LevelParameters != null)
            {
                foreach (var kvp in batch.LevelParameters)
                {
                    sb.LevelParameters.Add(new SerializedParameter
                    {
                        K = kvp.Key,
                        V = kvp.Value
                    });
                }
            }

            return JsonUtility.ToJson(sb);
        }

        public static SignalBatch Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return new SignalBatch();

            var sb = JsonUtility.FromJson<SerializedBatch>(json);
            if (sb == null) return new SignalBatch();
            var batch = new SignalBatch
            {
                LevelId = sb.LevelId,
                SessionStartTime = sb.SessionStartTime,
                LevelParameters = DeserializeParameters(sb.LevelParameters)
            };

            if (sb.Entries == null)
                return batch;

            for (int i = 0; i < sb.Entries.Count; i++)
            {
                var se = sb.Entries[i];
                batch.Add(new SignalEntry
                {
                    Key = se.K,
                    Value = se.V,
                    Confidence = se.HC == 0 ? 1f : Mathf.Clamp01(se.C),
                    HasConfidence = se.HC != 0,
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

        private static Dictionary<string, float> DeserializeParameters(
            List<SerializedParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            var result = new Dictionary<string, float>(parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (!string.IsNullOrEmpty(parameter.K))
                    result[parameter.K] = parameter.V;
            }

            return result;
        }
    }
}
