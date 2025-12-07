using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Data;

namespace Match3.Components.Board
{
    public class GridComponent : MonoBehaviour
    {
        public event Action OnGridInitialized;

        [Header("Settings")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector3 _originOffset = Vector3.zero;

        [Header("Prefabs")]
        [SerializeField] private CellComponent _cellPrefab;

        private CellComponent[,] _cells;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;

        public void Initialize(CellType[,] layout = null)
        {
            _cells = new CellComponent[_width, _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cellType = layout != null ? layout[x, y] : GetDefaultCellType(x, y);
                    CreateCell(x, y, cellType);
                }
            }

            OnGridInitialized?.Invoke();
        }

        public CellComponent GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y)) return null;
            return _cells[x, y];
        }

        public CellComponent GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public bool IsValidPosition(Vector2Int pos) => IsValidPosition(pos.x, pos.y);

        public List<CellComponent> GetNeighbors(Vector2Int position)
        {
            var neighbors = new List<CellComponent>(4);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in directions)
            {
                var cell = GetCell(position + dir);
                if (cell != null)
                    neighbors.Add(cell);
            }

            return neighbors;
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * _cellSize, gridPos.y * _cellSize, 0) + _originOffset;
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            var localPos = worldPos - _originOffset;
            return new Vector2Int(
                Mathf.RoundToInt(localPos.x / _cellSize),
                Mathf.RoundToInt(localPos.y / _cellSize)
            );
        }

        public bool TryWorldToGrid(Vector2 worldPos, out Vector2Int gridPos)
        {
            gridPos = WorldToGrid(worldPos);
            return IsValidPosition(gridPos);
        }

        public IEnumerable<CellComponent> GetAllCells()
        {
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    yield return _cells[x, y];
        }

        public IEnumerable<CellComponent> GetRow(int y)
        {
            for (int x = 0; x < _width; x++)
                yield return _cells[x, y];
        }

        public IEnumerable<CellComponent> GetColumn(int x)
        {
            for (int y = 0; y < _height; y++)
                yield return _cells[x, y];
        }

        private void CreateCell(int x, int y, CellType type)
        {
            var position = new Vector2Int(x, y);
            var worldPos = GridToWorld(position);

            var cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
            cell.name = $"Cell_{x}_{y}";
            cell.Initialize(position, type);

            _cells[x, y] = cell;
        }

        private CellType GetDefaultCellType(int x, int y)
        {
            return y == _height - 1 ? CellType.Spawner : CellType.Normal;
        }
    }
}
