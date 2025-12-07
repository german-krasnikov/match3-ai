using System.Collections.Generic;
using Match3.Core;

namespace Match3.Data
{
    public readonly struct MatchData
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public ElementType Type { get; }
        public bool IsHorizontal { get; }
        public int Length => Positions.Count;

        public MatchData(IReadOnlyList<GridPosition> positions, ElementType type, bool isHorizontal)
        {
            Positions = positions;
            Type = type;
            IsHorizontal = isHorizontal;
        }
    }
}
