using System;
using UnityEngine;

namespace Match3.Grid
{
    public class GridComponent : MonoBehaviour
    {
        public event Action OnGridReady;

        [Header("Configuration")]
        [SerializeField] private GridData _gridData;

        private Cell[,] _cells;

        public int Width => _gridData.Width;
        public int Height => _gridData.Height;
        public GridData Data => _gridData;

        private void Awake()
        {
            InitializeGrid();
            OnGridReady?.Invoke();
        }

        private void InitializeGrid()
        {
            _cells = new Cell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _cells[x, y] = new Cell(x, y);
                }
            }
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float step = _gridData.Step;
            float halfCell = _gridData.CellSize * 0.5f;

            float x = transform.position.x + gridPos.x * step + halfCell;
            float y = transform.position.y + gridPos.y * step + halfCell;

            return new Vector3(x, y, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float step = _gridData.Step;
            Vector3 localPos = worldPos - transform.position;

            int x = Mathf.FloorToInt(localPos.x / step);
            int y = Mathf.FloorToInt(localPos.y / step);

            return new Vector2Int(x, y);
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width &&
                   pos.y >= 0 && pos.y < Height;
        }

        public Cell GetCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), $"Position {pos} is outside grid bounds");

            return _cells[pos.x, pos.y];
        }

        private void OnDrawGizmos()
        {
            if (_gridData == null) return;
            DrawGridGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (_gridData == null) return;
            DrawGridGizmos(true);
        }

        private void DrawGridGizmos(bool selected)
        {
            Gizmos.color = selected ? Color.cyan : new Color(0.5f, 0.5f, 0.5f, 0.5f);

            float cellSize = _gridData.CellSize;
            float step = _gridData.Step;

            for (int x = 0; x < _gridData.Width; x++)
            {
                for (int y = 0; y < _gridData.Height; y++)
                {
                    Vector3 center = GridToWorldEditor(x, y);
                    Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
                }
            }

            if (selected)
            {
                Gizmos.color = Color.yellow;
                Vector3 gridCenter = transform.position + new Vector3(
                    _gridData.Width * step * 0.5f - _gridData.Spacing * 0.5f,
                    _gridData.Height * step * 0.5f - _gridData.Spacing * 0.5f,
                    0f
                );
                Vector3 gridSize = new Vector3(
                    _gridData.Width * step - _gridData.Spacing,
                    _gridData.Height * step - _gridData.Spacing,
                    0f
                );
                Gizmos.DrawWireCube(gridCenter, gridSize);
            }
        }

        private Vector3 GridToWorldEditor(int x, int y)
        {
            float step = _gridData.Step;
            float halfCell = _gridData.CellSize * 0.5f;

            return new Vector3(
                transform.position.x + x * step + halfCell,
                transform.position.y + y * step + halfCell,
                0f
            );
        }
    }
}
