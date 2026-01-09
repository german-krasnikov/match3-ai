// Assets/Scripts/Gameplay/GameplayCoordinator.cs
using System;
using System.Collections.Generic;
using Common;
using Features.Board.Models;
using Features.Board.Views;
using Features.Board.Services;

namespace Gameplay
{
    /// <summary>
    /// State machine coordinating the full game loop.
    /// Pure C# - testable without Unity.
    /// </summary>
    public class GameplayCoordinator : IDisposable
    {
        private GameState _state = GameState.WaitingForInput;

        private readonly BoardModel _board;
        private readonly IBoardView _view;
        private readonly IInputService _input;
        private readonly IMatchService _matcher;
        private readonly IFallService _faller;
        private readonly ISpawnService _spawner;

        public GameState CurrentState => _state;
        public event Action<GameState> OnStateChanged;

        public GameplayCoordinator(
            BoardModel board,
            IBoardView view,
            IInputService input,
            IMatchService matcher,
            IFallService faller,
            ISpawnService spawner)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
            _faller = faller ?? throw new ArgumentNullException(nameof(faller));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));

            _input.OnSwapRequested += HandleSwapRequested;
        }

        private void SetState(GameState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnStateChanged?.Invoke(_state);
        }

        private void HandleSwapRequested(GridPosition from, GridPosition to)
        {
            if (_state != GameState.WaitingForInput) return;

            var elementFrom = _board.GetElement(from);
            var elementTo = _board.GetElement(to);

            if (elementFrom == null || elementTo == null) return;

            ProcessSwap(from, to);
        }

        private void ProcessSwap(GridPosition from, GridPosition to)
        {
            SetState(GameState.Swapping);
            _input.SetInputEnabled(false);

            _board.SwapElements(from, to);
            _view.SwapElements(from, to, () => OnSwapComplete(from, to));
        }

        private void OnSwapComplete(GridPosition from, GridPosition to)
        {
            SetState(GameState.Matching);

            var matchesFrom = _matcher.FindMatchesAt(_board, from);
            var matchesTo = _matcher.FindMatchesAt(_board, to);

            var allMatches = CombineMatches(matchesFrom, matchesTo);

            if (allMatches.Count == 0)
            {
                // Invalid swap - rollback
                _board.SwapElements(from, to);
                _view.SwapElements(from, to, OnRollbackComplete);
            }
            else
            {
                ProcessDestroy(allMatches);
            }
        }

        private List<GridPosition> CombineMatches(List<GridPosition> a, List<GridPosition> b)
        {
            var result = new List<GridPosition>();
            foreach (var pos in a)
                if (!result.Contains(pos)) result.Add(pos);
            foreach (var pos in b)
                if (!result.Contains(pos)) result.Add(pos);
            return result;
        }

        private void OnRollbackComplete()
        {
            SetState(GameState.WaitingForInput);
            _input.SetInputEnabled(true);
        }

        private void ProcessDestroy(List<GridPosition> matches)
        {
            SetState(GameState.Destroying);

            _view.DestroyElements(matches, () =>
            {
                _board.RemoveElements(matches);
                ProcessFall();
            });
        }

        private void ProcessFall()
        {
            SetState(GameState.Falling);

            var falls = _faller.CalculateFalls(_board);

            if (falls.Count == 0)
            {
                ProcessRefill();
                return;
            }

            _board.ApplyMoves(ToTupleList(falls));
            _view.MoveElements(falls, OnFallComplete);
        }

        private List<(GridPosition from, GridPosition to)> ToTupleList(List<FallMove> falls)
        {
            var result = new List<(GridPosition, GridPosition)>();
            foreach (var fall in falls)
                result.Add((fall.From, fall.To));
            return result;
        }

        private void OnFallComplete()
        {
            ProcessRefill();
        }

        private void ProcessRefill()
        {
            SetState(GameState.Refilling);

            var emptyPositions = _faller.GetEmptyTopPositions(_board);

            if (emptyPositions.Count == 0)
            {
                CheckForCascade();
                return;
            }

            var spawns = new List<(GridPosition pos, ElementType type)>();
            foreach (var pos in emptyPositions)
            {
                var type = _spawner.GetRandomElement();
                var element = new Element(type);
                _board.SetElement(pos, element);
                spawns.Add((pos, type));
            }

            _view.SpawnElements(spawns, OnRefillComplete);
        }

        private void OnRefillComplete()
        {
            CheckForCascade();
        }

        private void CheckForCascade()
        {
            SetState(GameState.Matching);

            var matches = _matcher.FindAllMatches(_board);

            if (matches.Count > 0)
            {
                ProcessDestroy(matches);
            }
            else
            {
                SetState(GameState.WaitingForInput);
                _input.SetInputEnabled(true);
            }
        }

        public void Dispose()
        {
            _input.OnSwapRequested -= HandleSwapRequested;
        }
    }
}
