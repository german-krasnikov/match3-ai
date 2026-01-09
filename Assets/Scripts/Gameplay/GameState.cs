// Assets/Scripts/Gameplay/GameState.cs
namespace Gameplay
{
    /// <summary>
    /// States of the game loop state machine.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Waiting for player input (drag & drop).
        /// </summary>
        WaitingForInput,

        /// <summary>
        /// Swap animation in progress.
        /// </summary>
        Swapping,

        /// <summary>
        /// Checking for matches after swap.
        /// </summary>
        Matching,

        /// <summary>
        /// Destroy animation in progress.
        /// </summary>
        Destroying,

        /// <summary>
        /// Fall animation in progress.
        /// </summary>
        Falling,

        /// <summary>
        /// Spawning new elements to fill empty spaces.
        /// </summary>
        Refilling
    }
}
