using System.Collections.Generic;

namespace Cadence
{
    /// <summary>
    /// Core interface for the Dynamic Difficulty Adjustment system.
    /// Manages session lifecycle, signal recording, flow detection, and proposal generation.
    /// </summary>
    /// <remarks>
    /// NOT thread-safe. All methods must be called from the main Unity thread.
    /// </remarks>
    public interface IDDAService
    {
        /// <summary>
        /// Starts a new gameplay session with the given level parameters.
        /// If a session is already active, it is ended with <see cref="SessionOutcome.Abandoned"/>.
        /// </summary>
        /// <param name="levelId">Unique identifier for the level being played.</param>
        /// <param name="levelParameters">Key-value pairs describing the level (e.g., move_limit, goal_count).</param>
        void BeginSession(string levelId, Dictionary<string, float> levelParameters);

        /// <summary>
        /// Starts a new gameplay session with explicit level type for type-specific adjustment rules.
        /// If a session is already active, it is ended with <see cref="SessionOutcome.Abandoned"/>.
        /// </summary>
        /// <param name="levelId">Unique identifier for the level being played.</param>
        /// <param name="levelParameters">Key-value pairs describing the level.</param>
        /// <param name="type">Level type that determines which adjustment rules and scaling apply.</param>
        void BeginSession(string levelId, Dictionary<string, float> levelParameters,
            LevelType type);

        /// <summary>
        /// Ends the current session and triggers analysis, player model update, and history recording.
        /// No-op if no session is active.
        /// </summary>
        /// <param name="outcome">Whether the player won, lost, or abandoned the level.</param>
        void EndSession(SessionOutcome outcome);

        /// <summary>
        /// Returns <c>true</c> if a session is currently active (between BeginSession and EndSession).
        /// </summary>
        bool IsSessionActive { get; }

        /// <summary>
        /// Records a gameplay signal into the current session's signal batch and ring buffer.
        /// Ignored if no session is active.
        /// </summary>
        /// <param name="key">Signal key from <see cref="SignalKeys"/> constants.</param>
        /// <param name="value">Signal value; meaning varies by key (e.g., 1.0 for events, 0-1 for magnitudes).</param>
        /// <param name="tier">Priority tier for processing order. Defaults to <see cref="SignalTier.DecisionQuality"/>.</param>
        /// <param name="moveIndex">Sequential move index (1-based), or -1 if not move-related.</param>
        /// <param name="confidence">Confidence in the signal value, from 0 to 1. Defaults to 1 for legacy callers.</param>
        /// <remarks>NOT thread-safe. Call from the main thread only.</remarks>
        void RecordSignal(string key, float value = 1f,
            SignalTier tier = SignalTier.DecisionQuality, int moveIndex = -1,
            float confidence = 1f);

        /// <summary>
        /// Advances the flow detector by one frame. Call once per frame during active gameplay.
        /// No-op if no session is active.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds (typically <c>Time.deltaTime</c>).</param>
        /// <remarks>NOT thread-safe. Call from the main thread only.</remarks>
        void Tick(float deltaTime);

        /// <summary>
        /// Returns the most recent real-time flow state (challenge vs. skill balance).
        /// Updated each frame by <see cref="Tick"/>.
        /// </summary>
        FlowReading CurrentFlow { get; }

        /// <summary>
        /// Generates a difficulty adjustment proposal with explicit level type and optional level index for scheduling.
        /// The next level type must always be explicit.
        /// </summary>
        /// <param name="nextLevelParameters">Baseline parameters for the upcoming level.</param>
        /// <param name="nextLevelType">Level type that determines which rules and scaling apply.</param>
        /// <param name="nextLevelIndex">Global level index for sawtooth scheduling, or -1 to skip scheduling.</param>
        /// <returns>
        /// A proposal object. Returns <c>null</c> only when between-session adjustment is disabled globally.
        /// Tutorial or otherwise DDA-disabled level types return an empty proposal instead.
        /// </returns>
        AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters,
            LevelType nextLevelType, int nextLevelIndex = -1);

        /// <summary>
        /// Applies the SDK low-confidence safety cap to a host-game variant step.
        /// Call this after converting an <see cref="AdjustmentProposal"/> into a variant step.
        /// </summary>
        /// <param name="proposalStep">The host-computed variant step before safety capping.</param>
        /// <param name="proposal">Proposal that produced the step, or null to use the current player profile confidence.</param>
        /// <returns>The capped or original variant step.</returns>
        int CapVariantStepForConfidence(int proposalStep, AdjustmentProposal proposal = null);

        /// <summary>
        /// Applies the SDK failed-retry safety cap to a host-game variant step.
        /// Call this when retrying the same level after a failed attempt.
        /// </summary>
        /// <param name="proposalStep">The host-computed variant step before safety capping.</param>
        /// <param name="isFailedRetry">True only when replaying the same level after a loss.</param>
        /// <returns>A step that cannot increase difficulty during failed retry.</returns>
        int CapVariantStepForFailedRetry(int proposalStep, bool isFailedRetry);

        /// <summary>
        /// Applies the SDK failed-retry safety cap after the host has computed the final variant.
        /// Call this when retrying the same level after a failed attempt.
        /// </summary>
        /// <param name="proposedVariant">Variant the host planned to serve.</param>
        /// <param name="failedVariant">Variant the player just failed.</param>
        /// <param name="isFailedRetry">True only when replaying the same level after a loss.</param>
        /// <returns>A variant that cannot be harder than the failed variant during failed retry.</returns>
        int CapVariantForFailedRetry(int proposedVariant, int failedVariant, bool isFailedRetry);

        /// <summary>
        /// Records that the host actually applied a proposal. This is the only call that updates
        /// adjustment cooldowns; <see cref="GetProposal"/> is evaluation-only.
        /// </summary>
        /// <param name="proposal">Proposal that was applied by the host game. Empty and null proposals are ignored.</param>
        void RecordProposalApplied(AdjustmentProposal proposal);

        /// <summary>
        /// Returns the current player skill profile (Glicko-2 rating, deviation, volatility, and history).
        /// </summary>
        PlayerSkillProfile PlayerProfile { get; }

        /// <summary>
        /// Returns the most recent player archetype classification (e.g., SpeedRunner, ChurnRisk).
        /// </summary>
        PlayerArchetypeReading CurrentArchetype { get; }

        /// <summary>
        /// Serializes the player profile to a JSON string for persistence.
        /// </summary>
        /// <returns>JSON representation of the current <see cref="PlayerSkillProfile"/>.</returns>
        string SaveProfile();

        /// <summary>
        /// Restores a player profile from a previously saved JSON string.
        /// </summary>
        /// <param name="json">JSON string produced by <see cref="SaveProfile"/>.</param>
        void LoadProfile(string json);

        /// <summary>
        /// Returns the sawtooth difficulty multiplier for the given level index from the difficulty scheduler.
        /// </summary>
        /// <param name="levelIndex">Zero-based global level index.</param>
        /// <returns>Multiplier where 1.0 = baseline, &gt;1.0 = harder, &lt;1.0 = easier.</returns>
        float GetTargetMultiplier(int levelIndex);

        /// <summary>
        /// Registers an additional adjustment rule to be evaluated after the built-in rules.
        /// Call during setup before proposal generation begins.
        /// </summary>
        void RegisterRule(IAdjustmentRule rule);

        /// <summary>
        /// Registers a provider that supplies one or more adjustment rules.
        /// Call during setup before proposal generation begins.
        /// </summary>
        void RegisterRuleProvider(IAdjustmentRuleProvider provider);

        /// <summary>
        /// Registers a provider that can override built-in <see cref="LevelTypeConfig"/> defaults.
        /// Later registrations take precedence when multiple providers handle the same type.
        /// </summary>
        void RegisterLevelTypeConfigProvider(ILevelTypeConfigProvider provider);

        /// <summary>
        /// Returns a complete snapshot of the DDA system state for debug visualization.
        /// </summary>
        DDADebugData GetDebugSnapshot();
    }
}
