using UnityEngine;
using Match3.Board;

namespace Match3.Swap
{
    public class SwapSystem
    {
        /// <summary>
        /// Checks if swap is valid: positions are adjacent and both contain gems.
        /// </summary>
        public bool IsValidSwap(Vector2Int from, Vector2Int to, BoardData board)
        {
            // Check both positions are valid
            if (!board.IsValidPosition(from) || !board.IsValidPosition(to))
                return false;

            // Check both positions have gems
            if (board.IsEmpty(from) || board.IsEmpty(to))
                return false;

            // Check positions are adjacent (Manhattan distance = 1)
            if (!AreAdjacent(from, to))
                return false;

            return true;
        }

        /// <summary>
        /// Performs swap in BoardData. Does NOT validate - call IsValidSwap first.
        /// </summary>
        public void PerformSwap(BoardData board, Vector2Int a, Vector2Int b)
        {
            board.SwapGems(a, b);
        }

        /// <summary>
        /// Checks if swap would result in a match.
        /// Performs temporary swap, checks, then reverts.
        /// </summary>
        /// <param name="a">First position</param>
        /// <param name="b">Second position</param>
        /// <param name="board">Board data</param>
        /// <param name="matchChecker">Function that checks if position has match</param>
        /// <returns>True if swap would create at least one match</returns>
        public bool WillMatch(Vector2Int a, Vector2Int b, BoardData board,
            System.Func<BoardData, Vector2Int, bool> matchChecker)
        {
            // Perform swap
            board.SwapGems(a, b);

            // Check for matches at both positions
            bool hasMatch = matchChecker(board, a) || matchChecker(board, b);

            // Revert swap
            board.SwapGems(a, b);

            return hasMatch;
        }

        /// <summary>
        /// Simple overload that always returns true (for testing without MatchSystem).
        /// Replace with proper MatchSystem integration in Step 6.
        /// </summary>
        public bool WillMatch(Vector2Int a, Vector2Int b, BoardData board)
        {
            // Placeholder - will be replaced with MatchSystem integration
            // For now, all valid swaps are allowed
            return true;
        }

        /// <summary>
        /// Checks if two positions are adjacent (Manhattan distance = 1).
        /// </summary>
        public bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
    }
}
