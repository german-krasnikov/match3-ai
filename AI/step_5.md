# Step 5: Input & Swap System

## Цель

Система обработки свайпов и обмена gem-ов: детект свайпа, валидация, анимация swap/swap-back.

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────────────┐
│                  SwapController (future Step 8)                      │
│       Координация: Detect → Validate → Animate → Match Check         │
└─────────────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│  SwipeDetector  │   │   SwapSystem    │   │  SwapAnimator   │
│ (MonoBehaviour) │   │   (C# class)    │   │ (MonoBehaviour) │
│                 │   │                 │   │                 │
│ OnSwipeDetected │   │ IsValidSwap     │   │ AnimateSwap     │
│ (from, to)      │   │ PerformSwap     │   │ AnimateSwapBack │
│                 │   │ WillMatch       │   │ OnSwapComplete  │
└─────────────────┘   └─────────────────┘   └─────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│    GridData     │   │   BoardData     │   │    GemView      │
│  (from Step 1)  │   │  (from Step 3)  │   │  (from Step 2)  │
│  WorldToGrid    │   │  SwapGems       │   │  transform      │
│  IsValidPosition│   │  GetGem         │   │                 │
└─────────────────┘   └─────────────────┘   └─────────────────┘
```

**Flow:**
1. User touches/clicks on board → SwipeDetector records start position
2. User drags → SwipeDetector calculates direction
3. User releases → SwipeDetector fires OnSwipeDetected(from, to)
4. GameController receives event → calls SwapSystem.IsValidSwap
5. If valid → SwapSystem.PerformSwap + SwapAnimator.AnimateSwap
6. After animation → MatchSystem checks for matches
7. If no match → SwapSystem.PerformSwap (reverse) + SwapAnimator.AnimateSwapBack

---

## Файловая структура

```
Assets/Scripts/
  Input/
    SwipeDetector.cs      # MonoBehaviour, touch/mouse input
  Swap/
    SwapSystem.cs         # C# class, логика валидации и обмена
    SwapAnimator.cs       # MonoBehaviour, DOTween анимации
```

---

## Component 1: SwipeDetector

**File:** `Assets/Scripts/Input/SwipeDetector.cs`

**Type:** MonoBehaviour

**Responsibility:** Детектирует свайп жест (touch или mouse), определяет начальную и конечную ячейки, стреляет событием.

### Code

```csharp
using System;
using UnityEngine;
using Match3.Grid;

namespace Match3.Input
{
    public class SwipeDetector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _minSwipeDistance = 0.3f;
        [SerializeField] private Camera _camera;

        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;

        private GridData _gridData;
        private bool _isDragging;
        private Vector3 _startWorldPos;
        private Vector2Int _startGridPos;

        /// <summary>
        /// Fires when valid swipe detected.
        /// from = starting cell, to = target cell (adjacent)
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwipeDetected;

        /// <summary>
        /// Enables/disables input processing.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Start()
        {
            if (_gridView != null)
                _gridData = _gridView.Data;
        }

        /// <summary>
        /// Sets GridData reference (for runtime initialization).
        /// </summary>
        public void Initialize(GridData gridData)
        {
            _gridData = gridData;
        }

        private void Update()
        {
            if (!IsEnabled || _gridData == null)
                return;

            HandleInput();
        }

        private void HandleInput()
        {
            // Support both mouse and touch
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandlePointerDown(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isDragging)
            {
                HandlePointerUp(UnityEngine.Input.mousePosition);
            }

            // Touch input (mobile)
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandlePointerDown(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_isDragging)
                            HandlePointerUp(touch.position);
                        break;
                }
            }
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector2Int gridPos = _gridData.WorldToGrid(worldPos);

            // Only start drag if on valid grid cell
            if (!_gridData.IsValidPosition(gridPos))
                return;

            _isDragging = true;
            _startWorldPos = worldPos;
            _startGridPos = gridPos;
        }

        private void HandlePointerUp(Vector2 screenPos)
        {
            _isDragging = false;

            Vector3 endWorldPos = ScreenToWorld(screenPos);
            Vector2 delta = endWorldPos - _startWorldPos;

            // Check minimum swipe distance
            if (delta.magnitude < _minSwipeDistance)
                return;

            // Determine direction (4-directional: up, down, left, right)
            Vector2Int direction = GetSwipeDirection(delta);
            if (direction == Vector2Int.zero)
                return;

            Vector2Int targetPos = _startGridPos + direction;

            // Validate target is on grid
            if (!_gridData.IsValidPosition(targetPos))
                return;

            // Fire event
            OnSwipeDetected?.Invoke(_startGridPos, targetPos);
        }

        private Vector2Int GetSwipeDirection(Vector2 delta)
        {
            // Determine primary direction based on larger axis
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipe
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                // Vertical swipe
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }
    }
}
```

### Dependencies (SerializeField)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| _minSwipeDistance | float | Yes | Minimum swipe distance in world units (default: 0.3) |
| _camera | Camera | No | Camera for screen→world conversion (defaults to Camera.main) |
| _gridView | GridView | Yes | Reference to GridView for GridData access |

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| OnSwipeDetected | `event Action<Vector2Int, Vector2Int>` | Fires with (from, to) grid positions |
| IsEnabled | `bool { get; set; }` | Enable/disable input processing |
| Initialize | `void Initialize(GridData)` | Set GridData at runtime |

### Input Handling Notes

- **Mouse:** GetMouseButtonDown(0) / GetMouseButtonUp(0)
- **Touch:** Input.GetTouch(0) with TouchPhase
- **Direction:** 4-directional only (Match3 standard)
- **Validation:** Start position must be on grid, target must be adjacent and on grid

---

## Component 2: SwapSystem

**File:** `Assets/Scripts/Swap/SwapSystem.cs`

**Type:** Plain C# class (not MonoBehaviour)

**Responsibility:** Валидирует swap (соседние ячейки, обе содержат gem-ы). Выполняет swap в BoardData. Проверяет будет ли match после swap.

### Code

```csharp
using UnityEngine;
using Match3.Board;

namespace Match3.Swap
{
    public class SwapSystem
    {
        /// <summary>
        /// Checks if swap is valid: positions are adjacent and both contain gems.
        /// </summary>
        public bool IsValidSwap(Vector2Int from, Vector2Int to, BoardData board)
        {
            // Check both positions are valid
            if (!board.IsValidPosition(from) || !board.IsValidPosition(to))
                return false;

            // Check both positions have gems
            if (board.IsEmpty(from) || board.IsEmpty(to))
                return false;

            // Check positions are adjacent (Manhattan distance = 1)
            if (!AreAdjacent(from, to))
                return false;

            return true;
        }

        /// <summary>
        /// Performs swap in BoardData. Does NOT validate - call IsValidSwap first.
        /// </summary>
        public void PerformSwap(BoardData board, Vector2Int a, Vector2Int b)
        {
            board.SwapGems(a, b);
        }

        /// <summary>
        /// Checks if swap would result in a match.
        /// Performs temporary swap, checks, then reverts.
        /// </summary>
        /// <param name="a">First position</param>
        /// <param name="b">Second position</param>
        /// <param name="board">Board data</param>
        /// <param name="matchChecker">Function that checks if position has match</param>
        /// <returns>True if swap would create at least one match</returns>
        public bool WillMatch(Vector2Int a, Vector2Int b, BoardData board,
            System.Func<BoardData, Vector2Int, bool> matchChecker)
        {
            // Perform swap
            board.SwapGems(a, b);

            // Check for matches at both positions
            bool hasMatch = matchChecker(board, a) || matchChecker(board, b);

            // Revert swap
            board.SwapGems(a, b);

            return hasMatch;
        }

        /// <summary>
        /// Simple overload that always returns true (for testing without MatchSystem).
        /// Replace with proper MatchSystem integration in Step 6.
        /// </summary>
        public bool WillMatch(Vector2Int a, Vector2Int b, BoardData board)
        {
            // Placeholder - will be replaced with MatchSystem integration
            // For now, all valid swaps are allowed
            return true;
        }

        /// <summary>
        /// Checks if two positions are adjacent (Manhattan distance = 1).
        /// </summary>
        public bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| IsValidSwap | `bool IsValidSwap(Vector2Int, Vector2Int, BoardData)` | Check if swap is valid |
| PerformSwap | `void PerformSwap(BoardData, Vector2Int, Vector2Int)` | Execute swap in data |
| WillMatch | `bool WillMatch(Vector2Int, Vector2Int, BoardData, Func)` | Check if swap creates match |
| WillMatch | `bool WillMatch(Vector2Int, Vector2Int, BoardData)` | Placeholder (always true) |
| AreAdjacent | `bool AreAdjacent(Vector2Int, Vector2Int)` | Check if positions are neighbors |

### Notes

- `WillMatch` с Func параметром позволяет инъектировать MatchSystem.HasMatchAt без прямой зависимости
- Placeholder `WillMatch` без Func нужен для тестирования до Step 6
- BoardData.SwapGems уже реализован и не стреляет событиями

---

## Component 3: SwapAnimator

**File:** `Assets/Scripts/Swap/SwapAnimator.cs`

**Type:** MonoBehaviour

**Responsibility:** Анимирует обмен двух gem-ов с помощью DOTween. Анимирует возврат при неудачном swap.

### Code

```csharp
using System;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;

namespace Match3.Swap
{
    public class SwapAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private Ease _swapEase = Ease.OutQuad;
        [SerializeField] private float _swapBackDuration = 0.15f;
        [SerializeField] private Ease _swapBackEase = Ease.InOutQuad;

        private Sequence _currentSequence;

        /// <summary>
        /// Fires when swap animation completes.
        /// </summary>
        public event Action OnSwapComplete;

        /// <summary>
        /// Fires when swap-back animation completes.
        /// </summary>
        public event Action OnSwapBackComplete;

        /// <summary>
        /// Animates two gems swapping positions.
        /// Returns Tween for chaining or null if invalid.
        /// </summary>
        public Tween AnimateSwap(GemView a, GemView b)
        {
            if (a == null || b == null)
            {
                OnSwapComplete?.Invoke();
                return null;
            }

            // Kill any running animation
            KillCurrentAnimation();

            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;

            _currentSequence = DOTween.Sequence();

            // Move both gems simultaneously
            _currentSequence.Join(
                a.transform.DOMove(posB, _swapDuration).SetEase(_swapEase)
            );
            _currentSequence.Join(
                b.transform.DOMove(posA, _swapDuration).SetEase(_swapEase)
            );

            _currentSequence.OnComplete(() =>
            {
                _currentSequence = null;
                OnSwapComplete?.Invoke();
            });

            return _currentSequence;
        }

        /// <summary>
        /// Animates two gems swapping back to original positions.
        /// Used when swap doesn't result in a match.
        /// </summary>
        public Tween AnimateSwapBack(GemView a, GemView b)
        {
            if (a == null || b == null)
            {
                OnSwapBackComplete?.Invoke();
                return null;
            }

            // Kill any running animation
            KillCurrentAnimation();

            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;

            _currentSequence = DOTween.Sequence();

            // Add small delay before swap back for visual feedback
            _currentSequence.AppendInterval(0.05f);

            // Move both gems back
            _currentSequence.Join(
                a.transform.DOMove(posB, _swapBackDuration).SetEase(_swapBackEase)
            );
            _currentSequence.Join(
                b.transform.DOMove(posA, _swapBackDuration).SetEase(_swapBackEase)
            );

            _currentSequence.OnComplete(() =>
            {
                _currentSequence = null;
                OnSwapBackComplete?.Invoke();
            });

            return _currentSequence;
        }

        /// <summary>
        /// Returns true if animation is currently playing.
        /// </summary>
        public bool IsAnimating => _currentSequence != null && _currentSequence.IsPlaying();

        /// <summary>
        /// Kills current animation immediately.
        /// </summary>
        public void KillCurrentAnimation()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
```

### Dependencies (SerializeField)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| _swapDuration | float | Yes | Swap animation duration (default: 0.2s) |
| _swapEase | Ease | Yes | DOTween ease for swap (default: OutQuad) |
| _swapBackDuration | float | Yes | Swap-back duration (default: 0.15s) |
| _swapBackEase | Ease | Yes | DOTween ease for swap-back (default: InOutQuad) |

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| OnSwapComplete | `event Action` | Fires when swap animation completes |
| OnSwapBackComplete | `event Action` | Fires when swap-back animation completes |
| AnimateSwap | `Tween AnimateSwap(GemView, GemView)` | Animate gem swap |
| AnimateSwapBack | `Tween AnimateSwapBack(GemView, GemView)` | Animate swap reversal |
| IsAnimating | `bool` | True if animation in progress |
| KillCurrentAnimation | `void KillCurrentAnimation()` | Stop current animation |

### Animation Notes

- **Swap:** Both gems move simultaneously with OutQuad (fast start, smooth end)
- **Swap-back:** Small delay (0.05s) then move back with InOutQuad (smooth both ways)
- **Sequence:** Uses DOTween.Sequence for synchronized parallel movement
- **Cleanup:** Kills previous animation before starting new one

---

## Integration: Complete Swap Flow

### Full Flow Example

```csharp
// This will be in GameController (Step 8)
// Shown here for understanding

public class SwapFlowExample
{
    // Dependencies
    private BoardView _boardView;
    private BoardData _boardData;
    private SwipeDetector _swipeDetector;
    private SwapSystem _swapSystem;
    private SwapAnimator _swapAnimator;
    // private MatchSystem _matchSystem; // Step 6

    private void OnEnable()
    {
        _swipeDetector.OnSwipeDetected += HandleSwipe;
        _swapAnimator.OnSwapComplete += HandleSwapComplete;
        _swapAnimator.OnSwapBackComplete += HandleSwapBackComplete;
    }

    private void OnDisable()
    {
        _swipeDetector.OnSwipeDetected -= HandleSwipe;
        _swapAnimator.OnSwapComplete -= HandleSwapComplete;
        _swapAnimator.OnSwapBackComplete -= HandleSwapBackComplete;
    }

    private Vector2Int _swapFrom;
    private Vector2Int _swapTo;

    private void HandleSwipe(Vector2Int from, Vector2Int to)
    {
        // 1. Validate swap
        if (!_swapSystem.IsValidSwap(from, to, _boardData))
            return;

        // 2. Disable input during animation
        _swipeDetector.IsEnabled = false;

        // 3. Store positions for later
        _swapFrom = from;
        _swapTo = to;

        // 4. Perform swap in data
        _swapSystem.PerformSwap(_boardData, from, to);

        // 5. Update BoardView tracking
        _boardView.UpdateViewPosition(from, to);
        _boardView.UpdateViewPosition(to, from);

        // 6. Animate swap
        var gemA = _boardView.GetView(to);  // Now at 'to' after data swap
        var gemB = _boardView.GetView(from); // Now at 'from' after data swap
        _swapAnimator.AnimateSwap(gemA, gemB);
    }

    private void HandleSwapComplete()
    {
        // 7. Check for matches (Step 6)
        // bool hasMatch = _matchSystem.HasMatchAt(_boardData, _swapFrom) ||
        //                 _matchSystem.HasMatchAt(_boardData, _swapTo);
        bool hasMatch = true; // Placeholder until Step 6

        if (hasMatch)
        {
            // Continue to match/destroy flow
            _swipeDetector.IsEnabled = true;
            // Trigger match detection...
        }
        else
        {
            // No match - swap back
            _swapSystem.PerformSwap(_boardData, _swapFrom, _swapTo);
            _boardView.UpdateViewPosition(_swapFrom, _swapTo);
            _boardView.UpdateViewPosition(_swapTo, _swapFrom);

            var gemA = _boardView.GetView(_swapFrom);
            var gemB = _boardView.GetView(_swapTo);
            _swapAnimator.AnimateSwapBack(gemA, gemB);
        }
    }

    private void HandleSwapBackComplete()
    {
        // 8. Re-enable input
        _swipeDetector.IsEnabled = true;
    }
}
```

---

## BoardView Addition

**Note:** UpdateViewPosition нужно вызывать дважды при swap (для каждого gem-а). Текущая реализация уже поддерживает это.

### View Tracking After Swap

```
Before swap:
  Position A: GemView_Red
  Position B: GemView_Blue

After BoardData.SwapGems(A, B):
  Data at A: Blue gem
  Data at B: Red gem

After BoardView.UpdateViewPosition(A, B) + UpdateViewPosition(B, A):
  _views[A]: GemView_Blue
  _views[B]: GemView_Red
```

**Important:** Порядок вызовов UpdateViewPosition имеет значение. Для swap нужно:
1. Сохранить оба view
2. Обновить оба одновременно

Либо добавить метод SwapViews в BoardView (опционально):

```csharp
// Add to BoardView.cs
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
```

---

## Scene Setup

### Hierarchy

```
Scene
└── Grid
    └── GridView (component)
└── Board
    ├── BoardView (component)
    └── Gems
└── Systems
    ├── SwipeDetector (component)
    └── SwapAnimator (component)
```

### Inspector Setup

#### SwipeDetector
1. Add to "Systems" GameObject (or create "InputSystem")
2. Add `SwipeDetector` component
3. Assign references:
   - _minSwipeDistance: 0.3
   - _camera: (leave empty for Camera.main)
   - _gridView: drag GridView from scene

#### SwapAnimator
1. Add to "Systems" GameObject
2. Add `SwapAnimator` component
3. Assign references:
   - _swapDuration: 0.2
   - _swapEase: OutQuad
   - _swapBackDuration: 0.15
   - _swapBackEase: InOutQuad

---

## Data Flow Diagram

```
┌────────────────────────────────────────────────────────────────────┐
│                         SWAP FLOW                                   │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [Touch/Click Down] on board                                        │
│         │                                                           │
│         ▼                                                           │
│  SwipeDetector.HandlePointerDown()                                  │
│         │                                                           │
│         ├──► ScreenToWorld(screenPos)                               │
│         ├──► GridData.WorldToGrid(worldPos)                         │
│         ├──► Check GridData.IsValidPosition()                       │
│         └──► Store _startGridPos, _isDragging = true                │
│                                                                     │
│  [Drag]                                                             │
│         │                                                           │
│         ▼                                                           │
│  (nothing during drag - only track start/end)                       │
│                                                                     │
│  [Touch/Click Up]                                                   │
│         │                                                           │
│         ▼                                                           │
│  SwipeDetector.HandlePointerUp()                                    │
│         │                                                           │
│         ├──► Calculate delta = endPos - startPos                    │
│         ├──► Check minimum distance                                 │
│         ├──► GetSwipeDirection() → Vector2Int (4-dir)               │
│         ├──► Calculate targetPos = startPos + direction             │
│         ├──► Check GridData.IsValidPosition(targetPos)              │
│         └──► Fire OnSwipeDetected(from, to)                         │
│                    │                                                │
│                    ▼                                                │
│  GameController receives event                                      │
│         │                                                           │
│         ├──► SwapSystem.IsValidSwap(from, to, board)                │
│         │    ├── Check both positions valid                         │
│         │    ├── Check both have gems                               │
│         │    └── Check adjacent (Manhattan = 1)                     │
│         │                                                           │
│         ├──► If invalid → return (do nothing)                       │
│         │                                                           │
│         ├──► SwipeDetector.IsEnabled = false                        │
│         ├──► SwapSystem.PerformSwap(board, from, to)                │
│         ├──► BoardView.SwapViews(from, to)                          │
│         └──► SwapAnimator.AnimateSwap(gemA, gemB)                   │
│                    │                                                │
│                    ▼                                                │
│  [Animation Playing]                                                │
│         │                                                           │
│         └──► DOTween.Sequence moves both gems                       │
│                    │                                                │
│                    ▼                                                │
│  SwapAnimator.OnSwapComplete                                        │
│         │                                                           │
│         ├──► MatchSystem.HasMatchAt(from) || HasMatchAt(to)         │
│         │    (Step 6 integration)                                   │
│         │                                                           │
│         ├──► If HAS match:                                          │
│         │    └── Continue to Match/Destroy flow (Step 7)            │
│         │                                                           │
│         └──► If NO match:                                           │
│              ├── SwapSystem.PerformSwap (reverse)                   │
│              ├── BoardView.SwapViews (reverse)                      │
│              └── SwapAnimator.AnimateSwapBack()                     │
│                         │                                           │
│                         ▼                                           │
│              OnSwapBackComplete                                     │
│                         │                                           │
│                         └──► SwipeDetector.IsEnabled = true         │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

---

## Edge Cases

### 1. Swipe starts outside grid
- HandlePointerDown checks IsValidPosition
- If outside grid, _isDragging stays false
- No event fired

### 2. Swipe ends outside grid
- targetPos calculated from direction
- IsValidPosition check before firing event
- Invalid target = no event

### 3. Swipe too short
- delta.magnitude < _minSwipeDistance
- No direction determined, no event

### 4. Swipe to empty cell
- SwapSystem.IsValidSwap checks board.IsEmpty
- Returns false if target empty
- No animation triggered

### 5. Diagonal swipe
- GetSwipeDirection uses larger axis
- Always returns one of 4 cardinal directions
- Diagonal input mapped to nearest axis

### 6. Rapid taps (no drag)
- Very short delta < minSwipeDistance
- Treated as cancelled swipe

### 7. Animation interrupted
- KillCurrentAnimation called before new animation
- Clean handoff to new animation

### 8. Multiple touches
- Only first touch (Input.GetTouch(0)) processed
- Additional touches ignored

---

## Testing Checklist

### SwipeDetector Tests
- [ ] Mouse click on grid records start position
- [ ] Mouse release calculates direction correctly
- [ ] Touch input works same as mouse
- [ ] Swipe outside grid ignored
- [ ] Short swipes (< minDistance) ignored
- [ ] Diagonal swipes map to nearest cardinal direction
- [ ] IsEnabled = false prevents input processing
- [ ] Event contains correct from/to positions

### SwapSystem Tests
- [ ] IsValidSwap returns false for same position
- [ ] IsValidSwap returns false for non-adjacent positions
- [ ] IsValidSwap returns false for empty cells
- [ ] IsValidSwap returns true for valid adjacent gems
- [ ] PerformSwap updates BoardData correctly
- [ ] AreAdjacent returns correct results for all cases
- [ ] WillMatch performs temporary swap and reverts

### SwapAnimator Tests
- [ ] AnimateSwap moves both gems to swapped positions
- [ ] Animation duration matches settings
- [ ] OnSwapComplete fires after animation
- [ ] AnimateSwapBack works correctly
- [ ] OnSwapBackComplete fires after swap-back
- [ ] IsAnimating returns correct state
- [ ] KillCurrentAnimation stops animation immediately
- [ ] Null gems don't cause errors

### Integration Tests
- [ ] Full swipe → swap → animate flow works
- [ ] Input disabled during animation
- [ ] Input re-enabled after animation completes
- [ ] Invalid swap shows no visual feedback
- [ ] Swap-back animation plays when no match

---

## File Checklist

- [ ] Create `Assets/Scripts/Input/` folder
- [ ] Create `Assets/Scripts/Swap/` folder
- [ ] Implement `SwipeDetector.cs`
- [ ] Implement `SwapSystem.cs`
- [ ] Implement `SwapAnimator.cs`
- [ ] (Optional) Add `SwapViews()` to BoardView
- [ ] Add SwipeDetector to scene
- [ ] Add SwapAnimator to scene
- [ ] Assign references in Inspector
- [ ] Test with manual swipes

---

## Notes

- SwipeDetector is MonoBehaviour for Update loop and Camera reference
- SwapSystem is pure C# for testability
- SwapAnimator is MonoBehaviour for DOTween and lifetime management
- 4-directional swipe (not 8) is Match3 standard
- WillMatch placeholder returns true until Step 6 integration
- BoardView.SwapViews is optional helper for cleaner code
- Input disabled during animation prevents race conditions
- All animations use DOTween.Sequence for synchronized movement
