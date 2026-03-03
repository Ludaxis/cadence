namespace Cadence
{
    /// <summary>
    /// Classifies the player into a behavioral archetype based on their skill profile and most recent session.
    /// Archetype classification drives per-archetype adjustment scaling via <see cref="ArchetypeAdjustmentStrategy"/>.
    /// </summary>
    public interface IPlayerProfiler
    {
        /// <summary>
        /// Analyzes the player's skill profile and last session to produce an archetype classification
        /// with confidence scores for each archetype.
        /// </summary>
        /// <param name="profile">Current player skill profile (rating, history, stats).</param>
        /// <param name="lastSession">Summary of the most recent completed session.</param>
        /// <returns>Archetype reading with the dominant archetype and per-archetype confidence scores.</returns>
        PlayerArchetypeReading Classify(PlayerSkillProfile profile, SessionSummary lastSession);
    }
}
