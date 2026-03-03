namespace Cadence
{
    /// <summary>
    /// Maps <see cref="PlayerArchetype"/> classifications to adjustment scaling rules.
    /// Used by the <see cref="AdjustmentEngine"/> to modulate delta magnitudes and block
    /// upward adjustments for at-risk player types.
    /// </summary>
    public static class ArchetypeAdjustmentStrategy
    {
        /// <summary>
        /// Returns a scale modifier for adjustment deltas based on player archetype.
        /// Higher = more aggressive adjustments, lower = more conservative.
        /// </summary>
        public static float GetAdjustmentScaleModifier(PlayerArchetype archetype) => archetype switch
        {
            PlayerArchetype.SpeedRunner       => 1.3f,
            PlayerArchetype.CarefulThinker    => 0.8f,
            PlayerArchetype.StrugglingLearner => 0.6f,
            PlayerArchetype.BoosterDependent  => 0.9f,
            PlayerArchetype.ChurnRisk         => 0.5f,
            _                                 => 1.0f
        };

        /// <summary>
        /// Returns true if this archetype should override and prevent upward difficulty adjustments.
        /// </summary>
        public static bool ShouldBlockUpwardAdjustment(PlayerArchetype archetype)
        {
            return archetype == PlayerArchetype.ChurnRisk ||
                   archetype == PlayerArchetype.StrugglingLearner;
        }
    }
}
