using UnityEngine;
using Match3.Components.Board;

namespace Match3.Core
{
    public class SwapValidator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private MatchDetector _matchDetector;

        public bool IsValidSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!_grid.IsValidPosition(posA) || !_grid.IsValidPosition(posB))
                return false;

            if (!AreNeighbors(posA, posB))
                return false;

            var cellA = _grid.GetCell(posA);
            var cellB = _grid.GetCell(posB);

            if (cellA?.CurrentTile == null || cellB?.CurrentTile == null)
                return false;

            if (!cellA.CurrentTile.CanSwap || !cellB.CurrentTile.CanSwap)
                return false;

            return true;
        }

        public bool WillCreateMatch(Vector2Int posA, Vector2Int posB)
        {
            if (_matchDetector == null) return true;
            return _matchDetector.WouldCreateMatch(posA, posB);
        }

        private bool AreNeighbors(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
    }
}
