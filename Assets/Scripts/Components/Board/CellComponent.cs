using System;
using UnityEngine;
using Match3.Data;
using Match3.Interfaces;

namespace Match3.Components.Board
{
    public class CellComponent : MonoBehaviour, ICell
    {
        public event Action<CellComponent, TileComponent> OnTileChanged;

        [SerializeField] private SpriteRenderer _backgroundRenderer;

        private Vector2Int _gridPosition;
        private CellType _cellType;
        private TileComponent _currentTile;

        public Vector2Int GridPosition => _gridPosition;
        public CellType CellType => _cellType;
        public TileComponent CurrentTile => _currentTile;
        public bool IsEmpty => _currentTile == null;
        public bool CanHoldTile => _cellType == CellType.Normal || _cellType == CellType.Spawner;
        public bool IsSpawner => _cellType == CellType.Spawner;

        public void Initialize(Vector2Int position, CellType type)
        {
            _gridPosition = position;
            _cellType = type;
            UpdateVisual();
        }

        public void SetTile(TileComponent tile)
        {
            _currentTile = tile;

            if (tile != null)
            {
                tile.SetGridPosition(_gridPosition);
            }

            OnTileChanged?.Invoke(this, tile);
        }

        public TileComponent RemoveTile()
        {
            var tile = _currentTile;
            _currentTile = null;
            OnTileChanged?.Invoke(this, null);
            return tile;
        }

        public void ClearTile()
        {
            _currentTile = null;
        }

        private void UpdateVisual()
        {
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.enabled = _cellType != CellType.Empty;

                _backgroundRenderer.color = _cellType switch
                {
                    CellType.Blocked => new Color(0.3f, 0.3f, 0.3f),
                    CellType.Spawner => new Color(0.9f, 0.9f, 1f),
                    _ => Color.white
                };
            }
        }
    }
}
