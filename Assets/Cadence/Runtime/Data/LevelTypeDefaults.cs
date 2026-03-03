namespace Cadence
{
    /// <summary>
    /// Provides default <see cref="LevelTypeConfig"/> settings for each <see cref="LevelType"/>.
    /// These defaults define the primary adjustable parameter, adjustment scale, and DDA eligibility.
    /// </summary>
    public static class LevelTypeDefaults
    {
        /// <summary>
        /// Returns the default configuration for the given level type, including primary parameter,
        /// adjustment scale, and whether DDA and mid-session detection are enabled.
        /// </summary>
        public static LevelTypeConfig GetDefaults(LevelType type) => type switch
        {
            LevelType.MoveLimited    => new LevelTypeConfig(type, "move_limit", 1f, true, true),
            LevelType.TimeLimited    => new LevelTypeConfig(type, "time_limit", 1f, true, true),
            LevelType.GoalCollection => new LevelTypeConfig(type, "goal_count", 0.8f, true, true),
            LevelType.Boss           => new LevelTypeConfig(type, null, 0.3f, true, true),
            LevelType.Breather       => new LevelTypeConfig(type, null, 0.5f, false, true),
            LevelType.Tutorial       => new LevelTypeConfig(type, null, 0f, false, false),
            _                        => new LevelTypeConfig(type, null, 1f, true, true) // Standard
        };
    }
}
