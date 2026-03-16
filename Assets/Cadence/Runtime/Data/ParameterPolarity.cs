namespace Cadence
{
    /// <summary>
    /// Defines how a numeric parameter maps to semantic difficulty.
    /// </summary>
    public enum ParameterPolarity : byte
    {
        /// <summary>Higher numeric values make the level harder.</summary>
        HigherIsHarder = 0,

        /// <summary>Higher numeric values make the level easier.</summary>
        HigherIsEasier = 1
    }
}
