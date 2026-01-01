# Step 8: Game Loop (StateMachine)

## Goal

Manage game flow via StateMachine: coordinate all systems, handle state transitions, orchestrate the match-destroy-fall cycle.

---

## Dependencies

| Step | Component | Usage |
|------|-----------|-------|
| 1 | `GridData` | Coordinate conversion, grid validation |
| 1 | `GridView` | Access to GridData |
| 2 | `GemView` | Visual representation for animations |
| 2 | `GemConfig` | Gem type configuration |
| 3 | `BoardData` | Core game data, gem CRUD |
| 3 | `BoardView` | View management, gem creation/destruction |
| 3 | `SpawnSystem` | Generate gem types with anti-match |
| 4 | `FallSystem` | Calculate and apply falls |
| 4 | `FallAnimator` | Animate gem falling |
| 5 | `SwipeDetector` | User input, swipe events |
| 5 | `SwapSystem` | Validate and perform swaps |
| 5 | `SwapAnimator` | Animate swap/swap-back |
| 6 | `MatchSystem` | Find matches on board |
| 7 | `DestroySystem` | Remove gems from data |
| 7 | `DestroyAnimator` | Animate gem destruction |

---

## Architecture

```
                    ┌─────────────────────────────────────────────┐
                    │              GameController                  │
                    │         (MonoBehaviour, entry point)         │
                    │                                              │
                    │  Coordinates systems, subscribes to events   │
                    └─────────────────────────────────────────────┘
                                        │
                                        │ uses
                                        ▼
                    ┌─────────────────────────────────────────────┐
                    │            GameStateMachine                  │
                    │              (C# class)                      │
                    │                                              │
                    │  CurrentState, SetState(), OnStateChanged    │
                    └─────────────────────────────────────────────┘
                                        │
                            controls state transitions
                                        │
        ┌───────────────┬───────────────┼───────────────┬───────────────┐
        ▼               ▼               ▼               ▼               ▼
   ┌─────────┐    ┌──────────┐    ┌──────────┐    ┌───────────┐   ┌──────────┐
   │  Idle   │    │ Swapping │    │ Matching │    │ Destroying│   │ Falling  │
   │         │    │          │    │          │    │           │   │          │
   │ Input   │───►│ Animate  │───►│  Find    │───►│  Animate  │──►│ Fall +   │
   │ enabled │    │  swap    │    │ matches  │    │  destroy  │   │  Spawn   │
   └─────────┘    └──────────┘    └──────────┘    └───────────┘   └──────────┘
        ▲                               │                               │
        │                               │ no matches                    │
        │                               ▼                               │
        │                         ┌──────────┐                          │
        │                         │ Checking │                          │
        │                         │          │                          │
        │◄────── no matches ──────│  Check   │◄─────────────────────────┘
        │                         │  new     │
        │                         └──────────┘
        │                               │ has matches
        │                               ▼
        └───────────────────────── Matching ◄──┘
```

---

## State Transitions

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STATE FLOW DIAGRAM                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────┐                                                                    │
│  │ IDLE │◄─────────────────────────────────────────────────┐                │
│  └──┬───┘                                                   │                │
│     │ User swipes                                           │                │
│     │ OnSwipeDetected(from, to)                            │                │
│     ▼                                                       │                │
│  ┌──────────┐                                               │                │
│  │ SWAPPING │                                               │                │
│  └────┬─────┘                                               │                │
│       │ SwapAnimator.OnSwapComplete                         │                │
│       ▼                                                     │                │
│  ┌──────────┐                                               │                │
│  │ MATCHING │◄──────────────────────────────┐               │                │
│  └────┬─────┘                               │               │                │
│       │                                     │               │                │
│       ├─► matches.Count == 0 ──────────────►│ SwapBack ────►│                │
│       │   (swap back + return to Idle)      │               │                │
│       │                                     │               │                │
│       ▼ matches.Count > 0                   │               │                │
│  ┌────────────┐                             │               │                │
│  │ DESTROYING │                             │               │                │
│  └─────┬──────┘                             │               │                │
│        │ DestroyAnimator.OnDestroyComplete  │               │                │
│        ▼                                    │               │                │
│  ┌──────────┐                               │               │                │
│  │ FALLING  │                               │               │                │
│  └────┬─────┘                               │               │                │
│       │ FallAnimator.OnAllFallsComplete     │               │                │
│       ▼                                     │               │                │
│  ┌──────────┐                               │               │                │
│  │ CHECKING │                               │               │                │
│  └────┬─────┘                               │               │                │
│       │                                     │               │                │
│       ├─► HasAnyMatch() == true ───────────►│               │                │
│       │   (cascade: more matches!)          ▲               │                │
│       │                                     │               │                │
│       └─► HasAnyMatch() == false ───────────────────────────┘                │
│           (board stable, return to Idle)                                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### State Responsibilities

| State | Entry Action | Exit Condition | Next State |
|-------|--------------|----------------|------------|
| Idle | Enable input | OnSwipeDetected | Swapping |
| Swapping | Animate swap | OnSwapComplete | Matching |
| Matching | Find matches | Immediate | Destroying (if matches) / Idle (swap back) |
| Destroying | Animate destruction | OnDestroyComplete | Falling |
| Falling | Fall + spawn gems | OnAllFallsComplete | Checking |
| Checking | Check for cascades | Immediate | Matching (if matches) / Idle (stable) |

---

## Files

```
Assets/Scripts/Game/
    GameState.cs          # enum (6 states)
    GameStateMachine.cs   # C# class, FSM logic
    GameController.cs     # MonoBehaviour, orchestration
```

---

## Component 1: GameState

**File:** `Assets/Scripts/Game/GameState.cs`

**Type:** enum

**Responsibility:** Define all possible game states.

### Code

```csharp
namespace Match3.Game
{
    /// <summary>
    /// All possible states of the game loop.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Waiting for player input. Input enabled.
        /// </summary>
        Idle,

        /// <summary>
        /// Swap animation in progress. Input disabled.
        /// </summary>
        Swapping,

        /// <summary>
        /// Finding matches on board. Immediate transition.
        /// </summary>
        Matching,

        /// <summary>
        /// Destroy animation in progress.
        /// </summary>
        Destroying,

        /// <summary>
        /// Gems falling + new gems spawning.
        /// </summary>
        Falling,

        /// <summary>
        /// Checking for cascade matches after fall.
        /// Immediate transition to Matching or Idle.
        /// </summary>
        Checking
    }
}
```

---

## Component 2: GameStateMachine

**File:** `Assets/Scripts/Game/GameStateMachine.cs`

**Type:** Plain C# class (not MonoBehaviour)

**Responsibility:** Manage current state, fire state change events, validate transitions.

### Code

```csharp
using System;

namespace Match3.Game
{
    /// <summary>
    /// Simple finite state machine for game loop.
    /// Does NOT contain game logic — only state management.
    /// </summary>
    public class GameStateMachine
    {
        private GameState _currentState;

        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Fires when state changes.
        /// Parameters: (previousState, newState)
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>
        /// Creates FSM starting in Idle state.
        /// </summary>
        public GameStateMachine()
        {
            _currentState = GameState.Idle;
        }

        /// <summary>
        /// Creates FSM with specified initial state.
        /// </summary>
        public GameStateMachine(GameState initialState)
        {
            _currentState = initialState;
        }

        /// <summary>
        /// Transitions to new state. Fires OnStateChanged.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState)
                return;

            var previousState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Checks if current state matches expected.
        /// </summary>
        public bool IsInState(GameState state)
        {
            return _currentState == state;
        }

        /// <summary>
        /// Resets FSM to Idle state.
        /// </summary>
        public void Reset()
        {
            SetState(GameState.Idle);
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| CurrentState | `GameState` | Get current state |
| OnStateChanged | `event Action<GameState, GameState>` | Fires on transition (prev, new) |
| SetState | `void SetState(GameState newState)` | Transition to new state |
| IsInState | `bool IsInState(GameState state)` | Check current state |
| Reset | `void Reset()` | Return to Idle |

### Notes

- Pure data class, no game logic
- Fires event AFTER state changes (synchronous)
- Ignores duplicate state sets (no event if same state)
- Event provides both previous and new state for debugging

---

## Component 3: GameController

**File:** `Assets/Scripts/Game/GameController.cs`

**Type:** MonoBehaviour

**Responsibility:** Orchestrate all systems, handle state transitions, manage game flow.

### Code

```csharp
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

            // Get data references
            _gridData = _gridView.Data;

            // Initialize animators with GridData
            _fallAnimator.Initialize(_gridData);
        }

        private void InitializeBoard()
        {
            // BoardView initializes BoardData and fills grid
            _boardData = _boardView.Data;

            // Initialize swipe detector
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
                    var spawnPos = GetSpawnPosition(col, rowsAbove);

                    // Create view at spawn position
                    var view = _boardView.CreateGemAt(spawnPos, gemData);

                    // Register view at target position
                    _boardView.RegisterView(targetPos, view);

                    newGems.Add((view, spawnPos, targetPos));
                }
            }

            return newGems;
        }

        private Vector3 GetSpawnPosition(int column, int rowsAbove)
        {
            // Spawn above the grid
            var topRow = _boardData.Height - 1 + rowsAbove;
            var gridPos = new Vector2Int(column, topRow);
            return _gridData.GridToWorld(gridPos);
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
```

### Dependencies (SerializeField)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| _gridView | GridView | Yes | Grid coordinate system |
| _boardView | BoardView | Yes | Gem view management |
| _gemConfig | GemConfig | Yes | Gem types and sprites |
| _swipeDetector | SwipeDetector | Yes | User input |
| _swapAnimator | SwapAnimator | Yes | Swap/swap-back animation |
| _destroyAnimator | DestroyAnimator | Yes | Destruction animation |
| _fallAnimator | FallAnimator | Yes | Fall animation |

### Public API

GameController is an entry point and orchestrator. It has no public API — all coordination happens through events and state machine.

### Notes

- All systems created in Awake, subscriptions in OnEnable
- Swipe handling validates state before processing
- Swap tracking (_swapFrom, _swapTo) for swap-back
- _isSwapBack prevents infinite loop on no-match
- _pendingDestroyPositions passed between states
- Debug.Log for state transitions (remove in production)

---

## Required BoardView Additions

GameController requires these methods on BoardView (from Step 3). Add if not present:

```csharp
// Add to Assets/Scripts/Board/BoardView.cs

/// <summary>
/// Swaps view references at two positions.
/// </summary>
public void SwapViews(Vector2Int a, Vector2Int b)
{
    var viewA = _views[a.x, a.y];
    var viewB = _views[b.x, b.y];

    _views[a.x, a.y] = viewB;
    _views[b.x, b.y] = viewA;

    if (viewA != null) viewA.SetGridPosition(b);
    if (viewB != null) viewB.SetGridPosition(a);
}

/// <summary>
/// Updates view position in tracking array.
/// </summary>
public void UpdateViewPosition(Vector2Int from, Vector2Int to)
{
    var view = _views[from.x, from.y];
    _views[from.x, from.y] = null;
    _views[to.x, to.y] = view;

    if (view != null) view.SetGridPosition(to);
}

/// <summary>
/// Registers existing view at position.
/// </summary>
public void RegisterView(Vector2Int pos, GemView view)
{
    _views[pos.x, pos.y] = view;
    if (view != null) view.SetGridPosition(pos);
}

/// <summary>
/// Creates gem view at world position (for spawning above grid).
/// </summary>
public GemView CreateGemAt(Vector3 worldPos, GemData gem)
{
    var view = Instantiate(_gemPrefab, _gemsContainer);
    view.Setup(gem.Type, _gemConfig);
    view.transform.position = worldPos;
    return view;
}
```

---

## Required BoardData Additions

Add silent setter for spawning (from Step 4 spec):

```csharp
// Add to Assets/Scripts/Board/BoardData.cs

/// <summary>
/// Sets gem at position WITHOUT firing OnGemAdded event.
/// Used when view is created manually (fall spawn).
/// </summary>
public void SetGemSilent(Vector2Int pos, GemData gem)
{
    if (!IsValidPosition(pos))
        return;
    _gems[pos.x, pos.y] = gem;
    // No event fired
}
```

---

## Scene Setup

### Hierarchy

```
Scene
├── Grid
│   └── GridView (component)
├── Board
│   ├── BoardView (component)
│   └── Gems (container)
├── Systems
│   ├── SwipeDetector (component)
│   ├── SwapAnimator (component)
│   ├── DestroyAnimator (component)
│   └── FallAnimator (component)
└── GameController (component)
```

### Inspector Setup

#### GameController
1. Create empty GameObject "GameController" in scene
2. Add `GameController` component
3. Assign references:
   - _gridView: drag GridView
   - _boardView: drag BoardView
   - _gemConfig: drag GemConfig ScriptableObject
   - _swipeDetector: drag SwipeDetector
   - _swapAnimator: drag SwapAnimator
   - _destroyAnimator: drag DestroyAnimator
   - _fallAnimator: drag FallAnimator

---

## Complete Game Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         COMPLETE GAME FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [GAME START]                                                                │
│       │                                                                      │
│       ├──► GameController.Awake()                                            │
│       │    └── InitializeSystems() — create FSM, systems                     │
│       │                                                                      │
│       ├──► GameController.OnEnable()                                         │
│       │    └── SubscribeToEvents() — wire up all events                      │
│       │                                                                      │
│       └──► GameController.Start()                                            │
│            ├── InitializeBoard() — get BoardData reference                   │
│            └── SetState(Idle) — begin game loop                              │
│                                                                              │
│  ═══════════════════════════════════════════════════════════════════════     │
│                                                                              │
│  [IDLE STATE]                                                                │
│       │                                                                      │
│       └──► EnterIdleState()                                                  │
│            └── _swipeDetector.IsEnabled = true                               │
│                     │                                                        │
│                     │ User swipes on board                                   │
│                     ▼                                                        │
│            OnSwipeDetected(from, to)                                         │
│                     │                                                        │
│                     ▼                                                        │
│  [SWAPPING STATE]                                                            │
│       │                                                                      │
│       ├──► HandleSwipeDetected()                                             │
│       │    ├── Validate: IsInState(Idle), IsValidSwap                        │
│       │    ├── _swipeDetector.IsEnabled = false                              │
│       │    ├── Store _swapFrom, _swapTo                                      │
│       │    ├── _swapSystem.PerformSwap() — update data                       │
│       │    ├── _boardView.SwapViews() — update view tracking                 │
│       │    ├── _swapAnimator.AnimateSwap() — start animation                 │
│       │    └── SetState(Swapping)                                            │
│       │                                                                      │
│       └──► [Wait for animation...]                                           │
│            └── OnSwapComplete                                                │
│                     │                                                        │
│                     ▼                                                        │
│  [MATCHING STATE]                                                            │
│       │                                                                      │
│       └──► EnterMatchingState()                                              │
│            ├── _matchSystem.FindAllMatches()                                 │
│            │                                                                 │
│            ├─► matches.Count == 0                                            │
│            │   ├── First time? → PerformSwapBack()                           │
│            │   │   ├── Revert data: _swapSystem.PerformSwap()                │
│            │   │   ├── Revert views: _boardView.SwapViews()                  │
│            │   │   ├── _swapAnimator.AnimateSwapBack()                       │
│            │   │   └── [Wait...] → OnSwapBackComplete → SetState(Idle)       │
│            │   │                                                             │
│            │   └── Already swapped back? → SetState(Idle)                    │
│            │                                                                 │
│            └─► matches.Count > 0                                             │
│                ├── _pendingDestroyPositions = GetUniquePositions()           │
│                └── SetState(Destroying)                                      │
│                         │                                                    │
│                         ▼                                                    │
│  [DESTROYING STATE]                                                          │
│       │                                                                      │
│       └──► EnterDestroyingState()                                            │
│            ├── GetViews(_pendingDestroyPositions)                            │
│            └── _destroyAnimator.AnimateDestroy(views)                        │
│                     │                                                        │
│                     │ [Wait for animation...]                                │
│                     ▼                                                        │
│            HandleDestroyComplete()                                           │
│            ├── _destroySystem.DestroyGems() — remove from data               │
│            │   └── BoardData.RemoveGem() → OnGemRemoved                      │
│            │       └── BoardView cleans up GameObjects                       │
│            └── SetState(Falling)                                             │
│                     │                                                        │
│                     ▼                                                        │
│  [FALLING STATE]                                                             │
│       │                                                                      │
│       └──► EnterFallingState()                                               │
│            ├── _fallSystem.CalculateFalls() — compute moves                  │
│            ├── Collect existingFalls (view, targetPos)                       │
│            ├── _fallSystem.ApplyFalls() — update data                        │
│            ├── _boardView.UpdateViewPosition() for each                      │
│            │                                                                 │
│            ├── SpawnNewGems() for each column:                               │
│            │   ├── Get empty positions                                       │
│            │   ├── For each empty:                                           │
│            │   │   ├── _spawnSystem.GenerateType()                           │
│            │   │   ├── _boardData.SetGemSilent()                             │
│            │   │   ├── Calculate spawn position (above grid)                 │
│            │   │   ├── _boardView.CreateGemAt()                              │
│            │   │   └── _boardView.RegisterView()                             │
│            │   └── Collect (view, startPos, targetPos)                       │
│            │                                                                 │
│            └── _fallAnimator.AnimateAllFalls(existing, new)                  │
│                     │                                                        │
│                     │ [Wait for animation...]                                │
│                     ▼                                                        │
│            HandleFallsComplete()                                             │
│            └── SetState(Checking)                                            │
│                     │                                                        │
│                     ▼                                                        │
│  [CHECKING STATE]                                                            │
│       │                                                                      │
│       └──► EnterCheckingState()                                              │
│            └── _matchSystem.HasAnyMatch()                                    │
│                     │                                                        │
│                     ├─► true → SetState(Matching)                            │
│                     │         └── [CASCADE: repeat Destroy → Fall → Check]   │
│                     │                                                        │
│                     └─► false → SetState(Idle)                               │
│                                 └── [Board stable, wait for input]           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Edge Cases

| Situation | Behavior |
|-----------|----------|
| Swipe while animating | Ignored (state check in HandleSwipeDetected) |
| Swipe outside grid | Ignored (SwipeDetector validates) |
| Swipe to empty cell | Ignored (SwapSystem.IsValidSwap returns false) |
| Swap creates no match | Swap back animation, return to Idle |
| Cascade matches (chain) | Repeat Matching → Destroying → Falling → Checking |
| All gems destroyed | Full respawn via Falling state |
| Empty destroy list | Skip to Falling state |
| No falls needed | FallAnimator fires OnAllFallsComplete immediately |

---

## Testing Checklist

### GameState Tests
- [ ] All 6 states defined
- [ ] Values distinct

### GameStateMachine Tests
- [ ] Starts in Idle (default constructor)
- [ ] Starts in specified state (parameterized constructor)
- [ ] SetState changes CurrentState
- [ ] SetState fires OnStateChanged with correct params
- [ ] SetState ignores same state (no event)
- [ ] IsInState returns correct value
- [ ] Reset returns to Idle

### GameController Integration Tests
- [ ] Initialization creates all systems
- [ ] Events subscribed on enable
- [ ] Events unsubscribed on disable
- [ ] Swipe in Idle state triggers Swapping
- [ ] Swipe in non-Idle state ignored
- [ ] Valid swap animates correctly
- [ ] Invalid swap does nothing
- [ ] Match found → Destroying state
- [ ] No match → swap back animation → Idle
- [ ] Destroy animation → gems removed from data
- [ ] Fall animation → gems land correctly
- [ ] New gems spawn above grid
- [ ] Cascade detection works (Checking → Matching)
- [ ] Board stabilizes (Checking → Idle)
- [ ] Input disabled during animations
- [ ] Input enabled in Idle state

### Full Flow Tests
- [ ] Simple match: swap → match → destroy → fall → idle
- [ ] No match: swap → swap back → idle
- [ ] Cascade: swap → match → destroy → fall → match → destroy → fall → idle
- [ ] Multiple matches: all unique positions destroyed
- [ ] L-shape match: no duplicate destroys
- [ ] Full column cleared: all new gems spawn correctly

---

## File Checklist

- [ ] Create `Assets/Scripts/Game/` folder
- [ ] Implement `GameState.cs`
- [ ] Implement `GameStateMachine.cs`
- [ ] Implement `GameController.cs`
- [ ] Add BoardView methods (SwapViews, UpdateViewPosition, RegisterView, CreateGemAt)
- [ ] Add BoardData.SetGemSilent method
- [ ] Create GameController GameObject in scene
- [ ] Assign all references in Inspector
- [ ] Test complete game flow

---

## Notes

- GameStateMachine is pure C# for testability
- GameController is MonoBehaviour for scene references and lifecycle
- All state transitions logged (remove in production)
- Cascade support via Checking → Matching loop
- Anti-match spawn prevents immediate re-matches after fall
- Input management automatic via state transitions
- Event-driven architecture keeps components decoupled
- Debug.Log statements help with development, remove for release
