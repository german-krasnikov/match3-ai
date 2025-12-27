using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Core;

namespace Match3.Grid
{
    /// <summary>
    /// Управляет сеткой ячеек. Реализует IGrid и IBoardState.
    /// </summary>
    public class GridComponent : MonoBehaviour, IGrid, IBoardState
    {
        public event Action OnBoardChanged;

        [Header("Config")]
        [SerializeField] private GridConfig _config;

        [Header("Prefabs")]
        [SerializeField] private CellComponent _cellPrefab;

        private CellComponent[,] _cells;

        public int Width => _config.Width;
        public int Height => _config.Height;
        public GridConfig Config => _config;

        public IEnumerable<GridPosition> AllPositions
        {
            get
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        yield return new GridPosition(x, y);
            }
        }

        public void Initialize()
        {
            CreateCells();
        }

        private void CreateCells()
        {
            _cells = new CellComponent[Width, Height];
            Vector3 offset = _config.GetGridOffset();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var pos = new GridPosition(x, y);
                    var worldPos = GridToWorld(pos);

                    var cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
                    cell.transform.localScale = Vector3.one * _config.CellSize;

                    // Шахматный паттерн
                    var color = (x + y) % 2 == 0 ? _config.CellColorA : _config.CellColorB;
                    cell.Initialize(pos, color);

                    _cells[x, y] = cell;
                }
            }
        }

        // === IGrid ===
        public Vector3 GridToWorld(GridPosition position)
        {
            Vector3 offset = _config.GetGridOffset();
            return new Vector3(
                position.X * _config.TotalCellSize + offset.x,
                position.Y * _config.TotalCellSize + offset.y,
                0
            ) + transform.position;
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            Vector3 offset = _config.GetGridOffset();
            Vector3 local = worldPosition - transform.position - offset;

            int x = Mathf.RoundToInt(local.x / _config.TotalCellSize);
            int y = Mathf.RoundToInt(local.y / _config.TotalCellSize);

            return new GridPosition(x, y);
        }

        public bool IsValidPosition(GridPosition position)
        {
            return position.X >= 0 && position.X < Width &&
                   position.Y >= 0 && position.Y < Height;
        }

        // === IBoardState ===
        public IPiece GetPieceAt(GridPosition position)
        {
            if (!IsValidPosition(position)) return null;
            return _cells[position.X, position.Y].CurrentPiece;
        }

        public void SetPieceAt(GridPosition position, IPiece piece)
        {
            if (!IsValidPosition(position)) return;

            _cells[position.X, position.Y].SetPiece(piece);
            piece.Position = position;
            piece.SetWorldPosition(GridToWorld(position));

            OnBoardChanged?.Invoke();
        }

        public void ClearCell(GridPosition position)
        {
            if (!IsValidPosition(position)) return;
            _cells[position.X, position.Y].Clear();
            OnBoardChanged?.Invoke();
        }

        public bool IsEmpty(GridPosition position)
        {
            if (!IsValidPosition(position)) return false;
            return _cells[position.X, position.Y].IsEmpty;
        }

        public CellComponent GetCell(GridPosition position)
        {
            if (!IsValidPosition(position)) return null;
            return _cells[position.X, position.Y];
        }
    }
}
