using System;
using UnityEngine;
using Match3.Core;

namespace Match3.Grid
{
    public class GridComponent : MonoBehaviour, IGrid
    {
        public event Action<Vector2Int, IGridElement> OnElementPlaced;
        public event Action<Vector2Int> OnCellCleared;

        [Header("Grid Settings")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector2 _origin = Vector2.zero;

        private IGridElement[,] _grid;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;

        private void Awake()
        {
            _grid = new IGridElement[_width, _height];
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float x = _origin.x + gridPos.x * _cellSize + _cellSize * 0.5f;
            float y = _origin.y + gridPos.y * _cellSize + _cellSize * 0.5f;
            return new Vector3(x, y, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _origin.x) / _cellSize);
            int y = Mathf.FloorToInt((worldPos.y - _origin.y) / _cellSize);
            return new Vector2Int(x, y);
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width &&
                   pos.y >= 0 && pos.y < _height;
        }

        public IGridElement GetElementAt(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return null;
            return _grid[pos.x, pos.y];
        }

        public void SetElementAt(Vector2Int pos, IGridElement element)
        {
            if (!IsValidPosition(pos))
                return;

            _grid[pos.x, pos.y] = element;

            if (element != null)
            {
                element.GridPosition = pos;
                OnElementPlaced?.Invoke(pos, element);
            }
        }

        public void ClearCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return;

            _grid[pos.x, pos.y] = null;
            OnCellCleared?.Invoke(pos);
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _occupiedCellColor = new Color(0f, 1f, 0f, 0.2f);

        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;

            Gizmos.color = _gridColor;

            // Vertical lines
            for (int x = 0; x <= _width; x++)
            {
                Vector3 start = new Vector3(_origin.x + x * _cellSize, _origin.y, 0);
                Vector3 end = new Vector3(_origin.x + x * _cellSize, _origin.y + _height * _cellSize, 0);
                Gizmos.DrawLine(start, end);
            }

            // Horizontal lines
            for (int y = 0; y <= _height; y++)
            {
                Vector3 start = new Vector3(_origin.x, _origin.y + y * _cellSize, 0);
                Vector3 end = new Vector3(_origin.x + _width * _cellSize, _origin.y + y * _cellSize, 0);
                Gizmos.DrawLine(start, end);
            }

            // Occupied cells
            if (_grid == null) return;

            Gizmos.color = _occupiedCellColor;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid[x, y] != null)
                    {
                        Vector3 center = GridToWorld(new Vector2Int(x, y));
                        Gizmos.DrawCube(center, new Vector3(_cellSize * 0.9f, _cellSize * 0.9f, 0.1f));
                    }
                }
            }
        }
#endif
    }
}
