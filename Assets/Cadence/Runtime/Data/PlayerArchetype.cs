namespace Cadence
{
    /// <summary>
    /// Behavioral archetype classification for DDA modulation.
    /// Each archetype adjusts how aggressively the system tunes difficulty.
    /// </summary>
    public enum PlayerArchetype : byte
    {
        /// <summary>Not yet classified. Requires 3+ sessions. No adjustment modulation (1.0x).</summary>
        Unknown = 0,

        /// <summary>High efficiency, fast completion, high win rate. Adjustment scale: 1.3x (more aggressive).</summary>
        SpeedRunner = 1,

        /// <summary>Moderate efficiency, longer sessions, steady win rate. Adjustment scale: 0.8x (conservative).</summary>
        CarefulThinker = 2,

        /// <summary>Low efficiency, low win rate, improving trend. Adjustment scale: 0.6x. Upward adjustment blocked.</summary>
        StrugglingLearner = 3,

        /// <summary>High booster usage, low efficiency despite winning. Adjustment scale: 0.9x.</summary>
        BoosterDependent = 4,

        /// <summary>Declining engagement, declining efficiency, low recent win rate. Adjustment scale: 1.4x. Upward blocked.</summary>
        ChurnRisk = 5
    }
}
