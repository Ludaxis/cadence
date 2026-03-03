namespace Cadence
{
    /// <summary>
    /// Real-time player flow state, classified by the Flow Detector every tick.
    /// Drives mid-session relief and between-session adjustment decisions.
    /// </summary>
    public enum FlowState : byte
    {
        /// <summary>Insufficient data (fewer than WarmupMoves). No action taken.</summary>
        Unknown = 0,

        /// <summary>Efficiency > 0.85 AND Tempo > 0.7. Player breezing through. Response: increase challenge.</summary>
        Boredom = 1,

        /// <summary>Default state. Moderate efficiency and steady tempo. Response: maintain current difficulty.</summary>
        Flow = 2,

        /// <summary>Efficiency &lt; 0.3 AND Tempo &lt; 0.2. Erratic pacing, low quality. Response: reduce pressure.</summary>
        Anxiety = 3,

        /// <summary>FrustrationScore > 0.7. High waste, pauses, low efficiency. Response: immediate relief.</summary>
        Frustration = 4
    }
}
