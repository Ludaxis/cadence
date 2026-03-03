namespace Cadence
{
    public static class ArchetypeAdjustmentStrategy
    {
        /// <summary>
        /// Returns a scale modifier for adjustment deltas based on player archetype.
        /// Higher = more aggressive adjustments, lower = more conservative.
        /// </summary>
        public static float GetAdjustmentScaleModifier(PlayerArchetype archetype)
        {
            switch (archetype)
            {
                case PlayerArchetype.SpeedRunner:        return 1.3f;
                case PlayerArchetype.CarefulThinker:     return 0.8f;
                case PlayerArchetype.StrugglingLearner:  return 0.6f;
                case PlayerArchetype.BoosterDependent:   return 0.9f;
                case PlayerArchetype.ChurnRisk:          return 0.5f;
                default:                                 return 1.0f;
            }
        }

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
