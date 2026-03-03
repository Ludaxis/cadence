namespace Cadence
{
    /// <summary>
    /// Result of a completed session. Drives Glicko-2 update direction.
    /// </summary>
    public enum SessionOutcome : byte
    {
        /// <summary>Player completed the level successfully. Glicko-2 score = 1.0.</summary>
        Win = 0,

        /// <summary>Player failed the level. Glicko-2 score = 0.0.</summary>
        Lose = 1,

        /// <summary>Player quit before finishing. Treated as a loss with additional frustration signal.</summary>
        Abandoned = 2
    }
}
