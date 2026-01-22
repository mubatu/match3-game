namespace Enums
{
    /// <summary>
    /// Represents the current state of the game board.
    /// Used to prevent input during animations and ensure sequential phase execution.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Board is idle and ready for player input.
        /// </summary>
        Idle,
        
        /// <summary>
        /// Items are being swapped (player initiated swap).
        /// </summary>
        Swapping,
        
        /// <summary>
        /// Matched items are being destroyed.
        /// </summary>
        Blasting,
        
        /// <summary>
        /// Items are falling due to gravity.
        /// </summary>
        Gravity,
        
        /// <summary>
        /// New items are being spawned to fill gaps.
        /// </summary>
        Refilling,
        
        /// <summary>
        /// Checking for cascade matches after gravity/refill.
        /// </summary>
        Cascading
    }
}
