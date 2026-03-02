namespace Cadence
{
    public static class SignalKeys
    {
        // Tier 0: Decision Quality
        public const string MoveExecuted = "move.executed";
        public const string MoveOptimal = "move.optimal";
        public const string MoveWaste = "move.waste";
        public const string ResourceEfficiency = "resource.efficiency";
        public const string ProgressDelta = "progress.delta";

        // Tier 1: Behavioral Tempo
        public const string InterMoveInterval = "tempo.interval";
        public const string HesitationTime = "tempo.hesitation";
        public const string PauseTriggered = "tempo.pause";

        // Tier 2: Strategic Pattern
        public const string PowerUpUsed = "strategy.powerup";
        public const string ResourceStored = "strategy.stored";
        public const string SequenceMatch = "strategy.sequence_match";

        // Tier 3: Retry & Meta
        public const string AttemptNumber = "meta.attempt";
        public const string SessionGapDays = "meta.session_gap";
        public const string LevelAbandoned = "meta.abandoned";

        // Tier 4: Raw Input
        public const string InputAccuracy = "input.accuracy";
        public const string InputRejected = "input.rejected";

        // Session lifecycle (infrastructure)
        public const string SessionStarted = "session.started";
        public const string SessionEnded = "session.ended";
        public const string SessionOutcome = "session.outcome";
    }
}
