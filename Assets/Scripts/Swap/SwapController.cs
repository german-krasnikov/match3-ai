using System;
using Match3.Core;
using Match3.Grid;
using Match3.Input;
using Match3.Match;
using UnityEngine;

namespace Match3.Swap
{
    public class SwapController : MonoBehaviour
    {
        public event Action<GridPosition, GridPosition> OnSwapComplete;
        public event Action OnSwapFailed;

        [SerializeField] private SwipeInputHandler _inputHandler;
        [SerializeField] private SwapAnimator _animator;
        [SerializeField] private GridView _gridView;

        private GridData _grid;
        private SwapValidator _validator;
        private bool _isSwapping;

        public bool IsSwapping => _isSwapping;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _validator = new SwapValidator(grid, new LineMatchFinder());
            _inputHandler.OnSwipeDetected += OnSwipeDetected;
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.OnSwipeDetected -= OnSwipeDetected;
        }

        private void OnSwipeDetected(GridPosition from, GridPosition direction)
        {
            if (_isSwapping) return;
            TrySwap(from, from + direction);
        }

        public void TrySwap(GridPosition a, GridPosition b)
        {
            if (_isSwapping) return;

            if (!_validator.AreNeighbors(a, b)) return;

            var elementA = _grid.GetElement(a);
            var elementB = _grid.GetElement(b);
            if (elementA == null || elementB == null) return;

            _isSwapping = true;
            _inputHandler.SetEnabled(false);

            bool isValid = _validator.WouldCreateMatch(a, b);

            if (isValid)
                ExecuteValidSwap(a, b, elementA, elementB);
            else
                ExecuteInvalidSwap(elementA, elementB);
        }

        private void ExecuteValidSwap(GridPosition a, GridPosition b, Elements.IElement elementA, Elements.IElement elementB)
        {
            _grid.SwapElements(a, b);
            elementA.Position = b;
            elementB.Position = a;

            _animator.AnimateSwap(elementA, elementB, b, a, () =>
            {
                _isSwapping = false;
                OnSwapComplete?.Invoke(a, b);
            });
        }

        private void ExecuteInvalidSwap(Elements.IElement elementA, Elements.IElement elementB)
        {
            _animator.AnimateInvalidSwap(elementA, elementB, () =>
            {
                _isSwapping = false;
                OnSwapFailed?.Invoke();
            });
        }

        public void EnableInput() => _inputHandler.SetEnabled(true);
    }
}
