using System;
using UnityEngine;
using Match3.Common;
using Match3.Components.Board;
using Match3.Components.Animation;

namespace Match3.Core
{
    public class SwapController : MonoBehaviour
    {
        public event Action OnSwapStarted;
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
        public event Action OnSwapFailed;
        public event Action OnSwapInvalid;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SwapValidator _validator;
        [SerializeField] private SwapAnimator _animator;

        private enum SwapState { Idle, Animating, Reverting }
        private SwapState _state = SwapState.Idle;

        private Vector2Int _swapPosA;
        private Vector2Int _swapPosB;
        private TileComponent _tileA;
        private TileComponent _tileB;

        public bool IsProcessing => _state != SwapState.Idle;

        private void OnEnable()
        {
            _animator.OnSwapComplete += OnSwapAnimationComplete;
            _animator.OnRevertComplete += OnRevertAnimationComplete;
        }

        private void OnDisable()
        {
            _animator.OnSwapComplete -= OnSwapAnimationComplete;
            _animator.OnRevertComplete -= OnRevertAnimationComplete;
        }

        public void TrySwap(Vector2Int fromPos, SwipeDirection direction)
        {
            if (_state != SwapState.Idle) return;

            Vector2Int toPos = GetTargetPosition(fromPos, direction);
            TrySwap(fromPos, toPos);
        }

        public void TrySwap(Vector2Int posA, Vector2Int posB)
        {
            if (_state != SwapState.Idle) return;

            if (!_validator.IsValidSwap(posA, posB))
            {
                OnSwapInvalid?.Invoke();
                return;
            }

            _swapPosA = posA;
            _swapPosB = posB;
            _tileA = _grid.GetCell(posA).CurrentTile;
            _tileB = _grid.GetCell(posB).CurrentTile;

            _tileA.SetMoving(true);
            _tileB.SetMoving(true);

            _state = SwapState.Animating;
            OnSwapStarted?.Invoke();

            _animator.AnimateSwap(_tileA.transform, _tileB.transform);
        }

        private void OnSwapAnimationComplete()
        {
            SwapTilesInGrid(_swapPosA, _swapPosB);

            if (_validator.WillCreateMatch(_swapPosA, _swapPosB))
            {
                _tileA.SetMoving(false);
                _tileB.SetMoving(false);
                _state = SwapState.Idle;
                OnSwapCompleted?.Invoke(_swapPosA, _swapPosB);
            }
            else
            {
                _state = SwapState.Reverting;
                _animator.AnimateSwap(_tileA.transform, _tileB.transform, isRevert: true);
            }
        }

        private void OnRevertAnimationComplete()
        {
            SwapTilesInGrid(_swapPosA, _swapPosB);

            _tileA.SetMoving(false);
            _tileB.SetMoving(false);
            _state = SwapState.Idle;
            OnSwapFailed?.Invoke();
        }

        private void SwapTilesInGrid(Vector2Int posA, Vector2Int posB)
        {
            var cellA = _grid.GetCell(posA);
            var cellB = _grid.GetCell(posB);

            var tempTile = cellA.CurrentTile;
            cellA.SetTile(cellB.CurrentTile);
            cellB.SetTile(tempTile);
        }

        private Vector2Int GetTargetPosition(Vector2Int from, SwipeDirection direction)
        {
            return direction switch
            {
                SwipeDirection.Up => from + Vector2Int.up,
                SwipeDirection.Down => from + Vector2Int.down,
                SwipeDirection.Left => from + Vector2Int.left,
                SwipeDirection.Right => from + Vector2Int.right,
                _ => from
            };
        }
    }
}
