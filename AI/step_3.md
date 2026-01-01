# Step 3: Spawn System

## Цель

Хранение данных доски, спаун gem-ов с анти-матч логикой, визуализация через BoardView.

---

## Архитектура

```
               ┌──────────────────────────────────────────┐
               │           BoardView (MonoBehaviour)       │
               │    [SerializeField] GridView, GemConfig   │
               │         GemView[,] _views                 │
               │    CreateGem, DestroyGem, GetView         │
               └──────────────────────────────────────────┘
                        │                    │
          subscribes to │                    │ uses
                        ▼                    ▼
               ┌─────────────────┐   ┌─────────────────┐
               │   BoardData     │   │   SpawnSystem   │
               │   (C# class)    │   │   (C# class)    │
               │  GemData?[,]    │   │  GenerateType() │
               │   CRUD + events │   │  anti-match     │
               └─────────────────┘   └─────────────────┘
                        │
                 uses   │
                        ▼
               ┌─────────────────┐
               │    GridData     │
               │   (from Step 1) │
               └─────────────────┘
```

**Flow:**
1. BoardView создает BoardData в Awake
2. BoardView вызывает FillBoard() при старте
3. FillBoard итерирует по сетке, для каждой позиции:
   - SpawnSystem.GenerateType(pos, board) — возвращает тип без матча
   - BoardData.SetGem(pos, gem) — сохраняет данные
   - BoardData.OnGemAdded event fires
4. BoardView слушает OnGemAdded → CreateGemView()

---

## Файловая структура

```
Assets/Scripts/
  Board/
    BoardData.cs          # C# class, данные доски
    BoardView.cs          # MonoBehaviour, визуализация
  Spawn/
    SpawnSystem.cs        # C# class, логика генерации
```

---

## Component 1: BoardData

**File:** `Assets/Scripts/Board/BoardData.cs`

**Type:** Plain C# class (не MonoBehaviour)

**Responsibility:** Хранит GemData?[,] сетку. CRUD операции. События изменений.

### Code

```csharp
using System;
using UnityEngine;

namespace Match3.Board
{
    public class BoardData
    {
        private readonly GemData?[,] _gems;
        private readonly int _width;
        private readonly int _height;

        public int Width => _width;
        public int Height => _height;
        public Vector2Int Size => new Vector2Int(_width, _height);

        /// <summary>
        /// Fires when gem is added to position.
        /// </summary>
        public event Action<Vector2Int, GemData> OnGemAdded;

        /// <summary>
        /// Fires when gem is removed from position.
        /// </summary>
        public event Action<Vector2Int> OnGemRemoved;

        public BoardData(int width, int height)
        {
            _width = width;
            _height = height;
            _gems = new GemData?[width, height];
        }

        public BoardData(Vector2Int size) : this(size.x, size.y) { }

        /// <summary>
        /// Returns gem at position or null if empty/invalid.
        /// </summary>
        public GemData? GetGem(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return null;
            return _gems[pos.x, pos.y];
        }

        /// <summary>
        /// Sets gem at position. Overwrites existing.
        /// </summary>
        public void SetGem(Vector2Int pos, GemData gem)
        {
            if (!IsValidPosition(pos))
                return;
            _gems[pos.x, pos.y] = gem;
            OnGemAdded?.Invoke(pos, gem);
        }

        /// <summary>
        /// Removes gem at position.
        /// </summary>
        public void RemoveGem(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return;
            if (_gems[pos.x, pos.y] == null)
                return;
            _gems[pos.x, pos.y] = null;
            OnGemRemoved?.Invoke(pos);
        }

        /// <summary>
        /// Returns true if position has no gem.
        /// </summary>
        public bool IsEmpty(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return false;
            return _gems[pos.x, pos.y] == null;
        }

        /// <summary>
        /// Returns true if position is within board bounds.
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width &&
                   pos.y >= 0 && pos.y < _height;
        }

        /// <summary>
        /// Moves gem from one position to another.
        /// Does NOT fire events (use for swaps, falls).
        /// </summary>
        public void MoveGem(Vector2Int from, Vector2Int to)
        {
            if (!IsValidPosition(from) || !IsValidPosition(to))
                return;
            var gem = _gems[from.x, from.y];
            if (gem == null)
                return;
            _gems[from.x, from.y] = null;
            _gems[to.x, to.y] = gem.Value.WithPosition(to);
        }

        /// <summary>
        /// Swaps gems at two positions.
        /// Does NOT fire events.
        /// </summary>
        public void SwapGems(Vector2Int a, Vector2Int b)
        {
            if (!IsValidPosition(a) || !IsValidPosition(b))
                return;
            var gemA = _gems[a.x, a.y];
            var gemB = _gems[b.x, b.y];

            _gems[a.x, a.y] = gemB?.WithPosition(a);
            _gems[b.x, b.y] = gemA?.WithPosition(b);
        }

        /// <summary>
        /// Returns gem type at position or null.
        /// </summary>
        public GemType? GetGemType(Vector2Int pos)
        {
            var gem = GetGem(pos);
            return gem?.Type;
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| Width | `int` | Board width |
| Height | `int` | Board height |
| Size | `Vector2Int` | Board dimensions |
| OnGemAdded | `event Action<Vector2Int, GemData>` | Fires when gem added |
| OnGemRemoved | `event Action<Vector2Int>` | Fires when gem removed |
| GetGem | `GemData? GetGem(Vector2Int pos)` | Get gem at position |
| SetGem | `void SetGem(Vector2Int pos, GemData gem)` | Set gem (fires event) |
| RemoveGem | `void RemoveGem(Vector2Int pos)` | Remove gem (fires event) |
| IsEmpty | `bool IsEmpty(Vector2Int pos)` | Check if cell is empty |
| IsValidPosition | `bool IsValidPosition(Vector2Int pos)` | Check bounds |
| MoveGem | `void MoveGem(Vector2Int from, Vector2Int to)` | Move without events |
| SwapGems | `void SwapGems(Vector2Int a, Vector2Int b)` | Swap without events |
| GetGemType | `GemType? GetGemType(Vector2Int pos)` | Get type only |

### Notes
- `GemData?` — nullable struct для пустых ячеек
- MoveGem/SwapGems не вызывают события (BoardView анимирует вручную)
- SetGem/RemoveGem вызывают события (для initial fill и destroy)

---

## Component 2: SpawnSystem

**File:** `Assets/Scripts/Spawn/SpawnSystem.cs`

**Type:** Plain C# class (не MonoBehaviour)

**Responsibility:** Генерация типа gem-а с анти-матч логикой (не создает 3+ в линию при спауне).

### Code

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Gem;

namespace Match3.Spawn
{
    public class SpawnSystem
    {
        private readonly GemConfig _config;

        public SpawnSystem(GemConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates gem type that won't create match at position.
        /// </summary>
        public GemType GenerateType(Vector2Int pos, BoardData board)
        {
            var forbidden = GetForbiddenTypes(pos, board);
            return GetRandomTypeExcluding(forbidden);
        }

        /// <summary>
        /// Returns types that would create a match at position.
        /// </summary>
        private HashSet<GemType> GetForbiddenTypes(Vector2Int pos, BoardData board)
        {
            var forbidden = new HashSet<GemType>();

            // Check horizontal (2 gems to the left)
            var leftType = CheckConsecutive(pos, Vector2Int.left, board, 2);
            if (leftType.HasValue)
                forbidden.Add(leftType.Value);

            // Check vertical (2 gems below)
            var belowType = CheckConsecutive(pos, Vector2Int.down, board, 2);
            if (belowType.HasValue)
                forbidden.Add(belowType.Value);

            return forbidden;
        }

        /// <summary>
        /// Checks if there are N consecutive gems of same type in direction.
        /// Returns that type if found, null otherwise.
        /// </summary>
        private GemType? CheckConsecutive(Vector2Int pos, Vector2Int direction, BoardData board, int count)
        {
            GemType? matchType = null;
            int matches = 0;

            for (int i = 1; i <= count; i++)
            {
                var checkPos = pos + direction * i;
                var type = board.GetGemType(checkPos);

                if (!type.HasValue)
                    return null;

                if (matchType == null)
                {
                    matchType = type.Value;
                    matches = 1;
                }
                else if (type.Value == matchType.Value)
                {
                    matches++;
                }
                else
                {
                    return null;
                }
            }

            return matches >= count ? matchType : null;
        }

        /// <summary>
        /// Gets random type excluding forbidden ones.
        /// Falls back to any random type if all forbidden (edge case).
        /// </summary>
        private GemType GetRandomTypeExcluding(HashSet<GemType> forbidden)
        {
            var allTypes = _config.GetAllTypes();

            // Build list of allowed types
            var allowed = new List<GemType>();
            foreach (var type in allTypes)
            {
                if (!forbidden.Contains(type))
                    allowed.Add(type);
            }

            // Fallback if somehow all types are forbidden
            if (allowed.Count == 0)
                return _config.GetRandomType();

            int index = Random.Range(0, allowed.Count);
            return allowed[index];
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| Constructor | `SpawnSystem(GemConfig config)` | Creates system with config |
| GenerateType | `GemType GenerateType(Vector2Int pos, BoardData board)` | Generate type without match |

### Anti-Match Logic

При заполнении слева направо, снизу вверх:
- Проверяем 2 gem-а слева: если одинаковые, запрещаем этот тип
- Проверяем 2 gem-а снизу: если одинаковые, запрещаем этот тип
- Выбираем случайный из разрешенных типов

```
Example:
Position (2, 0) - checking before placing

  [?]  <- placing here
[R][R] <- 2 Red gems to the left

Red is forbidden, choose from [Blue, Green, Yellow, Purple, Orange]
```

---

## Component 3: BoardView

**File:** `Assets/Scripts/Board/BoardView.cs`

**Type:** MonoBehaviour

**Responsibility:** Создание/удаление GemView. Хранит ссылки на view-объекты. Слушает BoardData события.

### Code

```csharp
using System;
using UnityEngine;
using Match3.Gem;
using Match3.Spawn;

namespace Match3.Board
{
    public class BoardView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private GemConfig _gemConfig;
        [SerializeField] private GemView _gemPrefab;

        [Header("Parents")]
        [SerializeField] private Transform _gemsParent;

        private BoardData _boardData;
        private SpawnSystem _spawnSystem;
        private GemView[,] _views;
        private GridData _gridData;

        public BoardData Data => _boardData;

        /// <summary>
        /// Fires when initial board fill is complete.
        /// </summary>
        public event Action OnBoardReady;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            FillBoard();
        }

        private void OnEnable()
        {
            if (_boardData != null)
            {
                _boardData.OnGemAdded += HandleGemAdded;
                _boardData.OnGemRemoved += HandleGemRemoved;
            }
        }

        private void OnDisable()
        {
            if (_boardData != null)
            {
                _boardData.OnGemAdded -= HandleGemAdded;
                _boardData.OnGemRemoved -= HandleGemRemoved;
            }
        }

        private void Initialize()
        {
            _gridData = _gridView.Data;
            var size = _gridData.Size;

            _boardData = new BoardData(size);
            _spawnSystem = new SpawnSystem(_gemConfig);
            _views = new GemView[size.x, size.y];

            // Subscribe after creating BoardData
            _boardData.OnGemAdded += HandleGemAdded;
            _boardData.OnGemRemoved += HandleGemRemoved;
        }

        /// <summary>
        /// Fills entire board with gems (initial fill).
        /// </summary>
        private void FillBoard()
        {
            var size = _gridData.Size;

            // Fill from bottom-left to top-right
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var pos = new Vector2Int(x, y);
                    SpawnGemAt(pos);
                }
            }

            OnBoardReady?.Invoke();
        }

        /// <summary>
        /// Spawns gem at position using SpawnSystem for type.
        /// </summary>
        public void SpawnGemAt(Vector2Int pos)
        {
            var type = _spawnSystem.GenerateType(pos, _boardData);
            var gem = new GemData(type, pos);
            _boardData.SetGem(pos, gem);
        }

        /// <summary>
        /// Returns GemView at position or null.
        /// </summary>
        public GemView GetView(Vector2Int pos)
        {
            if (!_boardData.IsValidPosition(pos))
                return null;
            return _views[pos.x, pos.y];
        }

        /// <summary>
        /// Creates GemView at position (called manually, not via event).
        /// Used for fall system when spawning from above.
        /// </summary>
        public void CreateGem(Vector2Int pos, GemData gem)
        {
            var worldPos = _gridData.GridToWorld(pos);
            CreateGemView(pos, gem, worldPos);
        }

        /// <summary>
        /// Creates GemView at spawn position above grid, then BoardView tracks it.
        /// Used for fall system.
        /// </summary>
        public GemView CreateGemAbove(int column, int rowsAbove, GemData gem)
        {
            var spawnPos = _gridData.GetSpawnPosition(column, rowsAbove);
            var view = InstantiateGemView(gem, spawnPos);
            // Note: Don't add to _views array - will be added when gem lands
            return view;
        }

        /// <summary>
        /// Destroys GemView at position (called manually, not via event).
        /// </summary>
        public void DestroyGem(Vector2Int pos)
        {
            var view = GetView(pos);
            if (view != null)
            {
                _views[pos.x, pos.y] = null;
                Destroy(view.gameObject);
            }
        }

        /// <summary>
        /// Updates view reference when gem moves to new position.
        /// </summary>
        public void UpdateViewPosition(Vector2Int from, Vector2Int to)
        {
            var view = _views[from.x, from.y];
            if (view == null)
                return;

            _views[from.x, from.y] = null;
            _views[to.x, to.y] = view;
            view.SetGridPosition(to);
        }

        /// <summary>
        /// Registers view at position (for gems spawned above grid).
        /// </summary>
        public void RegisterView(Vector2Int pos, GemView view)
        {
            if (_boardData.IsValidPosition(pos))
            {
                _views[pos.x, pos.y] = view;
                view.SetGridPosition(pos);
            }
        }

        // --- Event Handlers ---

        private void HandleGemAdded(Vector2Int pos, GemData gem)
        {
            var worldPos = _gridData.GridToWorld(pos);
            CreateGemView(pos, gem, worldPos);
        }

        private void HandleGemRemoved(Vector2Int pos)
        {
            DestroyGem(pos);
        }

        // --- Private Helpers ---

        private void CreateGemView(Vector2Int pos, GemData gem, Vector3 worldPos)
        {
            var view = InstantiateGemView(gem, worldPos);
            view.SetGridPosition(pos);
            _views[pos.x, pos.y] = view;
        }

        private GemView InstantiateGemView(GemData gem, Vector3 worldPos)
        {
            Transform parent = _gemsParent != null ? _gemsParent : transform;
            var view = Instantiate(_gemPrefab, worldPos, Quaternion.identity, parent);
            view.Setup(gem.Type, _gemConfig);
            view.name = $"Gem_{gem.Type}_{gem.Position.x}_{gem.Position.y}";
            return view;
        }
    }
}
```

### Dependencies (SerializeField)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| _gridView | GridView | Yes | Reference to grid for coordinate conversion |
| _gemConfig | GemConfig | Yes | Configuration for gem sprites |
| _gemPrefab | GemView | Yes | Prefab to instantiate |
| _gemsParent | Transform | No | Parent for gem objects (defaults to self) |

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| Data | `BoardData` | Access to board data |
| OnBoardReady | `event Action` | Fires when initial fill complete |
| SpawnGemAt | `void SpawnGemAt(Vector2Int pos)` | Spawn gem with anti-match |
| GetView | `GemView GetView(Vector2Int pos)` | Get view at position |
| CreateGem | `void CreateGem(Vector2Int pos, GemData gem)` | Create view at position |
| CreateGemAbove | `GemView CreateGemAbove(int col, int rows, GemData gem)` | Create above grid (for fall) |
| DestroyGem | `void DestroyGem(Vector2Int pos)` | Destroy view at position |
| UpdateViewPosition | `void UpdateViewPosition(Vector2Int from, Vector2Int to)` | Update view tracking |
| RegisterView | `void RegisterView(Vector2Int pos, GemView view)` | Register view after fall |

---

## Namespace Structure

Добавить namespace во все файлы:

```csharp
// BoardData.cs, BoardView.cs
namespace Match3.Board { ... }

// SpawnSystem.cs
namespace Match3.Spawn { ... }
```

Обновить Step 1 и Step 2 файлы с namespace (если еще нет):
```csharp
// GridData.cs, GridView.cs, GridConfig.cs
namespace Match3.Grid { ... }

// GemType.cs, GemData.cs, GemView.cs, GemConfig.cs, GemTypeData.cs
namespace Match3.Gem { ... }
```

---

## Scene Setup

### Hierarchy

```
Scene
└── Grid
    ├── GridView (component)
    └── Cells (parent for cell visuals)
└── Board
    ├── BoardView (component)
    └── Gems (parent for gem objects)
```

### Inspector Setup for BoardView

1. Create empty GameObject "Board" in scene
2. Add `BoardView` component
3. Create child "Gems" (empty) for gem parent
4. Assign references:
   - _gridView: drag GridView from scene
   - _gemConfig: drag GemConfig.asset
   - _gemPrefab: drag Gem.prefab
   - _gemsParent: drag Gems child object

---

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                          INITIAL FILL                             │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  BoardView.Start()                                                │
│       │                                                           │
│       ▼                                                           │
│  FillBoard() ──► for each (x,y) from (0,0) to (7,7)              │
│       │                                                           │
│       ▼                                                           │
│  SpawnGemAt(pos)                                                  │
│       │                                                           │
│       ├──► SpawnSystem.GenerateType(pos, board)                  │
│       │         │                                                 │
│       │         ├──► Check 2 left: forbidden if same type        │
│       │         ├──► Check 2 below: forbidden if same type       │
│       │         └──► Return random from allowed types            │
│       │                                                           │
│       └──► BoardData.SetGem(pos, gem)                            │
│                 │                                                 │
│                 └──► OnGemAdded event ──► HandleGemAdded()       │
│                                                  │                │
│                                                  ▼                │
│                                     CreateGemView(pos, gem)       │
│                                           │                       │
│                                           ├──► Instantiate prefab │
│                                           ├──► view.Setup()       │
│                                           └──► Store in _views[,] │
│                                                                   │
│  OnBoardReady event                                               │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Integration with Future Steps

### Step 4 (Fall System) will use:

```csharp
// After gems destroyed, fall system:
// 1. Calculate falls
var falls = fallSystem.CalculateFalls(boardView.Data);

// 2. Apply to data (no events)
foreach (var fall in falls)
{
    boardView.Data.MoveGem(fall.from, fall.to);
    boardView.UpdateViewPosition(fall.from, fall.to);
    // Animate view separately
}

// 3. Spawn new gems above
for (int col = 0; col < width; col++)
{
    int emptyCount = CountEmptyInColumn(col);
    for (int i = 0; i < emptyCount; i++)
    {
        var type = spawnSystem.GenerateType(...);
        var gem = new GemData(type, ...);
        var view = boardView.CreateGemAbove(col, i + 1, gem);
        // Animate fall, then:
        boardView.RegisterView(targetPos, view);
        boardView.Data.SetGem(targetPos, gem); // No event needed, already have view
    }
}
```

### Step 5 (Swap System) will use:

```csharp
// Swap gems in data
boardView.Data.SwapGems(posA, posB);

// Animate views
var viewA = boardView.GetView(posA);
var viewB = boardView.GetView(posB);
swapAnimator.AnimateSwap(viewA, viewB);

// Update view tracking
boardView.UpdateViewPosition(posA, posB);
boardView.UpdateViewPosition(posB, posA);
```

### Step 7 (Destroy System) will use:

```csharp
// Remove from data (fires events, auto-destroys views)
foreach (var pos in matchedPositions)
{
    boardView.Data.RemoveGem(pos);
}
```

---

## Testing Checklist

### BoardData Tests
- [ ] GetGem returns null for empty position
- [ ] GetGem returns null for invalid position
- [ ] SetGem fires OnGemAdded event
- [ ] RemoveGem fires OnGemRemoved event
- [ ] IsEmpty returns true for empty, false for filled
- [ ] MoveGem does NOT fire events
- [ ] SwapGems does NOT fire events

### SpawnSystem Tests
- [ ] GenerateType never returns type that makes horizontal match
- [ ] GenerateType never returns type that makes vertical match
- [ ] Works at board edges (0,0), (7,7)
- [ ] Works with only 2 gem types (edge case)

### BoardView Tests
- [ ] Initial fill creates 64 gems (8x8)
- [ ] No initial matches exist after fill
- [ ] GetView returns correct view
- [ ] DestroyGem removes from scene
- [ ] OnBoardReady fires after fill

### Visual Tests
- [ ] All gems visible on screen
- [ ] Gems positioned correctly in grid
- [ ] No overlapping gems
- [ ] Correct sprites for types

---

## File Checklist

- [ ] Create `Assets/Scripts/Board/` folder
- [ ] Create `Assets/Scripts/Spawn/` folder
- [ ] Implement `BoardData.cs`
- [ ] Implement `SpawnSystem.cs`
- [ ] Implement `BoardView.cs`
- [ ] Add namespaces to Step 1 files (if needed)
- [ ] Add namespaces to Step 2 files (if needed)
- [ ] Setup scene hierarchy
- [ ] Assign all references in BoardView
- [ ] Play and verify 64 gems spawn without matches

---

## Notes

- BoardData is pure C# for testability
- SpawnSystem is pure C# for testability
- BoardView is the only MonoBehaviour, manages lifecycle
- Events used for initial fill; direct calls for animations (fall, swap)
- GemView stores GridPosition for easy lookup
- Filling order (bottom-left to top-right) is important for anti-match logic
