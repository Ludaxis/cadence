namespace Cadence
{
    /// <summary>
    /// A single adjustment rule that can modify difficulty parameters in an <see cref="AdjustmentProposal"/>.
    /// Rules are evaluated in priority order; each rule may add parameter deltas and reasons to the proposal.
    /// </summary>
    /// <remarks>
    /// <b>Contract:</b> <see cref="Evaluate"/> is only called when <see cref="IsApplicable"/> returns <c>true</c>.
    /// Implementations must not throw; a failing rule should simply return without modifying the proposal.
    /// </remarks>
    public interface IAdjustmentRule
    {
        /// <summary>
        /// Human-readable name for this rule, used in debug logs and proposal reason strings.
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Returns <c>true</c> if this rule's preconditions are met and it should be evaluated.
        /// Called before <see cref="Evaluate"/> to allow early skip.
        /// </summary>
        /// <param name="context">Current player state, session history, and level configuration.</param>
        bool IsApplicable(AdjustmentContext context);

        /// <summary>
        /// Evaluates this rule against the context and writes parameter deltas into the proposal.
        /// Only called when <see cref="IsApplicable"/> returned <c>true</c>.
        /// </summary>
        /// <param name="context">Current player state, session history, and level configuration.</param>
        /// <param name="proposal">The proposal to modify with parameter deltas and reason strings.</param>
        void Evaluate(AdjustmentContext context, AdjustmentProposal proposal);
    }
}
