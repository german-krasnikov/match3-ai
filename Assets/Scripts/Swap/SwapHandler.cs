using System;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Input;
using Match3.Elements;
using Match3.Matching;

namespace Match3.Swap
{
    /// <summary>
    /// Handles swap logic: validates, animates, checks for matches.
    /// Game loop coordination is handled by GameLoopController.
    /// </summary>
    public class SwapHandler : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapStarted;
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
        public event Action<Vector2Int, Vector2Int> OnSwapReverted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private InputDetector _inputDetector;
        [SerializeField] private SwapAnimator _swapAnimator;
        [SerializeField] private MatchFinder _matchFinder;

        private bool _isProcessing;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
        }

        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            HandleSwapRequest(posA, posB);
        }

        private void HandleSwapRequest(Vector2Int posA, Vector2Int posB)
        {
            if (_isProcessing) return;
            if (!CanSwap(posA, posB)) return;

            var elementA = _board.GetElement(posA);
            var elementB = _board.GetElement(posB);

            if (elementA == null || elementB == null) return;

            StartSwap(posA, posB, elementA, elementB);
        }

        private bool CanSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!_grid.IsValidPosition(posA) || !_grid.IsValidPosition(posB))
                return false;

            int dx = Mathf.Abs(posA.x - posB.x);
            int dy = Mathf.Abs(posA.y - posB.y);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private void StartSwap(Vector2Int posA, Vector2Int posB,
            ElementComponent elementA, ElementComponent elementB)
        {
            _isProcessing = true;
            OnSwapStarted?.Invoke(posA, posB);

            Vector3 targetPosA = _grid.GridToWorld(posB);
            Vector3 targetPosB = _grid.GridToWorld(posA);

            Vector3 originalPosA = elementA.transform.position;
            Vector3 originalPosB = elementB.transform.position;

            _swapAnimator.AnimateSwap(elementA, elementB, targetPosA, targetPosB, () =>
            {
                _board.SwapElements(posA, posB);

                bool hasMatch = _matchFinder.WouldCreateMatch(posA, posB);

                if (hasMatch)
                {
                    _isProcessing = false;
                    OnSwapCompleted?.Invoke(posA, posB);
                }
                else
                {
                    RevertSwap(posA, posB, elementA, elementB, originalPosA, originalPosB);
                }
            });
        }

        private void RevertSwap(Vector2Int posA, Vector2Int posB,
            ElementComponent elementA, ElementComponent elementB,
            Vector3 originalPosA, Vector3 originalPosB)
        {
            _board.SwapElements(posA, posB);

            _swapAnimator.AnimateRevert(elementA, elementB, originalPosA, originalPosB, () =>
            {
                _isProcessing = false;
                OnSwapReverted?.Invoke(posA, posB);
            });
        }
    }
}
