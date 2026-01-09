// Assets/Scripts/Features/Board/Presenters/BoardPresenter.cs
using System;
using System.Collections.Generic;
using Common;
using Features.Board.Models;
using Features.Board.Views;
using Features.Board.Services;

namespace Features.Board.Presenters
{
    public class BoardPresenter : IDisposable
    {
        private readonly BoardModel _model;
        private readonly IBoardView _view;
        private readonly IMatchService _matchService;
        private readonly IInputService _inputService;
        private readonly IFallService _fallService;

        private bool _isProcessing;

        public event Action OnMatchesDestroyed;
        public event Action OnFallComplete;

        public BoardPresenter(
            BoardModel model,
            IBoardView view,
            float cellSize,
            IMatchService matchService = null,
            IInputService inputService = null,
            IFallService fallService = null)
        {
            _model = model;
            _view = view;
            _matchService = matchService;
            _inputService = inputService;
            _fallService = fallService;

            _view.Initialize(_model.Width, _model.Height, cellSize);

            _model.OnElementAdded += HandleElementAdded;
            _model.OnElementRemoved += HandleElementRemoved;

            if (_inputService != null)
            {
                _inputService.OnSwapRequested += HandleSwapRequested;
            }

            if (_view is IBoardView viewWithEvents)
            {
                viewWithEvents.OnSwapAttempted += HandleSwapAttempted;
            }
        }

        private void HandleElementAdded(GridPosition pos, ElementType type)
        {
            _view.CreateElement(pos, type);
        }

        private void HandleElementRemoved(GridPosition pos)
        {
            _view.RemoveElement(pos);
        }

        private void HandleSwapAttempted(GridPosition from, GridPosition to)
        {
            _inputService?.RequestSwap(from, to);
        }

        private void HandleSwapRequested(GridPosition from, GridPosition to)
        {
            if (_isProcessing) return;

            TrySwap(from, to);
        }

        /// <summary>
        /// Attempt to swap elements at two positions.
        /// If swap creates a match, it persists. Otherwise, elements swap back.
        /// </summary>
        public void TrySwap(GridPosition from, GridPosition to)
        {
            if (_isProcessing) return;

            var elementFrom = _model.GetElement(from);
            var elementTo = _model.GetElement(to);

            if (elementFrom == null || elementTo == null)
                return;

            _isProcessing = true;
            _inputService?.SetInputEnabled(false);

            // Perform swap in model first
            _model.SwapElements(from, to);

            // Animate the swap
            _view.SwapElements(from, to, () => OnSwapAnimationComplete(from, to));
        }

        private void OnSwapAnimationComplete(GridPosition from, GridPosition to)
        {
            bool createsMatch = CheckForMatches(from, to);

            if (!createsMatch)
            {
                // Rollback: swap back in model
                _model.SwapElements(from, to);

                // Animate swap back
                _view.SwapElements(from, to, OnRollbackComplete);
            }
            else
            {
                // Valid swap - process matches
                ProcessMatches(from, to);
            }
        }

        private bool CheckForMatches(GridPosition from, GridPosition to)
        {
            if (_matchService == null) return true;

            var matchesFrom = _matchService.FindMatchesAt(_model, from);
            var matchesTo = _matchService.FindMatchesAt(_model, to);

            return matchesFrom.Count > 0 || matchesTo.Count > 0;
        }

        private void ProcessMatches(GridPosition from, GridPosition to)
        {
            if (_matchService == null)
            {
                OnProcessingComplete();
                return;
            }

            var allMatches = new List<GridPosition>();

            var matchesFrom = _matchService.FindMatchesAt(_model, from);
            var matchesTo = _matchService.FindMatchesAt(_model, to);

            AddUniquePositions(allMatches, matchesFrom);
            AddUniquePositions(allMatches, matchesTo);

            if (allMatches.Count > 0)
            {
                DestroyMatches(allMatches);
            }
            else
            {
                OnProcessingComplete();
            }
        }

        private void AddUniquePositions(List<GridPosition> target, List<GridPosition> source)
        {
            foreach (var pos in source)
            {
                if (!target.Contains(pos))
                {
                    target.Add(pos);
                }
            }
        }

        /// <summary>
        /// Destroy matched elements: animate in view, then remove from model.
        /// </summary>
        public void DestroyMatches(List<GridPosition> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                OnProcessingComplete();
                return;
            }

            // Animate destruction in view
            _view.DestroyElements(matches, () => OnDestroyAnimationComplete(matches));
        }

        private void OnDestroyAnimationComplete(List<GridPosition> destroyedPositions)
        {
            // Remove from model after animation
            _model.RemoveElements(destroyedPositions);

            OnMatchesDestroyed?.Invoke();

            // Process falls after destroy
            ProcessFalls();
        }

        /// <summary>
        /// Process element falls after matches are destroyed.
        /// </summary>
        private void ProcessFalls()
        {
            if (_fallService == null)
            {
                OnProcessingComplete();
                return;
            }

            var fallMoves = _fallService.CalculateFalls(_model);

            if (fallMoves.Count == 0)
            {
                OnProcessingComplete();
                return;
            }

            // Animate falls in view
            _view.MoveElements(fallMoves, () => OnFallAnimationComplete(fallMoves));
        }

        private void OnFallAnimationComplete(List<FallMove> moves)
        {
            // Apply moves to model after animation
            foreach (var move in moves)
            {
                _model.MoveElement(move.From, move.To);
            }

            OnFallComplete?.Invoke();

            // After fall - could check for new matches (cascade) in Step 9
            // For now, complete processing
            OnProcessingComplete();
        }

        private void OnRollbackComplete()
        {
            OnProcessingComplete();
        }

        private void OnProcessingComplete()
        {
            _isProcessing = false;
            _inputService?.SetInputEnabled(true);
        }

        public void SyncViewWithModel()
        {
            _view.Clear();

            for (int x = 0; x < _model.Width; x++)
            {
                for (int y = 0; y < _model.Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    var element = _model.GetElement(pos);
                    if (element != null)
                    {
                        _view.CreateElement(pos, element.Type);
                    }
                }
            }
        }

        public bool IsProcessing => _isProcessing;

        public void Dispose()
        {
            _model.OnElementAdded -= HandleElementAdded;
            _model.OnElementRemoved -= HandleElementRemoved;

            if (_inputService != null)
            {
                _inputService.OnSwapRequested -= HandleSwapRequested;
            }

            if (_view is IBoardView viewWithEvents)
            {
                viewWithEvents.OnSwapAttempted -= HandleSwapAttempted;
            }
        }
    }
}
