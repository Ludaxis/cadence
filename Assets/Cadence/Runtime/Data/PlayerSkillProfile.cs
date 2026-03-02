using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    [Serializable]
    public class PlayerSkillProfile
    {
        public float Rating = DefaultRating;
        public float Deviation = DefaultDeviation;
        public float Volatility = DefaultVolatility;
        public int SessionsCompleted;
        public int TotalMoves;
        public float AverageEfficiency;
        public float AverageOutcome;
        public long LastSessionUtcTicks;

        public List<SessionHistoryEntry> RecentHistory = new List<SessionHistoryEntry>();

        public float Confidence01 => 1f - Mathf.Clamp01(Deviation / DefaultDeviation);
        public bool HasSufficientData => SessionsCompleted >= MinSessionsForConfidence;

        public const int MinSessionsForConfidence = 5;
        public const int MaxHistoryEntries = 20;
        public const float DefaultRating = 1500f;
        public const float DefaultDeviation = 350f;
        public const float DefaultVolatility = 0.06f;
    }

    [Serializable]
    public struct SessionHistoryEntry
    {
        public float Efficiency;
        public float Outcome;
        public float Duration;
        public int Moves;
        public long TimestampUtcTicks;
    }
}
