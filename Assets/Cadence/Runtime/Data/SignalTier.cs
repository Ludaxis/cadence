namespace Cadence
{
    /// <summary>
    /// Priority tier for signals. Tier 0 signals are critical for basic DDA functionality.
    /// Higher tiers provide enrichment and context.
    /// </summary>
    public enum SignalTier : byte
    {
        /// <summary>Tier 0: Move optimality, waste, progress. Without these, SkillScore = 0.</summary>
        DecisionQuality = 0,

        /// <summary>Tier 1: Inter-move intervals, hesitation, pauses. Feeds EngagementScore and TempoConsistency.</summary>
        BehavioralTempo = 1,

        /// <summary>Tier 2: Booster usage, sequence matches. Feeds archetype classification.</summary>
        StrategicPattern = 2,

        /// <summary>Tier 3: Attempt number, session gaps, abandonment. Cross-session context.</summary>
        RetryMeta = 3,

        /// <summary>Tier 4: Input accuracy, rejected inputs. Low-level confusion tracking.</summary>
        RawInput = 4,

        /// <summary>Tier 5: Reserved for future biometric signals (heart rate, GSR).</summary>
        Biometric = 5
    }
}
