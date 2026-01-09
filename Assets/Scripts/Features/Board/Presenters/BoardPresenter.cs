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
        private readonly ISpawnService _spawnService;

        private bool _isProcessing;

        public event Action OnMatchesDestroyed;
        public event Action OnFallComplete;
        public event Action OnRefillComplete;
        public event Action OnCascadeComplete;

        public BoardPresenter(
            BoardModel model,
            IBoardView view,
            float cellSize,
            IMatchService matchService = null,
            IInputService inputService = null,
            IFallService fallService = null,
            ISpawnService spawnService = null)
        {
            _model = model;
            _view = view;
            _matchService = matchService;
            _inputService = inputService;
            _fallService = fallService;
            _spawnService = spawnService;

            _view.Initialize(_model.Width, _model.Height, cellSize);

            _model.OnElementAdded += HandleElementAdded;
            _model.OnElementRemoved += HandleElementRemoved;

            if (_inputService != null)
                _inputService.OnSwapRequested += HandleSwapRequested;

            _view.OnSwapAttempted += HandleSwapAttempted;
        }

        private void HandleElementAdded(GridPosition pos, ElementType type) => _view.CreateElement(pos, type);
        private void HandleElementRemoved(GridPosition pos) => _view.RemoveElement(pos);

        private void HandleSwapAttempted(GridPosition from, GridPosition to) => _inputService?.RequestSwap(from, to);

        private void HandleSwapRequested(GridPosition from, GridPosition to)
        {
            if (_isProcessing) return;
            TrySwap(from, to);
        }

        public void TrySwap(GridPosition from, GridPosition to)
        {
            if (_isProcessing) return;

            var elementFrom = _model.GetElement(from);
            var elementTo = _model.GetElement(to);

            if (elementFrom == null || elementTo == null)
                return;

            _isProcessing = true;
            _inputService?.SetInputEnabled(false);

            _model.SwapElements(from, to);
            _view.SwapElements(from, to, () => OnSwapAnimationComplete(from, to));
        }

        private void OnSwapAnimationComplete(GridPosition from, GridPosition to)
        {
            bool createsMatch = CheckForMatches(from, to);

            if (!createsMatch)
            {
                _model.SwapElements(from, to);
                _view.SwapElements(from, to, OnProcessingComplete);
            }
            else
            {
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
            AddUniquePositions(allMatches, _matchService.FindMatchesAt(_model, from));
            AddUniquePositions(allMatches, _matchService.FindMatchesAt(_model, to));

            if (allMatches.Count > 0)
                DestroyMatches(allMatches);
            else
                OnProcessingComplete();
        }

        private void AddUniquePositions(List<GridPosition> target, List<GridPosition> source)
        {
            foreach (var pos in source)
                if (!target.Contains(pos))
                    target.Add(pos);
        }

        public void DestroyMatches(List<GridPosition> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                CheckCascade();
                return;
            }

            _view.DestroyElements(matches, () => OnDestroyAnimationComplete(matches));
        }

        private void OnDestroyAnimationComplete(List<GridPosition> destroyedPositions)
        {
            _model.RemoveElements(destroyedPositions);
            OnMatchesDestroyed?.Invoke();
            ProcessFalls();
        }

        private void ProcessFalls()
        {
            if (_fallService == null)
            {
                ProcessRefill();
                return;
            }

            var fallMoves = _fallService.CalculateFalls(_model);

            if (fallMoves.Count == 0)
            {
                ProcessRefill();
                return;
            }

            _view.MoveElements(fallMoves, () => OnFallAnimationComplete(fallMoves));
        }

        private void OnFallAnimationComplete(List<FallMove> moves)
        {
            foreach (var move in moves)
                _model.MoveElement(move.From, move.To);

            OnFallComplete?.Invoke();
            ProcessRefill();
        }

        private void ProcessRefill()
        {
            if (_fallService == null || _spawnService == null)
            {
                CheckCascade();
                return;
            }

            var emptyPositions = _fallService.GetEmptyTopPositions(_model);

            if (emptyPositions.Count == 0)
            {
                CheckCascade();
                return;
            }

            var spawns = new List<SpawnData>();
            var columnEmptyCounts = new Dictionary<int, int>();

            foreach (var pos in emptyPositions)
            {
                if (!columnEmptyCounts.ContainsKey(pos.X))
                    columnEmptyCounts[pos.X] = 0;
                columnEmptyCounts[pos.X]++;
            }

            foreach (var pos in emptyPositions)
            {
                var type = _spawnService.GetRandomElement();
                int fallDistance = columnEmptyCounts[pos.X];
                spawns.Add(new SpawnData(pos, type, fallDistance));
            }

            _view.SpawnElements(spawns, () => OnSpawnAnimationComplete(spawns));
        }

        private void OnSpawnAnimationComplete(List<SpawnData> spawns)
        {
            foreach (var spawn in spawns)
                _model.SetElement(spawn.Position, new Element(spawn.Type));

            OnRefillComplete?.Invoke();
            CheckCascade();
        }

        private void CheckCascade()
        {
            if (_matchService == null)
            {
                OnCascadeComplete?.Invoke();
                OnProcessingComplete();
                return;
            }

            var matches = _matchService.FindAllMatches(_model);

            if (matches.Count > 0)
            {
                DestroyMatches(matches);
            }
            else
            {
                OnCascadeComplete?.Invoke();
                OnProcessingComplete();
            }
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
                        _view.CreateElement(pos, element.Type);
                }
            }
        }

        public bool IsProcessing => _isProcessing;

        public void Dispose()
        {
            _model.OnElementAdded -= HandleElementAdded;
            _model.OnElementRemoved -= HandleElementRemoved;

            if (_inputService != null)
                _inputService.OnSwapRequested -= HandleSwapRequested;

            _view.OnSwapAttempted -= HandleSwapAttempted;
        }
    }
}
