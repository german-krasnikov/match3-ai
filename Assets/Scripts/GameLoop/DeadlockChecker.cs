using UnityEngine;
using Match3.Board;
using Match3.Matching;

namespace Match3.GameLoop
{
    /// <summary>
    /// Checks if any valid moves exist on the board.
    /// </summary>
    public static class DeadlockChecker
    {
        /// <summary>
        /// Returns true if at least one valid swap exists.
        /// </summary>
        public static bool HasPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            int width = board.Width;
            int height = board.Height;

            // Check horizontal swaps (with right neighbor)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x + 1, y);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        return true;
                }
            }

            // Check vertical swaps (with top neighbor)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x, y + 1);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        return true;
                }
            }

            return false;
        }

        private static bool WouldSwapCreateMatch(
            BoardComponent board,
            MatchFinder matchFinder,
            Vector2Int posA,
            Vector2Int posB)
        {
            var elementA = board.GetElement(posA);
            var elementB = board.GetElement(posB);

            if (elementA == null || elementB == null)
                return false;

            // Temporarily swap
            board.SwapElements(posA, posB);

            // Check for matches
            bool hasMatch = matchFinder.WouldCreateMatch(posA, posB);

            // Swap back
            board.SwapElements(posA, posB);

            return hasMatch;
        }

        /// <summary>
        /// Returns count of possible moves (for hints).
        /// </summary>
        public static int CountPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            int count = 0;
            int width = board.Width;
            int height = board.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x + 1, y);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        count++;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x, y + 1);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        count++;
                }
            }

            return count;
        }
    }
}
