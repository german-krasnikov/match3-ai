using System;
using System.Collections.Generic;

namespace Match3.Core
{
    public interface IBoardState
    {
        int Width { get; }
        int Height { get; }

        IPiece GetPieceAt(GridPosition position);
        void SetPieceAt(GridPosition position, IPiece piece);
        void ClearCell(GridPosition position);
        bool IsEmpty(GridPosition position);

        IEnumerable<GridPosition> AllPositions { get; }

        event Action OnBoardChanged;
    }
}
