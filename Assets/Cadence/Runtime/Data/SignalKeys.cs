namespace Cadence
{
    /// <summary>
    /// All signal key constants used by the DDA system.
    /// Signals are recorded via <c>IDDAService.RecordSignal(key, value, tier)</c>.
    /// </summary>
    /// <remarks>
    /// Signal processing pipeline:
    /// 1. Game fires RecordSignal() with one of these keys
    /// 2. SignalCollector stores it in the ring buffer
    /// 3. FlowDetector processes it in real-time (Tick)
    /// 4. SessionAnalyzer aggregates at session end
    /// 5. Derived scores (Skill, Engagement, Frustration) feed rules
    /// </remarks>
    public static class SignalKeys
    {
        // ─────────────────────────────────────────────────────
        // Tier 0: Decision Quality
        // These are the highest-priority signals. Without them,
        // SkillScore and MoveEfficiency cannot be computed.
        // ─────────────────────────────────────────────────────

        /// <summary>A move was executed. Value: 1.0. Increments move count for efficiency calculations.</summary>
        /// <remarks>Feeds: MoveEfficiency denominator, InterMoveVariance timestamps, EngagementScore.</remarks>
        public const string MoveExecuted = "move.executed";

        /// <summary>The executed move was strategically optimal. Value: 1.0.</summary>
        /// <remarks>Feeds: MoveEfficiency numerator -> SkillScore (70% weight) -> Glicko-2, Flow Channel Rule.</remarks>
        public const string MoveOptimal = "move.optimal";

        /// <summary>The executed move was wasteful/inefficient. Value: 0-1 waste magnitude.</summary>
        /// <remarks>Feeds: WasteRatio -> FrustrationScore (30% weight) -> Frustration Relief Rule.</remarks>
        public const string MoveWaste = "move.waste";

        /// <summary>Resource efficiency signal. Value: 0-1.</summary>
        /// <remarks>Optional enrichment for economy-aware games.</remarks>
        public const string ResourceEfficiency = "resource.efficiency";

        /// <summary>Micro-progress per action (e.g., cells filled / total). Value: 0-1.</summary>
        /// <remarks>Feeds: Flow Detector engagement window (pushes 1.0), ProgressRate aggregate.</remarks>
        public const string ProgressDelta = "progress.delta";

        // ─────────────────────────────────────────────────────
        // Tier 1: Behavioral Tempo
        // Pacing signals. How fast/steady is the player acting?
        // ─────────────────────────────────────────────────────

        /// <summary>Milliseconds since the previous move. Value: interval in seconds.</summary>
        /// <remarks>
        /// Feeds: MeanInterMoveInterval (Welford mean), InterMoveVariance (Welford variance)
        /// -> TempoConsistency -> EngagementScore (60% weight).
        /// Also feeds FrustrationScore variance component (25% weight).
        /// </remarks>
        public const string InterMoveInterval = "tempo.interval";

        /// <summary>Delay before first action within a move / turn. Value: seconds.</summary>
        /// <remarks>Anxiety indicator. Long hesitation = player unsure what to do.</remarks>
        public const string HesitationTime = "tempo.hesitation";

        /// <summary>Player paused or went idle. Value: 1.0 per pause event.</summary>
        /// <remarks>
        /// Feeds: PauseCount -> PausePenalty -> EngagementScore (40% weight).
        /// Also feeds FrustrationScore pause component (20% weight).
        /// Flow Detector pushes 0.0 to engagement window on this signal.
        /// </remarks>
        public const string PauseTriggered = "tempo.pause";

        // ─────────────────────────────────────────────────────
        // Tier 2: Strategic Pattern
        // How the player uses power-ups and strategic mechanics.
        // ─────────────────────────────────────────────────────

        /// <summary>A booster / power-up was used. Value: 1.0 per use.</summary>
        /// <remarks>
        /// Feeds: PowerUpsUsed count in SessionSummary.
        /// High usage -> BoosterDependent archetype classification.
        /// </remarks>
        public const string PowerUpUsed = "strategy.powerup";

        /// <summary>Resource was stored / banked. Value: amount.</summary>
        /// <remarks>Optional economy signal for resource management games.</remarks>
        public const string ResourceStored = "strategy.stored";

        /// <summary>A planned sequence / combo was correctly executed. Value: 1.0.</summary>
        /// <remarks>Feeds: SequenceMatchRate -> SkillScore (30% weight).</remarks>
        public const string SequenceMatch = "strategy.sequence_match";

        // ─────────────────────────────────────────────────────
        // Tier 3: Retry & Meta
        // Cross-session context that enriches frustration detection.
        // ─────────────────────────────────────────────────────

        /// <summary>Current attempt number for this level. Value: attempt count (int).</summary>
        /// <remarks>High count = player stuck. Feeds SessionSummary.AttemptNumber.</remarks>
        public const string AttemptNumber = "meta.attempt";

        /// <summary>Days since the player's last session. Value: float days.</summary>
        /// <remarks>Feeds: Glicko-2 time decay, ChurnRisk archetype scoring.</remarks>
        public const string SessionGapDays = "meta.session_gap";

        /// <summary>The level was abandoned (quit without finishing). Value: 1.0.</summary>
        /// <remarks>Feeds: SessionOutcome mapping (outcome = Abandoned).</remarks>
        public const string LevelAbandoned = "meta.abandoned";

        // ─────────────────────────────────────────────────────
        // Tier 4: Raw Input
        // Low-level input signals for confusion/accuracy tracking.
        // ─────────────────────────────────────────────────────

        /// <summary>Input accuracy metric. Value: 0-1.</summary>
        /// <remarks>Mapped from perfect_percentage in event tracking.</remarks>
        public const string InputAccuracy = "input.accuracy";

        /// <summary>An invalid/rejected input was attempted. Value: 1.0 per rejection.</summary>
        /// <remarks>Confusion/frustration signal. Flow Detector pushes 0.0 to engagement window.</remarks>
        public const string InputRejected = "input.rejected";

        // ─────────────────────────────────────────────────────
        // Session Lifecycle (infrastructure — not game-facing)
        // These are recorded automatically by DDAService.
        // ─────────────────────────────────────────────────────

        /// <summary>Session has started. Recorded automatically by BeginSession().</summary>
        public const string SessionStarted = "session.started";

        /// <summary>Session has ended. Recorded automatically by EndSession().</summary>
        public const string SessionEnded = "session.ended";

        /// <summary>Session outcome. Value: 1.0 (win), 0.0 (lose), -1.0 (abandoned).</summary>
        public const string SessionOutcome = "session.outcome";
    }
}
