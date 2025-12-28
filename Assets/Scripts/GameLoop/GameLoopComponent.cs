using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Match3.Core;
using Match3.Grid;
using Match3.Spawn;
using Match3.Swap;
using Match3.Match;
using Match3.Destruction;
using Match3.Gravity;

namespace Match3.GameLoop
{
    /// <summary>
    /// Центральный координатор игры. Управляет State Machine и
    /// последовательностью: Input → Swap → Match → Destroy → Gravity → Repeat
    /// </summary>
    public class GameLoopComponent : MonoBehaviour
    {
        // === СОБЫТИЯ ===
        public event Action<GameState> OnStateChanged;
        public event Action OnGameReady;
        public event Action<int> OnMatchesDestroyed;

        // === ЗАВИСИМОСТИ ===
        [Header("Systems")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SpawnComponent _spawn;
        [SerializeField] private SwapComponent _swap;
        [SerializeField] private MatchDetectionComponent _matchDetection;
        [SerializeField] private DestructionComponent _destruction;
        [SerializeField] private GravityComponent _gravity;
        [SerializeField] private InputComponent _input;

        // === СОСТОЯНИЕ ===
        private GameState _currentState = GameState.Initializing;

        public GameState CurrentState => _currentState;
        public bool IsInputAllowed => _currentState == GameState.WaitingForInput;

        // === UNITY CALLBACKS ===

        private void OnEnable()
        {
            _input.OnSwapRequested += HandleSwapRequested;
            _spawn.OnGridFilled += HandleGridFilled;
        }

        private void OnDisable()
        {
            _input.OnSwapRequested -= HandleSwapRequested;
            _spawn.OnGridFilled -= HandleGridFilled;
        }

        private void Start()
        {
            Initialize();
        }

        // === ИНИЦИАЛИЗАЦИЯ ===

        private void Initialize()
        {
            SetState(GameState.Initializing);
            _spawn.FillGrid();
        }

        private void HandleGridFilled()
        {
            SetState(GameState.WaitingForInput);
            OnGameReady?.Invoke();
        }

        // === STATE MACHINE ===

        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;

            // Блокируем/разблокируем input
            _input.SetInputEnabled(newState == GameState.WaitingForInput);

            OnStateChanged?.Invoke(_currentState);
            Debug.Log($"[GameLoop] State: {_currentState}");
        }

        // === ОБРАБОТКА INPUT ===

        private void HandleSwapRequested(Vector2Int pos1, Vector2Int pos2)
        {
            if (!IsInputAllowed)
            {
                Debug.Log("[GameLoop] Input blocked - not in WaitingForInput state");
                return;
            }

            _ = ProcessSwapAsync(pos1, pos2);
        }

        // === ОСНОВНОЙ ИГРОВОЙ ЦИКЛ ===

        private async Task ProcessSwapAsync(Vector2Int pos1, Vector2Int pos2)
        {
            // 1. SWAP
            SetState(GameState.Swapping);
            bool swapped = await _swap.TrySwap(pos1, pos2);

            if (!swapped)
            {
                SetState(GameState.WaitingForInput);
                return;
            }

            // 2. CHECK MATCHES
            SetState(GameState.CheckingMatches);
            var matches = _matchDetection.FindAllMatches();

            // 3. NO MATCHES → SWAP BACK
            if (matches.Count == 0)
            {
                SetState(GameState.Swapping);
                await _swap.SwapBack(pos1, pos2);
                SetState(GameState.WaitingForInput);
                return;
            }

            // 4. CASCADE LOOP
            await ProcessCascadeAsync(matches);

            // 5. DONE
            SetState(GameState.WaitingForInput);
        }

        private async Task ProcessCascadeAsync(List<Vector2Int> initialMatches)
        {
            var matches = initialMatches;
            int totalDestroyed = 0;

            while (matches.Count > 0)
            {
                // DESTROY
                SetState(GameState.Destroying);
                await _destruction.DestroyElements(matches);
                totalDestroyed += matches.Count;

                // GRAVITY
                SetState(GameState.Falling);
                await _gravity.ApplyGravity();

                // CHECK NEW MATCHES
                SetState(GameState.CheckingMatches);
                matches = _matchDetection.FindAllMatches();
            }

            if (totalDestroyed > 0)
                OnMatchesDestroyed?.Invoke(totalDestroyed);
        }

        // === PUBLIC API ===

        public void RestartGame()
        {
            // TODO: Очистка сетки перед рестартом
            Initialize();
        }
    }
}
