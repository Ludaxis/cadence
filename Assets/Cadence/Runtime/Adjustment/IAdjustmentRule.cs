using System.Collections.Generic;

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

    /// <summary>
    /// Provides one or more <see cref="IAdjustmentRule"/> instances for registration on an <see cref="IDDAService"/>.
    /// Use this when you want to ship a rule pack instead of registering rules one by one.
    /// </summary>
    public interface IAdjustmentRuleProvider
    {
        /// <summary>
        /// Creates the rules to register for the current service.
        /// Returned rules are evaluated after the built-in rules, in enumeration order.
        /// </summary>
        IEnumerable<IAdjustmentRule> CreateRules(DDAConfig config);
    }
}
