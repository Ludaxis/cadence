namespace Cadence
{
    /// <summary>
    /// Categorizes puzzle levels for level-type-aware DDA adjustments.
    /// Each type has default config via <see cref="LevelTypeDefaults.GetDefaults"/>.
    /// </summary>
    public enum LevelType : byte
    {
        /// <summary>Normal puzzle level. Full DDA adjustments, scale 1.0x.</summary>
        Standard = 0,

        /// <summary>Move-limited puzzle. Primary param = "move_limit". Scale 1.0x.</summary>
        MoveLimited = 1,

        /// <summary>Time-limited puzzle. Primary param = "time_limit". Scale 1.0x.</summary>
        TimeLimited = 2,

        /// <summary>Goal collection puzzle. Primary param = "goal_count". Scale 0.8x (conservative).</summary>
        GoalCollection = 3,

        /// <summary>Boss level. Handcrafted difficulty. Scale 0.3x (minimal adjustments).</summary>
        Boss = 4,

        /// <summary>Breather level after a boss. Scale 0.5x. No upward adjustment allowed.</summary>
        Breather = 5,

        /// <summary>Tutorial level. DDA fully disabled. Scale 0.0x.</summary>
        Tutorial = 6
    }
}
