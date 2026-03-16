namespace Cadence
{
    /// <summary>
    /// All signal key constants used by the DDA system, organized into a five-tier taxonomy.
    /// Signals are recorded via <c>IDDAService.RecordSignal(key, value, tier)</c>.
    /// </summary>
    /// <remarks>
    /// <para><b>Signal Taxonomy (5 tiers):</b></para>
    /// <list type="bullet">
    ///   <item><b>Tier 0 - Decision Quality:</b> Move efficiency and optimality. Required for SkillScore.</item>
    ///   <item><b>Tier 1 - Behavioral Tempo:</b> Pacing signals (inter-move intervals, hesitation, pauses).</item>
    ///   <item><b>Tier 2 - Strategic Pattern:</b> Power-up usage, combos, resource management.</item>
    ///   <item><b>Tier 3 - Retry &amp; Meta:</b> Cross-session context (attempt count, session gaps, abandons).</item>
    ///   <item><b>Tier 4 - Raw Input:</b> Low-level input accuracy and rejection tracking.</item>
    /// </list>
    /// <para><b>Processing pipeline:</b></para>
    /// <list type="number">
    ///   <item>Game fires <c>RecordSignal()</c> with one of these keys.</item>
    ///   <item><see cref="ISignalCollector"/> stores it in the ring buffer.</item>
    ///   <item><see cref="IFlowDetector"/> processes it in real-time via <c>Tick()</c>.</item>
    ///   <item><see cref="ISessionAnalyzer"/> aggregates at session end.</item>
    ///   <item>Derived scores (Skill, Engagement, Frustration) feed adjustment rules.</item>
    /// </list>
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
        /// <remarks>Feeds: SessionSummary.ResourceEfficiency01 -> EffectiveEfficiency01 -> SkillScore, Glicko-2, profiler trends.</remarks>
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
        /// When any explicit tempo.interval is recorded in a session, it becomes authoritative for
        /// tempo analysis and real-time flow detection. Timestamp-derived intervals stop for that session.
        /// Non-positive values are ignored.
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
        /// <remarks>
        /// Sets an explicit abandon flag for the active session. The host still calls EndSession(),
        /// but the final outcome will be coerced to Abandoned when this signal was recorded.
        /// </remarks>
        public const string LevelAbandoned = "meta.abandoned";

        // ─────────────────────────────────────────────────────
        // Tier 4: Raw Input
        // Low-level input signals for confusion/accuracy tracking.
        // ─────────────────────────────────────────────────────

        /// <summary>Input accuracy metric. Value: 0-1.</summary>
        /// <remarks>Feeds: SessionSummary.InputAccuracy01 -> SkillScore enrichment and FrustrationScore enrichment.</remarks>
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
