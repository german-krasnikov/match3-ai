# Step 4: Fall System

## Цель

Система падения gem-ов: вычисление перемещений, анимация падения, спаун новых gem-ов сверху.

---

## Архитектура

```
┌──────────────────────────────────────────────────────────────┐
│                    FallController (future)                    │
│          Координация: CalculateFalls → Animate → Spawn        │
└──────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│   FallSystem    │   │  FallAnimator   │   │   SpawnSystem   │
│   (C# class)    │   │ (MonoBehaviour) │   │  (from Step 3)  │
│                 │   │                 │   │                 │
│ CalculateFalls  │   │ AnimateFall     │   │ GenerateType    │
│ ApplyFalls      │   │ AnimateFalls    │   │                 │
│ CountEmptyAbove │   │ OnAllComplete   │   │                 │
└─────────────────┘   └─────────────────┘   └─────────────────┘
         │                    │
         ▼                    ▼
┌─────────────────┐   ┌─────────────────┐
│   BoardData     │   │   BoardView     │
│  (from Step 3)  │   │  (from Step 3)  │
│  MoveGem        │   │  GetView        │
│  IsEmpty        │   │  CreateGemAbove │
│  SetGem         │   │  UpdateViewPos  │
│                 │   │  RegisterView   │
└─────────────────┘   └─────────────────┘
```

**Flow:**
1. DestroySystem удаляет gem-ы → появляются пустые ячейки
2. FallSystem.CalculateFalls() — находит все FallMove (from → to)
3. FallSystem.ApplyFalls() — обновляет BoardData (MoveGem)
4. BoardView.UpdateViewPosition() — обновляет массив _views
5. FallAnimator.AnimateFalls() — анимирует все падения параллельно
6. Для каждого столбца: спаунить недостающие gem-ы сверху
7. FallAnimator.OnAllFallsComplete — сигнал завершения

---

## Файловая структура

```
Assets/Scripts/
  Fall/
    FallSystem.cs         # C# class, логика вычисления падений
    FallAnimator.cs       # MonoBehaviour, DOTween анимации
    FallMove.cs           # struct, данные о перемещении
```

---

## Component 1: FallMove

**File:** `Assets/Scripts/Fall/FallMove.cs`

**Type:** struct

**Responsibility:** Данные о перемещении одного gem-а при падении.

### Code

```csharp
using UnityEngine;

namespace Match3.Fall
{
    /// <summary>
    /// Represents a single gem fall movement.
    /// </summary>
    public readonly struct FallMove
    {
        /// <summary>
        /// Starting grid position.
        /// </summary>
        public Vector2Int From { get; }

        /// <summary>
        /// Target grid position.
        /// </summary>
        public Vector2Int To { get; }

        /// <summary>
        /// Distance in cells (for animation timing).
        /// </summary>
        public int Distance => From.y - To.y;

        public FallMove(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"Fall({From} -> {To}, dist={Distance})";
        }
    }
}
```

### Public API

| Member | Type | Description |
|--------|------|-------------|
| From | `Vector2Int` | Starting grid position |
| To | `Vector2Int` | Target grid position |
| Distance | `int` | Number of cells to fall |
| Constructor | `FallMove(Vector2Int, Vector2Int)` | Creates fall move |

---

## Component 2: FallSystem

**File:** `Assets/Scripts/Fall/FallSystem.cs`

**Type:** Plain C# class (not MonoBehaviour)

**Responsibility:** Вычисляет падения для заполнения пустых ячеек. Применяет изменения к BoardData.

### Code

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;

namespace Match3.Fall
{
    public class FallSystem
    {
        /// <summary>
        /// Calculates all fall moves needed to fill empty cells.
        /// Gems fall straight down. Returns moves sorted by column, then by row (bottom first).
        /// </summary>
        public List<FallMove> CalculateFalls(BoardData board)
        {
            var moves = new List<FallMove>();

            // Process each column independently
            for (int x = 0; x < board.Width; x++)
            {
                CalculateFallsForColumn(board, x, moves);
            }

            return moves;
        }

        /// <summary>
        /// Applies fall moves to BoardData.
        /// IMPORTANT: Apply from bottom to top to avoid overwriting.
        /// Does NOT trigger BoardData events (MoveGem is silent).
        /// </summary>
        public void ApplyFalls(BoardData board, List<FallMove> moves)
        {
            // Sort moves: process bottom rows first within each column
            // This ensures we don't overwrite gems that haven't moved yet
            moves.Sort((a, b) =>
            {
                if (a.From.x != b.From.x)
                    return a.From.x.CompareTo(b.From.x);
                return a.To.y.CompareTo(b.To.y); // Lower target first
            });

            foreach (var move in moves)
            {
                board.MoveGem(move.From, move.To);
            }
        }

        /// <summary>
        /// Counts empty cells in column (for spawning new gems).
        /// </summary>
        public int CountEmptyInColumn(BoardData board, int column)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
            {
                if (board.IsEmpty(new Vector2Int(column, y)))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Returns empty cell positions in column from bottom to top.
        /// Used for determining where new gems should land.
        /// </summary>
        public List<Vector2Int> GetEmptyPositionsInColumn(BoardData board, int column)
        {
            var positions = new List<Vector2Int>();
            for (int y = 0; y < board.Height; y++)
            {
                var pos = new Vector2Int(column, y);
                if (board.IsEmpty(pos))
                    positions.Add(pos);
            }
            return positions;
        }

        // --- Private Helpers ---

        private void CalculateFallsForColumn(BoardData board, int column, List<FallMove> moves)
        {
            // Track where the next gem should land
            int writeIndex = 0;

            // Scan from bottom to top
            for (int readIndex = 0; readIndex < board.Height; readIndex++)
            {
                var pos = new Vector2Int(column, readIndex);

                if (!board.IsEmpty(pos))
                {
                    // Gem exists at readIndex
                    if (readIndex != writeIndex)
                    {
                        // Gem needs to fall
                        var from = new Vector2Int(column, readIndex);
                        var to = new Vector2Int(column, writeIndex);
                        moves.Add(new FallMove(from, to));
                    }
                    writeIndex++;
                }
                // If empty, writeIndex stays put, waiting for next gem
            }
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| CalculateFalls | `List<FallMove> CalculateFalls(BoardData board)` | Calculate all needed falls |
| ApplyFalls | `void ApplyFalls(BoardData board, List<FallMove> moves)` | Apply falls to data |
| CountEmptyInColumn | `int CountEmptyInColumn(BoardData board, int column)` | Count empty cells in column |
| GetEmptyPositionsInColumn | `List<Vector2Int> GetEmptyPositionsInColumn(BoardData board, int column)` | Get empty positions |

### Algorithm

```
Column example (bottom to top):
Before: [_, R, _, G, B]  (_ = empty)
         0  1  2  3  4   (row index)

Algorithm:
- writeIndex = 0
- Row 0: empty, skip
- Row 1: R exists, R != writeIndex(0), add FallMove(1→0), writeIndex=1
- Row 2: empty, skip
- Row 3: G exists, 3 != writeIndex(1), add FallMove(3→1), writeIndex=2
- Row 4: B exists, 4 != writeIndex(2), add FallMove(4→2), writeIndex=3

Moves: [(1,0), (3,1), (4,2)]
After:  [R, G, B, _, _]
         0  1  2  3  4
```

---

## Component 3: FallAnimator

**File:** `Assets/Scripts/Fall/FallAnimator.cs`

**Type:** MonoBehaviour

**Responsibility:** Анимирует падение GemView с помощью DOTween. Отслеживает завершение всех анимаций.

### Code

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;
using Match3.Grid;

namespace Match3.Fall
{
    public class FallAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _fallSpeed = 8f;
        [SerializeField] private float _minDuration = 0.1f;
        [SerializeField] private Ease _fallEase = Ease.InQuad;

        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;

        private int _activeTweens;
        private GridData _gridData;

        /// <summary>
        /// Fires when ALL fall animations complete.
        /// </summary>
        public event Action OnAllFallsComplete;

        private void Awake()
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

        /// <summary>
        /// Animates single gem fall to target grid position.
        /// </summary>
        /// <param name="gem">GemView to animate</param>
        /// <param name="targetGridPos">Target grid position</param>
        /// <returns>Tween for chaining</returns>
        public Tween AnimateFall(GemView gem, Vector2Int targetGridPos)
        {
            if (_gridData == null)
            {
                Debug.LogError("FallAnimator: GridData not set!");
                return null;
            }

            Vector3 targetWorldPos = _gridData.GridToWorld(targetGridPos);
            float distance = Mathf.Abs(gem.transform.position.y - targetWorldPos.y);
            float duration = Mathf.Max(distance / _fallSpeed, _minDuration);

            return gem.transform
                .DOMove(targetWorldPos, duration)
                .SetEase(_fallEase);
        }

        /// <summary>
        /// Animates gem fall from world position to target grid position.
        /// Used for newly spawned gems above grid.
        /// </summary>
        public Tween AnimateFallFromPosition(GemView gem, Vector3 startPos, Vector2Int targetGridPos)
        {
            if (_gridData == null)
            {
                Debug.LogError("FallAnimator: GridData not set!");
                return null;
            }

            gem.transform.position = startPos;
            Vector3 targetWorldPos = _gridData.GridToWorld(targetGridPos);
            float distance = Mathf.Abs(startPos.y - targetWorldPos.y);
            float duration = Mathf.Max(distance / _fallSpeed, _minDuration);

            return gem.transform
                .DOMove(targetWorldPos, duration)
                .SetEase(_fallEase);
        }

        /// <summary>
        /// Animates multiple falls. Fires OnAllFallsComplete when done.
        /// </summary>
        /// <param name="gems">List of (GemView, targetGridPos) pairs</param>
        public void AnimateFalls(List<(GemView gem, Vector2Int targetPos)> falls)
        {
            if (falls == null || falls.Count == 0)
            {
                OnAllFallsComplete?.Invoke();
                return;
            }

            _activeTweens = falls.Count;

            foreach (var (gem, targetPos) in falls)
            {
                var tween = AnimateFall(gem, targetPos);
                if (tween != null)
                {
                    tween.OnComplete(HandleTweenComplete);
                }
                else
                {
                    HandleTweenComplete();
                }
            }
        }

        /// <summary>
        /// Animates falls for existing gems + newly spawned gems.
        /// </summary>
        /// <param name="existingFalls">Existing gems falling down</param>
        /// <param name="newGems">New gems spawning from above</param>
        public void AnimateAllFalls(
            List<(GemView gem, Vector2Int targetPos)> existingFalls,
            List<(GemView gem, Vector3 startPos, Vector2Int targetPos)> newGems)
        {
            int totalCount =
                (existingFalls?.Count ?? 0) +
                (newGems?.Count ?? 0);

            if (totalCount == 0)
            {
                OnAllFallsComplete?.Invoke();
                return;
            }

            _activeTweens = totalCount;

            // Animate existing gems
            if (existingFalls != null)
            {
                foreach (var (gem, targetPos) in existingFalls)
                {
                    var tween = AnimateFall(gem, targetPos);
                    if (tween != null)
                        tween.OnComplete(HandleTweenComplete);
                    else
                        HandleTweenComplete();
                }
            }

            // Animate new gems from spawn position
            if (newGems != null)
            {
                foreach (var (gem, startPos, targetPos) in newGems)
                {
                    var tween = AnimateFallFromPosition(gem, startPos, targetPos);
                    if (tween != null)
                        tween.OnComplete(HandleTweenComplete);
                    else
                        HandleTweenComplete();
                }
            }
        }

        /// <summary>
        /// Kills all active fall tweens.
        /// </summary>
        public void StopAll()
        {
            DOTween.Kill(transform);
            _activeTweens = 0;
        }

        // --- Private Helpers ---

        private void HandleTweenComplete()
        {
            _activeTweens--;
            if (_activeTweens <= 0)
            {
                _activeTweens = 0;
                OnAllFallsComplete?.Invoke();
            }
        }
    }
}
```

### Dependencies (SerializeField)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| _fallSpeed | float | Yes | Fall speed in units/sec (default: 8) |
| _minDuration | float | Yes | Minimum fall duration (default: 0.1) |
| _fallEase | Ease | Yes | DOTween ease (default: Ease.InQuad) |
| _gridView | GridView | Yes | Reference to GridView for coordinate conversion |

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| OnAllFallsComplete | `event Action` | Fires when all animations done |
| Initialize | `void Initialize(GridData gridData)` | Set GridData at runtime |
| AnimateFall | `Tween AnimateFall(GemView, Vector2Int)` | Animate single fall |
| AnimateFallFromPosition | `Tween AnimateFallFromPosition(GemView, Vector3, Vector2Int)` | Animate from spawn position |
| AnimateFalls | `void AnimateFalls(List<(GemView, Vector2Int)>)` | Animate multiple falls |
| AnimateAllFalls | `void AnimateAllFalls(existing, newGems)` | Animate all (existing + new) |
| StopAll | `void StopAll()` | Kill all active tweens |

### Animation Notes

- **Speed-based duration:** `duration = distance / _fallSpeed`
- **Minimum duration:** Prevents instant snapping for small distances
- **Ease:** `Ease.InQuad` gives natural gravity acceleration feel
- **Parallel execution:** All falls animate simultaneously
- **Tracking:** Counts active tweens, fires OnAllFallsComplete when all done

---

## Integration: Fall + Spawn Flow

### Full Flow Example

```csharp
// This will be in GameController (Step 8)
// Shown here for understanding

public class FallFlowExample
{
    // Dependencies
    private BoardView _boardView;
    private FallSystem _fallSystem;
    private FallAnimator _fallAnimator;
    private SpawnSystem _spawnSystem;
    private GridData _gridData;

    public void ExecuteFallAndSpawn()
    {
        var board = _boardView.Data;

        // 1. Calculate falls for existing gems
        var falls = _fallSystem.CalculateFalls(board);

        // 2. Collect animation data BEFORE applying to data
        var existingFalls = new List<(GemView gem, Vector2Int targetPos)>();
        foreach (var fall in falls)
        {
            var view = _boardView.GetView(fall.From);
            if (view != null)
            {
                existingFalls.Add((view, fall.To));
            }
        }

        // 3. Apply falls to data + update view tracking
        _fallSystem.ApplyFalls(board, falls);
        foreach (var fall in falls)
        {
            _boardView.UpdateViewPosition(fall.From, fall.To);
        }

        // 4. Spawn new gems for empty cells
        var newGems = new List<(GemView gem, Vector3 startPos, Vector2Int targetPos)>();
        for (int col = 0; col < board.Width; col++)
        {
            var emptyPositions = _fallSystem.GetEmptyPositionsInColumn(board, col);

            for (int i = 0; i < emptyPositions.Count; i++)
            {
                var targetPos = emptyPositions[i];

                // Generate type and create data
                var type = _spawnSystem.GenerateType(targetPos, board);
                var gem = new GemData(type, targetPos);

                // Set in BoardData (no event - we handle view manually)
                board.SetGemSilent(targetPos, gem); // NOTE: Need to add this method

                // Create view above grid
                int rowsAbove = emptyPositions.Count - i; // Higher spawn for higher targets
                var spawnPos = _gridData.GetSpawnPosition(col, rowsAbove);
                var view = _boardView.CreateGemAbove(col, rowsAbove, gem);

                // Register view at target position
                _boardView.RegisterView(targetPos, view);

                newGems.Add((view, spawnPos, targetPos));
            }
        }

        // 5. Animate all falls together
        _fallAnimator.AnimateAllFalls(existingFalls, newGems);

        // 6. Wait for OnAllFallsComplete event
    }
}
```

---

## BoardData Addition

**Note:** Need to add silent SetGem to BoardData for spawning (avoids duplicate view creation).

Add to `Assets/Scripts/Board/BoardData.cs`:

```csharp
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
└── Grid
    └── GridView (component)
└── Board
    ├── BoardView (component)
    └── Gems
└── Systems
    └── FallAnimator (component)
```

### Inspector Setup for FallAnimator

1. Create empty GameObject "Systems" in scene (or add to existing)
2. Create child "FallAnimator" or add component to Systems
3. Add `FallAnimator` component
4. Assign references:
   - _fallSpeed: 8
   - _minDuration: 0.1
   - _fallEase: InQuad
   - _gridView: drag GridView from scene

---

## Data Flow Diagram

```
┌────────────────────────────────────────────────────────────────────┐
│                        FALL + SPAWN FLOW                            │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [Gems Destroyed] → Empty cells in BoardData                        │
│         │                                                           │
│         ▼                                                           │
│  FallSystem.CalculateFalls(board)                                   │
│         │                                                           │
│         ├──► For each column, scan bottom to top                    │
│         │    Track writeIndex for next landing spot                 │
│         │    If gem found and row != writeIndex → add FallMove      │
│         │                                                           │
│         └──► Returns List<FallMove>                                 │
│                    │                                                │
│                    ▼                                                │
│  Collect animation data (GemView, targetPos) from FallMoves        │
│                    │                                                │
│                    ▼                                                │
│  FallSystem.ApplyFalls(board, moves)                                │
│         │                                                           │
│         └──► For each move: board.MoveGem(from, to)                 │
│                    │                                                │
│                    ▼                                                │
│  BoardView.UpdateViewPosition(from, to) for each move              │
│                    │                                                │
│                    ▼                                                │
│  For each column: count empty cells remaining                       │
│         │                                                           │
│         ├──► SpawnSystem.GenerateType() for each empty              │
│         ├──► BoardData.SetGemSilent() (no event)                    │
│         ├──► BoardView.CreateGemAbove() → GemView at spawn pos      │
│         └──► BoardView.RegisterView() at target position            │
│                    │                                                │
│                    ▼                                                │
│  FallAnimator.AnimateAllFalls(existingFalls, newGems)               │
│         │                                                           │
│         ├──► Start all DOTween.DOMove() in parallel                 │
│         ├──► Track _activeTweens count                              │
│         └──► Each OnComplete → decrement → check if all done        │
│                    │                                                │
│                    ▼                                                │
│  OnAllFallsComplete event → GameController → check for new matches  │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

---

## Edge Cases

### 1. No empty cells
- CalculateFalls returns empty list
- AnimateFalls immediately fires OnAllFallsComplete

### 2. Entire column empty
- No existing gems to fall
- Only new gems spawn from above

### 3. Bottom gem destroyed, others remain
- Upper gems all fall down
- New gems spawn to fill top

### 4. Multiple columns with different empty counts
- Each column processed independently
- All animations run in parallel

### 5. Rapid successive calls
- StopAll() can be called to cancel in-progress animations
- New AnimateAllFalls will reset _activeTweens counter

---

## Testing Checklist

### FallMove Tests
- [ ] Distance calculated correctly (From.y - To.y)
- [ ] ToString returns readable format

### FallSystem Tests
- [ ] Empty board returns no moves
- [ ] Full board returns no moves
- [ ] Single empty cell: gem above falls down
- [ ] Multiple empty cells: all gems compact correctly
- [ ] Column independence: changes in one column don't affect others
- [ ] ApplyFalls updates BoardData correctly
- [ ] CountEmptyInColumn returns correct count
- [ ] GetEmptyPositionsInColumn returns positions bottom to top

### FallAnimator Tests
- [ ] AnimateFall moves gem to correct position
- [ ] Animation duration = distance / speed
- [ ] Minimum duration enforced
- [ ] AnimateFalls with empty list fires OnAllFallsComplete immediately
- [ ] OnAllFallsComplete fires after ALL tweens complete
- [ ] StopAll kills active animations

### Integration Tests
- [ ] Destroy 1 gem → 1 gem falls → 1 new gem spawns
- [ ] Destroy bottom gem → column compacts correctly
- [ ] Destroy multiple in column → correct cascade
- [ ] New gems spawn at correct positions above grid
- [ ] New gems animate to correct target positions
- [ ] No visual glitches during animation

---

## File Checklist

- [ ] Create `Assets/Scripts/Fall/` folder
- [ ] Implement `FallMove.cs`
- [ ] Implement `FallSystem.cs`
- [ ] Implement `FallAnimator.cs`
- [ ] Add `SetGemSilent()` to BoardData
- [ ] Add FallAnimator to scene
- [ ] Assign references in Inspector
- [ ] Test with manual gem destruction

---

## Notes

- FallSystem is pure C# for testability
- FallAnimator is MonoBehaviour for DOTween and scene reference
- Animation uses speed-based duration, not fixed duration
- All falls animate in parallel for snappy feel
- Ease.InQuad gives natural gravity acceleration
- New gems spawn above grid and fall into place
- OnAllFallsComplete is the handoff point to GameController
