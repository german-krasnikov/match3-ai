using Match3.Core;
using Match3.Grid;
using Match3.Match;

namespace Match3.GameLoop
{
    public class DeadlockChecker
    {
        private readonly IMatchFinder _matchFinder;

        public DeadlockChecker(IMatchFinder matchFinder)
        {
            _matchFinder = matchFinder;
        }

        public bool HasPossibleMoves(GridData grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new GridPosition(x, y);

                    if (CheckSwap(grid, pos, GridPosition.Right)) return true;
                    if (CheckSwap(grid, pos, GridPosition.Up)) return true;
                }
            }
            return false;
        }

        private bool CheckSwap(GridData grid, GridPosition pos, GridPosition direction)
        {
            var neighbor = pos + direction;
            if (!grid.IsValidPosition(neighbor)) return false;

            var elementA = grid.GetElement(pos);
            var elementB = grid.GetElement(neighbor);

            if (elementA == null || elementB == null) return false;
            if (elementA.Type == elementB.Type) return false;

            grid.SwapElements(pos, neighbor);
            var matches = _matchFinder.FindMatchesAt(grid, new[] { pos, neighbor });
            grid.SwapElements(pos, neighbor);

            return matches.Count > 0;
        }
    }
}
