namespace LuckySpin.SlotMachine.Core
{
    /// <summary>
    /// Represents the high-level operational state of the slot machine game.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Ready for player input (bet changes, spin initiation).
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Reels are actively spinning towards predetermined outcomes.
        /// </summary>
        Spinning = 1,

        /// <summary>
        /// Reels have stopped; winning combinations are being evaluated.
        /// </summary>
        Evaluating = 2,

        /// <summary>
        /// Payout calculations and reward animations/sounds are playing.
        /// </summary>
        Payout = 3,

        /// <summary>
        /// Player balance is below the minimum bet threshold.
        /// </summary>
        InsufficientFunds = 4
    }
}

