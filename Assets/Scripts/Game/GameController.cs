using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Destroy;
using Match3.Fall;
using Match3.Gem;
using Match3.Grid;
using Match3.Input;
using Match3.Match;
using Match3.Spawn;
using Match3.Swap;

namespace Match3.Game
{
    /// <summary>
    /// Main game controller. Coordinates all systems via state machine.
    /// Entry point for Match3 game logic.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private GemConfig _gemConfig;

        [Header("Input")]
        [SerializeField] private SwipeDetector _swipeDetector;

        [Header("Animators")]
        [SerializeField] private SwapAnimator _swapAnimator;
        [SerializeField] private DestroyAnimator _destroyAnimator;
        [SerializeField] private FallAnimator _fallAnimator;

        // Systems (created at runtime)
        private GameStateMachine _stateMachine;
        private SwapSystem _swapSystem;
        private MatchSystem _matchSystem;
        private DestroySystem _destroySystem;
        private FallSystem _fallSystem;
        private SpawnSystem _spawnSystem;

        // Runtime data
        private GridData _gridData;
        private BoardData _boardData;

        // Swap tracking
        private Vector2Int _swapFrom;
        private Vector2Int _swapTo;
        private bool _isSwapBack;

        // Destroy tracking
        private List<Vector2Int> _pendingDestroyPositions;

        // --- Lifecycle ---

        private void Awake()
        {
            InitializeSystems();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // BoardView initializes in its Start()
            // Check if already ready, otherwise wait for event
            if (_boardView.Data != null)
            {
                InitializeBoard();
                _stateMachine.SetState(GameState.Idle);
            }
            else
            {
                _boardView.OnBoardReady += OnBoardReady;
            }
        }

        private void OnBoardReady()
        {
            _boardView.OnBoardReady -= OnBoardReady;
            InitializeBoard();
            _stateMachine.SetState(GameState.Idle);
        }

        // --- Initialization ---

        private void InitializeSystems()
        {
            // Create FSM
            _stateMachine = new GameStateMachine();

            // Create logic systems
            _swapSystem = new SwapSystem();
            _matchSystem = new MatchSystem();
            _destroySystem = new DestroySystem();
            _fallSystem = new FallSystem();
            _spawnSystem = new SpawnSystem(_gemConfig);
        }

        private void InitializeBoard()
        {
            // Get data references (GridView.Awake already called by now)
            _gridData = _gridView.Data;
            _boardData = _boardView.Data;

            // Initialize components that need GridData
            _fallAnimator.Initialize(_gridData);
            _swipeDetector.Initialize(_gridData);
        }

        // --- Event Subscriptions ---

        private void SubscribeToEvents()
        {
            // State machine
            _stateMachine.OnStateChanged += HandleStateChanged;

            // Input
            _swipeDetector.OnSwipeDetected += HandleSwipeDetected;

            // Animators
            _swapAnimator.OnSwapComplete += HandleSwapComplete;
            _swapAnimator.OnSwapBackComplete += HandleSwapBackComplete;
            _destroyAnimator.OnDestroyComplete += HandleDestroyComplete;
            _fallAnimator.OnAllFallsComplete += HandleFallsComplete;
        }

        private void UnsubscribeFromEvents()
        {
            // State machine
            _stateMachine.OnStateChanged -= HandleStateChanged;

            // Input
            _swipeDetector.OnSwipeDetected -= HandleSwipeDetected;

            // Animators
            _swapAnimator.OnSwapComplete -= HandleSwapComplete;
            _swapAnimator.OnSwapBackComplete -= HandleSwapBackComplete;
            _destroyAnimator.OnDestroyComplete -= HandleDestroyComplete;
            _fallAnimator.OnAllFallsComplete -= HandleFallsComplete;
        }

        // --- State Handling ---

        private void HandleStateChanged(GameState previousState, GameState newState)
        {
            // Debug log (remove in production)
            Debug.Log($"[GameController] State: {previousState} -> {newState}");

            // Handle entry actions
            switch (newState)
            {
                case GameState.Idle:
                    EnterIdleState();
                    break;
                case GameState.Matching:
                    EnterMatchingState();
                    break;
                case GameState.Destroying:
                    EnterDestroyingState();
                    break;
                case GameState.Falling:
                    EnterFallingState();
                    break;
                case GameState.Checking:
                    EnterCheckingState();
                    break;
                // Swapping is entered via HandleSwipeDetected
            }
        }

        // --- State: Idle ---

        private void EnterIdleState()
        {
            _swipeDetector.IsEnabled = true;
            _isSwapBack = false;
        }

        // --- State: Swapping (entered via input) ---

        private void HandleSwipeDetected(Vector2Int from, Vector2Int to)
        {
            // Only process in Idle state
            if (!_stateMachine.IsInState(GameState.Idle))
                return;

            // Validate swap
            if (!_swapSystem.IsValidSwap(from, to, _boardData))
                return;

            // Disable input
            _swipeDetector.IsEnabled = false;

            // Store swap positions
            _swapFrom = from;
            _swapTo = to;
            _isSwapBack = false;

            // Perform swap in data
            _swapSystem.PerformSwap(_boardData, from, to);

            // Update view tracking
            _boardView.SwapViews(from, to);

            // Get views (after swap, positions are swapped)
            var gemA = _boardView.GetView(to);
            var gemB = _boardView.GetView(from);

            // Start animation
            _swapAnimator.AnimateSwap(gemA, gemB);

            // Transition to Swapping state
            _stateMachine.SetState(GameState.Swapping);
        }

        private void HandleSwapComplete()
        {
            if (!_stateMachine.IsInState(GameState.Swapping))
                return;

            // Transition to Matching
            _stateMachine.SetState(GameState.Matching);
        }

        // --- State: Matching ---

        private void EnterMatchingState()
        {
            // Find matches at swap positions (or full board for cascade)
            var matches = _matchSystem.FindAllMatches(_boardData);

            if (matches.Count == 0)
            {
                // No matches
                if (_isSwapBack)
                {
                    // Already swapped back, go to Idle
                    _stateMachine.SetState(GameState.Idle);
                }
                else
                {
                    // Need to swap back
                    PerformSwapBack();
                }
                return;
            }

            // Has matches — collect unique positions
            _pendingDestroyPositions = _destroySystem.GetUniquePositions(matches);

            // Transition to Destroying
            _stateMachine.SetState(GameState.Destroying);
        }

        private void PerformSwapBack()
        {
            _isSwapBack = true;

            // Revert swap in data
            _swapSystem.PerformSwap(_boardData, _swapFrom, _swapTo);

            // Update view tracking
            _boardView.SwapViews(_swapFrom, _swapTo);

            // Get views
            var gemA = _boardView.GetView(_swapFrom);
            var gemB = _boardView.GetView(_swapTo);

            // Animate swap back
            _swapAnimator.AnimateSwapBack(gemA, gemB);
        }

        private void HandleSwapBackComplete()
        {
            // Return to Idle after swap back
            _stateMachine.SetState(GameState.Idle);
        }

        // --- State: Destroying ---

        private void EnterDestroyingState()
        {
            if (_pendingDestroyPositions == null || _pendingDestroyPositions.Count == 0)
            {
                // Nothing to destroy, skip to Falling
                _stateMachine.SetState(GameState.Falling);
                return;
            }

            // Get views for animation
            var views = GetViews(_pendingDestroyPositions);

            // Start destroy animation
            _destroyAnimator.AnimateDestroy(views);
        }

        private void HandleDestroyComplete()
        {
            if (!_stateMachine.IsInState(GameState.Destroying))
                return;

            // Remove gems from data (triggers BoardView cleanup)
            _destroySystem.DestroyGems(_boardData, _pendingDestroyPositions);

            // Clear pending list
            _pendingDestroyPositions = null;

            // Transition to Falling
            _stateMachine.SetState(GameState.Falling);
        }

        // --- State: Falling ---

        private void EnterFallingState()
        {
            // Calculate falls for existing gems
            var falls = _fallSystem.CalculateFalls(_boardData);

            // Collect animation data BEFORE applying to data
            var existingFalls = new List<(GemView gem, Vector2Int targetPos)>();
            foreach (var fall in falls)
            {
                var view = _boardView.GetView(fall.From);
                if (view != null)
                {
                    existingFalls.Add((view, fall.To));
                }
            }

            // Apply falls to data + update view tracking
            _fallSystem.ApplyFalls(_boardData, falls);
            foreach (var fall in falls)
            {
                _boardView.UpdateViewPosition(fall.From, fall.To);
            }

            // Spawn new gems for empty cells
            var newGems = SpawnNewGems();

            // Animate all falls
            _fallAnimator.AnimateAllFalls(existingFalls, newGems);
        }

        private List<(GemView gem, Vector3 startPos, Vector2Int targetPos)> SpawnNewGems()
        {
            var newGems = new List<(GemView gem, Vector3 startPos, Vector2Int targetPos)>();

            for (int col = 0; col < _boardData.Width; col++)
            {
                var emptyPositions = _fallSystem.GetEmptyPositionsInColumn(_boardData, col);

                for (int i = 0; i < emptyPositions.Count; i++)
                {
                    var targetPos = emptyPositions[i];

                    // Generate type with anti-match
                    var type = _spawnSystem.GenerateType(targetPos, _boardData);
                    var gemData = new GemData(type, targetPos);

                    // Set in BoardData (silent - no event)
                    _boardData.SetGemSilent(targetPos, gemData);

                    // Calculate spawn position above grid
                    int rowsAbove = emptyPositions.Count - i;

                    // Create view at spawn position
                    var view = _boardView.CreateGemAbove(col, rowsAbove, gemData);

                    // Register view at target position
                    _boardView.RegisterView(targetPos, view);

                    // Get spawn position for animation
                    var spawnPos = view.transform.position;

                    newGems.Add((view, spawnPos, targetPos));
                }
            }

            return newGems;
        }

        private void HandleFallsComplete()
        {
            if (!_stateMachine.IsInState(GameState.Falling))
                return;

            // Transition to Checking
            _stateMachine.SetState(GameState.Checking);
        }

        // --- State: Checking ---

        private void EnterCheckingState()
        {
            // Check for cascade matches
            if (_matchSystem.HasAnyMatch(_boardData))
            {
                // More matches! Continue cascade
                _stateMachine.SetState(GameState.Matching);
            }
            else
            {
                // Board is stable
                _stateMachine.SetState(GameState.Idle);
            }
        }

        // --- Helpers ---

        private List<GemView> GetViews(List<Vector2Int> positions)
        {
            var views = new List<GemView>(positions.Count);
            foreach (var pos in positions)
            {
                var view = _boardView.GetView(pos);
                if (view != null)
                {
                    views.Add(view);
                }
            }
            return views;
        }
    }
}
