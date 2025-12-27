using System.Collections.Generic;

namespace Match3.Core
{
    public readonly struct MatchResult
    {
        public readonly IReadOnlyList<GridPosition> Positions;
        public readonly PieceType Type;

        public MatchResult(IReadOnlyList<GridPosition> positions, PieceType type)
        {
            Positions = positions;
            Type = type;
        }

        public int Count => Positions?.Count ?? 0;
    }

    public interface IMatchDetector
    {
        IReadOnlyList<MatchResult> FindAllMatches();
        bool HasMatchAt(GridPosition position);
        MatchResult? FindMatchAt(GridPosition position);
    }
}
