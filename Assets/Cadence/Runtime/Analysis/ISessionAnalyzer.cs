namespace Cadence
{
    /// <summary>
    /// Aggregates raw signals from a completed session into derived scores (Skill, Engagement, Frustration)
    /// and summary statistics used by the player model and adjustment rules.
    /// </summary>
    public interface ISessionAnalyzer
    {
        /// <summary>
        /// Processes all signals in the batch and returns a <see cref="SessionSummary"/>
        /// containing move counts, derived scores, timing statistics, and outcome.
        /// </summary>
        /// <param name="batch">The complete signal batch from a finished session.</param>
        /// <returns>Aggregated session summary with derived metrics.</returns>
        SessionSummary Analyze(SignalBatch batch);
    }
}
