namespace Match3.Game
{
    /// <summary>
    /// All possible states of the game loop.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Waiting for player input. Input enabled.
        /// </summary>
        Idle,

        /// <summary>
        /// Swap animation in progress. Input disabled.
        /// </summary>
        Swapping,

        /// <summary>
        /// Finding matches on board. Immediate transition.
        /// </summary>
        Matching,

        /// <summary>
        /// Destroy animation in progress.
        /// </summary>
        Destroying,

        /// <summary>
        /// Gems falling + new gems spawning.
        /// </summary>
        Falling,

        /// <summary>
        /// Checking for cascade matches after fall.
        /// Immediate transition to Matching or Idle.
        /// </summary>
        Checking
    }
}
