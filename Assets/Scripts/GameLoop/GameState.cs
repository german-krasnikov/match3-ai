namespace Match3.GameLoop
{
    /// <summary>
    /// Game loop states for debugging and events.
    /// </summary>
    public enum GameState
    {
        Idle,
        Swapping,
        Matching,
        Destroying,
        Falling,
        Refilling,
        CheckingCascade,
        Shuffling
    }
}
