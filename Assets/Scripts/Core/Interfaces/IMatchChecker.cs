namespace Match3.Core
{
    public interface IMatchChecker
    {
        bool WouldCreateMatch(GridPosition position, PieceType type);
    }
}
