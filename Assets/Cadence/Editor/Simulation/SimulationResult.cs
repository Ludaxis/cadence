using System;
using System.Collections.Generic;

namespace Cadence.Editor
{
    [Serializable]
    public struct LevelSnapshot
    {
        public int LevelIndex;
        public LevelType LevelType;
        public float SawtoothMultiplier;
        public bool Won;
        public float PlayerRating;
        public float PlayerDeviation;
        public FlowState FlowState;
        public float RollingWinRate;
        public int LevelsThisSession;
        public bool SessionFatigueActive;
        public bool WasAbandoned;
        public float MeanInterMoveInterval;
        public float InputAccuracy01;
        public float ResourceEfficiency01;
        public Dictionary<string, float> AdjustedParams;
        public float AdjustmentDelta;
    }

    public class SimulationRun
    {
        public PlayerPersona Persona;
        public List<LevelSnapshot> Snapshots = new List<LevelSnapshot>();

        // Computed aggregates
        public float OverallWinRate;
        public float FinalRating;
        public int LongestWinStreak;
        public int LongestLoseStreak;
        public int FlowCount;
        public int FrustrationCount;
        public int BoredomCount;
        public int AnxietyCount;
        public float AverageAdjustmentDelta;

        public void ComputeAggregates()
        {
            if (Snapshots.Count == 0) return;

            int wins = 0;
            int currentWinStreak = 0;
            int currentLoseStreak = 0;
            float totalDelta = 0f;

            LongestWinStreak = 0;
            LongestLoseStreak = 0;
            FlowCount = 0;
            FrustrationCount = 0;
            BoredomCount = 0;
            AnxietyCount = 0;

            for (int i = 0; i < Snapshots.Count; i++)
            {
                var snap = Snapshots[i];

                if (snap.Won)
                {
                    wins++;
                    currentWinStreak++;
                    currentLoseStreak = 0;
                    if (currentWinStreak > LongestWinStreak)
                        LongestWinStreak = currentWinStreak;
                }
                else
                {
                    currentLoseStreak++;
                    currentWinStreak = 0;
                    if (currentLoseStreak > LongestLoseStreak)
                        LongestLoseStreak = currentLoseStreak;
                }

                switch (snap.FlowState)
                {
                    case FlowState.Flow:        FlowCount++; break;
                    case FlowState.Frustration:  FrustrationCount++; break;
                    case FlowState.Boredom:      BoredomCount++; break;
                    case FlowState.Anxiety:      AnxietyCount++; break;
                }

                totalDelta += Math.Abs(snap.AdjustmentDelta);
            }

            var last = Snapshots[Snapshots.Count - 1];
            OverallWinRate = (float)wins / Snapshots.Count;
            FinalRating = last.PlayerRating;
            AverageAdjustmentDelta = totalDelta / Snapshots.Count;
        }
    }
}
