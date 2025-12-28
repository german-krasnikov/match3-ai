using System;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Match3.Core;
using Match3.Grid;

namespace Match3.Swap
{
    public class SwapComponent : MonoBehaviour, ISwapSystem
    {
        public event Action<Vector2Int, Vector2Int> OnSwapStarted;
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;

        [Header("Settings")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private Ease _swapEase = Ease.OutQuad;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;

        public bool CanSwap(Vector2Int pos1, Vector2Int pos2)
        {
            if (!_grid.IsValidPosition(pos1) || !_grid.IsValidPosition(pos2))
                return false;

            if (_grid.GetElementAt(pos1) == null || _grid.GetElementAt(pos2) == null)
                return false;

            return AreNeighbors(pos1, pos2);
        }

        public async Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2)
        {
            if (!CanSwap(pos1, pos2))
                return false;

            await ExecuteSwap(pos1, pos2);
            return true;
        }

        public async Task SwapBack(Vector2Int pos1, Vector2Int pos2)
        {
            await ExecuteSwap(pos1, pos2);
        }

        private bool AreNeighbors(Vector2Int p1, Vector2Int p2)
        {
            int dx = Mathf.Abs(p1.x - p2.x);
            int dy = Mathf.Abs(p1.y - p2.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private async Task ExecuteSwap(Vector2Int pos1, Vector2Int pos2)
        {
            OnSwapStarted?.Invoke(pos1, pos2);

            var element1 = _grid.GetElementAt(pos1);
            var element2 = _grid.GetElementAt(pos2);

            // Cache world positions before swap
            Vector3 worldPos1 = _grid.GridToWorld(pos1);
            Vector3 worldPos2 = _grid.GridToWorld(pos2);

            // Swap in grid data (SetElementAt also updates GridPosition)
            _grid.SetElementAt(pos1, element2);
            _grid.SetElementAt(pos2, element1);

            // Animate to swapped positions
            await AnimateSwap(element1.GameObject, element2.GameObject, worldPos2, worldPos1);

            OnSwapCompleted?.Invoke(pos1, pos2);
        }

        private async Task AnimateSwap(GameObject go1, GameObject go2, Vector3 target1, Vector3 target2)
        {
            var tween1 = go1.transform.DOMove(target1, _swapDuration).SetEase(_swapEase);
            var tween2 = go2.transform.DOMove(target2, _swapDuration).SetEase(_swapEase);

            await Task.WhenAll(
                tween1.AsyncWaitForCompletion(),
                tween2.AsyncWaitForCompletion()
            );
        }
    }
}
