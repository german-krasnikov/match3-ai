using System;
using UnityEngine;
using Match3.Board;
using Match3.Input;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;
using Match3.Swap;

namespace Match3.GameLoop
{
    /// <summary>
    /// Coordinates the game loop: swap → match → destroy → fall → refill → cascade.
    /// </summary>
    public class GameLoopController : MonoBehaviour
    {
        public event Action<GameState> OnStateChanged;
        public event Action OnCascadeStarted;
        public event Action<int, int> OnCascadeCompleted; // totalDestroyed, cascadeLevel

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private InputBlocker _inputBlocker;
        [SerializeField] private SwapHandler _swapHandler;
        [SerializeField] private MatchFinder _matchFinder;
        [SerializeField] private DestroyHandler _destroyHandler;
        [SerializeField] private FallHandler _fallHandler;
        [SerializeField] private RefillHandler _refillHandler;
        [SerializeField] private BoardShuffler _boardShuffler;

        private GameState _currentState = GameState.Idle;
        private int _cascadeDestroyedCount;
        private int _cascadeLevel;

        public GameState CurrentState => _currentState;

        private void OnEnable()
        {
            _swapHandler.OnSwapStarted += OnSwapStarted;
            _swapHandler.OnSwapCompleted += OnSwapCompleted;
            _swapHandler.OnSwapReverted += OnSwapReverted;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
            _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted += OnShuffleCompleted;
        }

        private void OnDisable()
        {
            _swapHandler.OnSwapStarted -= OnSwapStarted;
            _swapHandler.OnSwapCompleted -= OnSwapCompleted;
            _swapHandler.OnSwapReverted -= OnSwapReverted;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
            _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted -= OnShuffleCompleted;
        }

        private void SetState(GameState state)
        {
            if (_currentState == state) return;
            _currentState = state;
            OnStateChanged?.Invoke(state);
        }

        private void OnSwapStarted(Vector2Int a, Vector2Int b)
        {
            _inputBlocker.Block();
            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;
            SetState(GameState.Swapping);
        }

        private void OnSwapReverted(Vector2Int a, Vector2Int b)
        {
            FinishTurn();
        }

        private void OnSwapCompleted(Vector2Int a, Vector2Int b)
        {
            SetState(GameState.Matching);
            ProcessMatches();
        }

        private void ProcessMatches()
        {
            var matches = _matchFinder.FindAllMatches();

            if (matches.Count > 0)
            {
                if (_cascadeLevel == 0)
                    OnCascadeStarted?.Invoke();

                SetState(GameState.Destroying);
                _destroyHandler.DestroyMatches(matches);
            }
            else
            {
                CheckDeadlock();
            }
        }

        private void OnDestroyCompleted(int count)
        {
            _cascadeDestroyedCount += count;
            _cascadeLevel++;

            SetState(GameState.Falling);
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            SetState(GameState.Refilling);
            _refillHandler.ExecuteRefills();
        }

        private void OnRefillsCompleted()
        {
            SetState(GameState.CheckingCascade);
            ProcessMatches(); // Cascade loop
        }

        private void CheckDeadlock()
        {
            if (DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                FinishTurn();
                return;
            }

            Debug.Log("[GameLoop] Deadlock detected! Shuffling...");
            SetState(GameState.Shuffling);
            _boardShuffler.Shuffle();
        }

        private void OnShuffleCompleted()
        {
            // Check for auto-matches after shuffle
            var matches = _matchFinder.FindAllMatches();
            if (matches.Count > 0)
            {
                SetState(GameState.Destroying);
                _destroyHandler.DestroyMatches(matches);
                return;
            }

            // Still deadlocked? Shuffle again
            if (!DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                Debug.LogWarning("[GameLoop] Still deadlocked! Shuffling again...");
                _boardShuffler.Shuffle();
                return;
            }

            FinishTurn();
        }

        private void FinishTurn()
        {
            if (_cascadeLevel > 0)
                OnCascadeCompleted?.Invoke(_cascadeDestroyedCount, _cascadeLevel);

            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;

            SetState(GameState.Idle);
            _inputBlocker.Unblock();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug State")]
        private void DebugState()
        {
            Debug.Log($"[GameLoop] State: {_currentState}, Cascade: {_cascadeLevel}, Destroyed: {_cascadeDestroyedCount}");
        }

        [ContextMenu("Debug Check Deadlock")]
        private void DebugCheckDeadlock()
        {
            bool hasMoves = DeadlockChecker.HasPossibleMoves(_board, _matchFinder);
            int count = DeadlockChecker.CountPossibleMoves(_board, _matchFinder);
            Debug.Log($"[GameLoop] HasMoves: {hasMoves}, PossibleMoves: {count}");
        }
#endif
    }
}
