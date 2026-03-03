namespace Cadence
{
    /// <summary>
    /// Maintains the player's skill model using Glicko-2 rating, tracks session history,
    /// and predicts win probability against a given difficulty level.
    /// </summary>
    public interface IPlayerModel
    {
        /// <summary>
        /// Returns the current player skill profile including rating, deviation, volatility, and history.
        /// </summary>
        PlayerSkillProfile Profile { get; }

        /// <summary>
        /// Updates the player's Glicko-2 rating and session history from a completed session's analysis.
        /// </summary>
        /// <param name="summary">Aggregated session summary from <see cref="ISessionAnalyzer.Analyze"/>.</param>
        void UpdateFromSession(SessionSummary summary);

        /// <summary>
        /// Predicts the player's win probability against a level of the given difficulty.
        /// </summary>
        /// <param name="levelDifficulty">Difficulty rating of the level (same scale as player rating).</param>
        /// <returns>Estimated win probability in the range [0, 1].</returns>
        float PredictWinRate(float levelDifficulty);

        /// <summary>
        /// Applies Glicko-2 time decay to increase rating deviation after player inactivity.
        /// </summary>
        /// <param name="daysSinceLastSession">Number of days since the player's last recorded session.</param>
        void ApplyTimeDecay(float daysSinceLastSession);

        /// <summary>
        /// Serializes the player model state to a JSON string for persistence.
        /// </summary>
        /// <returns>JSON representation of the player model.</returns>
        string Serialize();

        /// <summary>
        /// Restores the player model state from a previously serialized JSON string.
        /// </summary>
        /// <param name="json">JSON string produced by <see cref="Serialize"/>.</param>
        void Deserialize(string json);
    }
}
