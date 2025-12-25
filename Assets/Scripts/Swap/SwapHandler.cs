using System;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Input;
using Match3.Elements;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;

namespace Match3.Swap
{
    /// <summary>
    /// Handles swap logic: validates, animates, and checks for matches.
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
        [SerializeField] private InputBlocker _inputBlocker;
        [SerializeField] private SwapAnimator _swapAnimator;
        [SerializeField] private MatchFinder _matchFinder;
        [SerializeField] private DestroyHandler _destroyHandler;
        [SerializeField] private FallHandler _fallHandler;
        [SerializeField] private RefillHandler _refillHandler;

        private bool _isProcessing;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
            _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
            _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
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
            _inputBlocker.Block();

            OnSwapStarted?.Invoke(posA, posB);

            Vector3 targetPosA = _grid.GridToWorld(posB);
            Vector3 targetPosB = _grid.GridToWorld(posA);

            Vector3 originalPosA = elementA.transform.position;
            Vector3 originalPosB = elementB.transform.position;

            _swapAnimator.AnimateSwap(elementA, elementB, targetPosA, targetPosB, () =>
            {
                _board.SwapElements(posA, posB);

                bool hasMatch = CheckForMatch(posA, posB);

                if (hasMatch)
                {
                    CompleteSwap(posA, posB);
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
                OnSwapReverted?.Invoke(posA, posB);
                FinishSwap();
            });
        }

        private void CompleteSwap(Vector2Int posA, Vector2Int posB)
        {
            OnSwapCompleted?.Invoke(posA, posB);

            var matches = _matchFinder.FindAllMatches();
            if (matches.Count > 0)
            {
                _destroyHandler.DestroyMatches(matches);
            }
            else
            {
                FinishSwap();
            }
        }

        private void OnDestroyCompleted(int count)
        {
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            _refillHandler.ExecuteRefills();
        }

        private void OnRefillsCompleted()
        {
            // TODO: Stage 11 - Check for cascade matches here
            FinishSwap();
        }

        private void FinishSwap()
        {
            _isProcessing = false;
            _inputBlocker.Unblock();
        }

        private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
        {
            return _matchFinder.WouldCreateMatch(posA, posB);
        }
    }
}
