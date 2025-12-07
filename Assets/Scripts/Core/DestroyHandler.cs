using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Components.Board;
using Match3.Components.Animation;

namespace Match3.Core
{
    public class DestroyHandler : MonoBehaviour
    {
        public event Action<MatchResult> OnDestroyStarted;
        public event Action<List<Vector2Int>> OnTilesDestroyed;
        public event Action OnDestroyComplete;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private DestroyAnimator _animator;

        private bool _isProcessing;

        public bool IsProcessing => _isProcessing;

        public void DestroyMatches(MatchResult result)
        {
            if (_isProcessing || !result.HasMatches) return;

            _isProcessing = true;
            OnDestroyStarted?.Invoke(result);

            var tilesToDestroy = new List<TileComponent>();
            var positions = new List<Vector2Int>();

            foreach (var pos in result.AllPositions)
            {
                var cell = _grid.GetCell(pos);
                if (cell?.CurrentTile != null)
                {
                    tilesToDestroy.Add(cell.CurrentTile);
                    positions.Add(pos);
                }
            }

            foreach (var tile in tilesToDestroy)
                tile.IsMatched = true;

            _animator.AnimateDestroy(tilesToDestroy, () =>
            {
                foreach (var pos in positions)
                {
                    var cell = _grid.GetCell(pos);
                    if (cell != null)
                    {
                        var tile = cell.RemoveTile();
                        if (tile != null)
                            Destroy(tile.gameObject);
                    }
                }

                _isProcessing = false;
                OnTilesDestroyed?.Invoke(positions);
                OnDestroyComplete?.Invoke();
            });
        }

        public void DestroySingle(Vector2Int position)
        {
            var cell = _grid.GetCell(position);
            if (cell?.CurrentTile == null) return;

            var tile = cell.CurrentTile;
            tile.IsMatched = true;

            _animator.AnimateDestroySingle(tile, () =>
            {
                cell.RemoveTile();
                Destroy(tile.gameObject);
                OnTilesDestroyed?.Invoke(new List<Vector2Int> { position });
            });
        }
    }
}
