namespace Cadence
{
    /// <summary>
    /// Computes the sawtooth difficulty curve that modulates base difficulty across the level progression.
    /// Provides target multipliers, suggested level types, and curve previews for visualization.
    /// </summary>
    public interface IDifficultyScheduler
    {
        /// <summary>
        /// Returns the difficulty multiplier for the given level index on the sawtooth curve.
        /// </summary>
        /// <param name="levelIndex">Zero-based global level index.</param>
        /// <returns>Multiplier where 1.0 = baseline, &gt;1.0 = harder (boss peaks), &lt;1.0 = easier (breather dips).</returns>
        float GetTargetMultiplier(int levelIndex);

        /// <summary>
        /// Returns the suggested <see cref="LevelType"/> based on the level's position on the sawtooth curve.
        /// </summary>
        /// <param name="levelIndex">Zero-based global level index.</param>
        /// <returns>Boss at peaks, Breather at dips, Standard on ramps.</returns>
        LevelType GetSuggestedLevelType(int levelIndex);

        /// <summary>
        /// Returns an array of <see cref="DifficultyPoint"/> for visualizing a segment of the difficulty curve.
        /// </summary>
        /// <param name="start">Starting level index (inclusive).</param>
        /// <param name="count">Number of consecutive levels to include.</param>
        /// <returns>Array of difficulty points with multipliers and suggested types.</returns>
        DifficultyPoint[] GetCurvePreview(int start, int count);
    }
}
