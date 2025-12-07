using Match3.Core;
using Match3.Grid;
using Match3.Match;
using UnityEngine;

namespace Match3.Swap
{
    public class SwapValidator
    {
        private readonly GridData _grid;
        private readonly IMatchFinder _matchFinder;

        public SwapValidator(GridData grid, IMatchFinder matchFinder)
        {
            _grid = grid;
            _matchFinder = matchFinder;
        }

        public bool AreNeighbors(GridPosition a, GridPosition b)
        {
            var delta = a - b;
            return (Mathf.Abs(delta.X) == 1 && delta.Y == 0) ||
                   (delta.X == 0 && Mathf.Abs(delta.Y) == 1);
        }

        public bool WouldCreateMatch(GridPosition a, GridPosition b)
        {
            _grid.SwapElements(a, b);
            var matches = _matchFinder.FindMatchesAt(_grid, new[] { a, b });
            _grid.SwapElements(a, b);
            return matches.Count > 0;
        }

        public bool IsValidSwap(GridPosition a, GridPosition b)
        {
            if (!_grid.IsValidPosition(a) || !_grid.IsValidPosition(b))
                return false;

            if (_grid.GetElement(a) == null || _grid.GetElement(b) == null)
                return false;

            if (!AreNeighbors(a, b))
                return false;

            return WouldCreateMatch(a, b);
        }
    }
}
