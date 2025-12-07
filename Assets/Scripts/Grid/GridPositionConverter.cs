using Match3.Core;
using UnityEngine;

namespace Match3.Grid
{
    public class GridPositionConverter
    {
        private readonly float _cellSize;
        private readonly Vector2 _gridOrigin;

        public GridPositionConverter(float cellSize, Vector2 gridOrigin)
        {
            _cellSize = cellSize;
            _gridOrigin = gridOrigin;
        }

        public Vector3 GridToWorld(GridPosition pos)
            => new Vector3(
                _gridOrigin.x + pos.X * _cellSize,
                _gridOrigin.y + pos.Y * _cellSize,
                0);

        public GridPosition WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt((worldPos.x - _gridOrigin.x) / _cellSize);
            int y = Mathf.RoundToInt((worldPos.y - _gridOrigin.y) / _cellSize);
            return new GridPosition(x, y);
        }
    }
}
