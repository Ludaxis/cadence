namespace Cadence
{
    /// <summary>
    /// Persists and retrieves <see cref="SignalBatch"/> data per level for cross-session analysis and replay.
    /// </summary>
    public interface ISignalStorage
    {
        /// <summary>
        /// Saves a completed session's signal batch under the given level ID.
        /// </summary>
        /// <param name="levelId">Level identifier to associate the batch with.</param>
        /// <param name="batch">The signal batch to persist.</param>
        void Save(string levelId, SignalBatch batch);

        /// <summary>
        /// Loads a signal batch for the given level. Returns <c>null</c> if no data exists.
        /// </summary>
        /// <param name="levelId">Level identifier to look up.</param>
        /// <param name="sessionIndex">Zero-based session index, or -1 for the most recent session.</param>
        /// <returns>The stored signal batch, or <c>null</c> if not found.</returns>
        SignalBatch Load(string levelId, int sessionIndex = -1);

        /// <summary>
        /// Returns the number of stored sessions for the given level.
        /// </summary>
        /// <param name="levelId">Level identifier to query.</param>
        int GetSessionCount(string levelId);

        /// <summary>
        /// Removes oldest sessions exceeding the per-level limit to bound storage size.
        /// </summary>
        /// <param name="maxSessionsPerLevel">Maximum number of sessions to retain per level.</param>
        void Prune(int maxSessionsPerLevel);
    }
}
