using System;

namespace Cadence
{
    [Serializable]
    public struct SessionSummary
    {
        // Outcome
        public SessionOutcome Outcome;
        public float Duration;
        public int TotalMoves;
        public int TotalSignals;

        // Tier 0 aggregates
        public float MoveEfficiency;
        public float WasteRatio;
        public float ProgressRate;

        // Tier 1 aggregates
        public float MeanInterMoveInterval;
        public float InterMoveVariance;
        public float HesitationTime;
        public int PauseCount;

        // Tier 2 aggregates
        public int PowerUpsUsed;
        public float SequenceMatchRate;

        // Tier 3 aggregates
        public int AttemptNumber;
        public float SessionGapDays;

        // Derived scores
        public float SkillScore;
        public float EngagementScore;
        public float FrustrationScore;
    }
}
