using System;
using UnityEngine;
using Match3.Data;
using Match3.Components.Board;

namespace Match3.Core
{
    public class BoardController : MonoBehaviour
    {
        public event Action OnBoardReady;
        public event Action<CellComponent, TileComponent> OnTileChanged;

        [Header("Components")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private TileSpawner _spawner;

        public GridComponent Grid => _grid;
        public int Width => _grid.Width;
        public int Height => _grid.Height;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            _grid.Initialize();
            SubscribeToCells();
            FillBoard();
            OnBoardReady?.Invoke();
        }

        public TileComponent GetTile(int x, int y) => _grid.GetCell(x, y)?.CurrentTile;
        public TileComponent GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        public void SwapTiles(Vector2Int posA, Vector2Int posB)
        {
            var cellA = _grid.GetCell(posA);
            var cellB = _grid.GetCell(posB);

            if (cellA == null || cellB == null) return;

            var tileA = cellA.RemoveTile();
            var tileB = cellB.RemoveTile();

            cellA.SetTile(tileB);
            cellB.SetTile(tileA);

            // Update world positions
            if (tileA != null) tileA.SetWorldPosition(_grid.GridToWorld(posB));
            if (tileB != null) tileB.SetWorldPosition(_grid.GridToWorld(posA));
        }

        public bool AreNeighbors(Vector2Int posA, Vector2Int posB)
        {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y) == 1;
        }

        private void SubscribeToCells()
        {
            foreach (var cell in _grid.GetAllCells())
            {
                cell.OnTileChanged += HandleTileChanged;
            }
        }

        private void HandleTileChanged(CellComponent cell, TileComponent tile)
        {
            OnTileChanged?.Invoke(cell, tile);
        }

        private void FillBoard()
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    if (!cell.CanHoldTile || !cell.IsEmpty) continue;

                    var tile = _spawner.SpawnTileWithoutMatch(cell.GridPosition, WouldCreateMatch);
                    cell.SetTile(tile);
                }
            }
        }

        private bool WouldCreateMatch(Vector2Int position, TileType type)
        {
            // Check horizontal (2 tiles to the left)
            if (CheckLine(position, Vector2Int.left, type, 2))
                return true;

            // Check vertical (2 tiles below)
            if (CheckLine(position, Vector2Int.down, type, 2))
                return true;

            return false;
        }

        private bool CheckLine(Vector2Int start, Vector2Int direction, TileType type, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                var tile = GetTile(start + direction * i);
                if (tile == null || tile.Type != type)
                    return false;
            }
            return true;
        }

        private void OnDisable()
        {
            if (_grid == null) return;

            foreach (var cell in _grid.GetAllCells())
            {
                if (cell != null)
                    cell.OnTileChanged -= HandleTileChanged;
            }
        }
    }
}
