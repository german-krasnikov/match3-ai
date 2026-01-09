// Assets/Scripts/Features/Board/Services/FallService.cs
using System.Collections.Generic;
using Common;
using Features.Board.Models;

namespace Features.Board.Services
{
    /// <summary>
    /// Calculates fall movements for Match3 board.
    /// Elements fall down to fill empty spaces below them.
    /// </summary>
    public class FallService : IFallService
    {
        /// <summary>
        /// Calculate all fall movements.
        /// For each column, elements above empty spaces fall down.
        /// </summary>
        public List<FallMove> CalculateFalls(BoardModel board)
        {
            var moves = new List<FallMove>();

            // Process each column independently
            for (int x = 0; x < board.Width; x++)
            {
                CalculateColumnFalls(board, x, moves);
            }

            return moves;
        }

        private void CalculateColumnFalls(BoardModel board, int x, List<FallMove> moves)
        {
            // Find the lowest empty position in this column
            int writeIndex = 0;

            for (int y = 0; y < board.Height; y++)
            {
                var pos = new GridPosition(x, y);
                var element = board.GetElement(pos);

                if (element != null)
                {
                    // If there's an element and writeIndex is below current position,
                    // this element needs to fall
                    if (writeIndex < y)
                    {
                        var targetPos = new GridPosition(x, writeIndex);
                        moves.Add(new FallMove(pos, targetPos));
                    }
                    writeIndex++;
                }
            }
        }

        /// <summary>
        /// Get empty positions at top of each column after falls complete.
        /// Returns positions from bottom-most empty to top, for each column.
        /// </summary>
        public List<GridPosition> GetEmptyTopPositions(BoardModel board)
        {
            var emptyPositions = new List<GridPosition>();

            for (int x = 0; x < board.Width; x++)
            {
                int emptyCount = CountEmptyCells(board, x);

                // Empty positions start from (Height - emptyCount) to (Height - 1)
                for (int i = 0; i < emptyCount; i++)
                {
                    int y = board.Height - emptyCount + i;
                    emptyPositions.Add(new GridPosition(x, y));
                }
            }

            return emptyPositions;
        }

        private int CountEmptyCells(BoardModel board, int x)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
            {
                var pos = new GridPosition(x, y);
                if (board.GetElement(pos) == null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
