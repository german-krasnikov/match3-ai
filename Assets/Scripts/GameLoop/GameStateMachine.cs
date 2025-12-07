using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Destruction;
using Match3.Gravity;
using Match3.Grid;
using Match3.Match;
using Match3.Swap;
using UnityEngine;

namespace Match3.GameLoop
{
    public class GameStateMachine : MonoBehaviour
    {
        public event Action<GameState> OnStateChanged;
        public event Action OnGameOver;

        [SerializeField] private SwapController _swapController;
        [SerializeField] private MatchController _matchController;
        [SerializeField] private DestructionController _destructionController;
        [SerializeField] private GravityController _gravityController;

        private GridData _grid;
        private DeadlockChecker _deadlockChecker;
        private GameState _currentState;

        public GameState CurrentState => _currentState;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _deadlockChecker = new DeadlockChecker(new LineMatchFinder());

            SubscribeToEvents();
            SetState(GameState.Idle);
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            _swapController.OnSwapComplete += OnSwapComplete;
            _swapController.OnSwapFailed += OnSwapFailed;
            _matchController.OnMatchesFound += OnMatchesFound;
            _matchController.OnNoMatches += OnNoMatches;
            _destructionController.OnDestructionComplete += OnDestructionComplete;
            _gravityController.OnGravityComplete += OnGravityComplete;
        }

        private void UnsubscribeFromEvents()
        {
            if (_swapController != null)
            {
                _swapController.OnSwapComplete -= OnSwapComplete;
                _swapController.OnSwapFailed -= OnSwapFailed;
            }
            if (_matchController != null)
            {
                _matchController.OnMatchesFound -= OnMatchesFound;
                _matchController.OnNoMatches -= OnNoMatches;
            }
            if (_destructionController != null)
                _destructionController.OnDestructionComplete -= OnDestructionComplete;
            if (_gravityController != null)
                _gravityController.OnGravityComplete -= OnGravityComplete;
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            Debug.Log($"[Match3] State: {newState}");
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Idle:
                    _swapController.EnableInput();
                    break;

                case GameState.GameOver:
                    OnGameOver?.Invoke();
                    break;
            }
        }

        private void OnSwapComplete(GridPosition a, GridPosition b)
        {
            SetState(GameState.Matching);
            _matchController.CheckAt(a, b);
        }

        private void OnSwapFailed()
        {
            SetState(GameState.Idle);
        }

        private void OnMatchesFound(List<MatchData> matches)
        {
            SetState(GameState.Destroying);
            _destructionController.DestroyMatches(matches);
        }

        private void OnNoMatches()
        {
            SetState(GameState.Checking);
            CheckForDeadlock();
        }

        private void OnDestructionComplete(HashSet<GridPosition> destroyedPositions)
        {
            SetState(GameState.Falling);
            _gravityController.ApplyGravity(destroyedPositions);
        }

        private void OnGravityComplete(List<GridPosition> affectedPositions)
        {
            SetState(GameState.Matching);

            if (affectedPositions.Count > 0)
                _matchController.CheckAt(affectedPositions);
            else
                _matchController.CheckAll();
        }

        private void CheckForDeadlock()
        {
            if (_deadlockChecker.HasPossibleMoves(_grid))
                SetState(GameState.Idle);
            else
                SetState(GameState.GameOver);
        }
    }
}
