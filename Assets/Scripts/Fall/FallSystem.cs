using System.Collections.Generic;
using UnityEngine;
using Match3.Board;

namespace Match3.Fall
{
    public class FallSystem
    {
        /// <summary>
        /// Calculates all fall moves needed to fill empty cells.
        /// Gems fall straight down. Returns moves sorted by column, then by row (bottom first).
        /// </summary>
        public List<FallMove> CalculateFalls(BoardData board)
        {
            var moves = new List<FallMove>();

            // Process each column independently
            for (int x = 0; x < board.Width; x++)
            {
                CalculateFallsForColumn(board, x, moves);
            }

            return moves;
        }

        /// <summary>
        /// Applies fall moves to BoardData.
        /// IMPORTANT: Apply from bottom to top to avoid overwriting.
        /// Does NOT trigger BoardData events (MoveGem is silent).
        /// </summary>
        public void ApplyFalls(BoardData board, List<FallMove> moves)
        {
            // Sort moves: process bottom rows first within each column
            // This ensures we don't overwrite gems that haven't moved yet
            moves.Sort((a, b) =>
            {
                if (a.From.x != b.From.x)
                    return a.From.x.CompareTo(b.From.x);
                return a.To.y.CompareTo(b.To.y); // Lower target first
            });

            foreach (var move in moves)
            {
                board.MoveGem(move.From, move.To);
            }
        }

        /// <summary>
        /// Counts empty cells in column (for spawning new gems).
        /// </summary>
        public int CountEmptyInColumn(BoardData board, int column)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
            {
                if (board.IsEmpty(new Vector2Int(column, y)))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Returns empty cell positions in column from bottom to top.
        /// Used for determining where new gems should land.
        /// </summary>
        public List<Vector2Int> GetEmptyPositionsInColumn(BoardData board, int column)
        {
            var positions = new List<Vector2Int>();
            for (int y = 0; y < board.Height; y++)
            {
                var pos = new Vector2Int(column, y);
                if (board.IsEmpty(pos))
                    positions.Add(pos);
            }
            return positions;
        }

        // --- Private Helpers ---

        private void CalculateFallsForColumn(BoardData board, int column, List<FallMove> moves)
        {
            // Track where the next gem should land
            int writeIndex = 0;

            // Scan from bottom to top
            for (int readIndex = 0; readIndex < board.Height; readIndex++)
            {
                var pos = new Vector2Int(column, readIndex);

                if (!board.IsEmpty(pos))
                {
                    // Gem exists at readIndex
                    if (readIndex != writeIndex)
                    {
                        // Gem needs to fall
                        var from = new Vector2Int(column, readIndex);
                        var to = new Vector2Int(column, writeIndex);
                        moves.Add(new FallMove(from, to));
                    }
                    writeIndex++;
                }
                // If empty, writeIndex stays put, waiting for next gem
            }
        }
    }
}
