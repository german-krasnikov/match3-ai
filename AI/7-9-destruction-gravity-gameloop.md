# Phase 7-9: Destruction, Gravity, Game Loop — Реализация

## Обзор

Финальные системы Match3: уничтожение элементов, гравитация с волновой анимацией, стейт-машина игрового цикла.

```
Assets/Scripts/
├── Destruction/
│   └── DestructionController.cs    # Уничтожение + возврат в пул
├── Gravity/
│   ├── FallData.cs                 # Данные о падении
│   ├── GravityCalculator.cs        # Расчёт падений (Pure C#)
│   └── GravityController.cs        # Анимация + спаун
└── GameLoop/
    ├── GameState.cs                # Enum состояний
    ├── DeadlockChecker.cs          # Проверка тупика (Pure C#)
    └── GameStateMachine.cs         # Оркестратор
```

**Принятые решения:**
- VFX при уничтожении: нет (scale to zero уже в ElementView)
- Scoring: нет (добавим позже)
- Deadlock: Game Over (без перемешивания)
- Gravity: волной сверху вниз
- Каскады: бесконечно пока есть матчи

---

## Phase 7: Destruction System

### Архитектура

```
MatchController.OnMatchesFound
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                  DestructionController                       │
│                    (MonoBehaviour)                           │
│                                                              │
│  DestroyMatches(matches)                                     │
│    1. Собрать уникальные позиции                            │
│    2. Получить элементы из GridData                         │
│    3. Удалить из GridData (логика)                          │
│    4. Запустить анимации параллельно                        │
│    5. По завершении → вернуть в пул                         │
│    6. Fire OnDestructionComplete                            │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
    GravityController
```

### 7.1 DestructionController.cs

**Файл:** `Assets/Scripts/Destruction/DestructionController.cs`

```csharp
using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Destruction
{
    public class DestructionController : MonoBehaviour
    {
        public event Action<HashSet<GridPosition>> OnDestructionComplete;

        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private readonly HashSet<GridPosition> _positionsToDestroy = new();
        private readonly List<IElement> _elementsToDestroy = new();
        private int _pendingAnimations;

        public void Initialize(GridData grid)
        {
            _grid = grid;
        }

        public void DestroyMatches(List<MatchData> matches)
        {
            CollectUniquePositions(matches);
            if (_positionsToDestroy.Count == 0)
            {
                OnDestructionComplete?.Invoke(_positionsToDestroy);
                return;
            }

            CollectElements();
            RemoveFromGrid();
            PlayDestroyAnimations();
        }

        private void CollectUniquePositions(List<MatchData> matches)
        {
            _positionsToDestroy.Clear();
            foreach (var match in matches)
            {
                foreach (var pos in match.Positions)
                    _positionsToDestroy.Add(pos);
            }
        }

        private void CollectElements()
        {
            _elementsToDestroy.Clear();
            foreach (var pos in _positionsToDestroy)
            {
                var element = _grid.GetElement(pos);
                if (element != null)
                    _elementsToDestroy.Add(element);
            }
        }

        private void RemoveFromGrid()
        {
            foreach (var pos in _positionsToDestroy)
                _grid.RemoveElement(pos);
        }

        private void PlayDestroyAnimations()
        {
            _pendingAnimations = _elementsToDestroy.Count;

            foreach (var element in _elementsToDestroy)
            {
                element.PlayDestroyAnimation(() =>
                {
                    _factory.ReturnElement(element);
                    _pendingAnimations--;

                    if (_pendingAnimations <= 0)
                        OnDestructionComplete?.Invoke(_positionsToDestroy);
                });
            }
        }
    }
}
```

**API:**
```csharp
event Action<HashSet<GridPosition>> OnDestructionComplete;  // Позиции для gravity

void Initialize(GridData grid);
void DestroyMatches(List<MatchData> matches);
```

**Почему HashSet в событии:**
- GravityController нужны колонки с пустотами
- HashSet даёт уникальные позиции без дубликатов
- O(1) проверка `Contains`

---

## Phase 8: Gravity System

### Архитектура

```
DestructionController.OnDestructionComplete(destroyedPositions)
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                   GravityController                          │
│                    (MonoBehaviour)                           │
│                                                              │
│  ApplyGravity(destroyedPositions)                           │
│    1. GravityCalculator.CalculateFalls() → List<FallData>   │
│    2. Сортировка по Y (сверху вниз) для волны               │
│    3. Анимация падений с задержкой                          │
│    4. Спаун новых элементов сверху                          │
│    5. Fire OnGravityComplete(affectedPositions)             │
└────────────┬────────────────────────────────────────────────┘
             │ использует
             ▼
┌─────────────────────────────────────────────────────────────┐
│                  GravityCalculator                           │
│                     (Pure C#)                                │
│                                                              │
│  CalculateFalls(grid, columns) → List<FallData>             │
│    • Для каждой колонки снизу вверх                         │
│    • Находит пустоты, сдвигает элементы вниз                │
│  CountEmptyInColumn(grid, column) → int                     │
└─────────────────────────────────────────────────────────────┘
```

### 8.1 FallData.cs

**Файл:** `Assets/Scripts/Gravity/FallData.cs`

```csharp
using Match3.Core;
using Match3.Elements;

namespace Match3.Gravity
{
    public readonly struct FallData
    {
        public IElement Element { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public int Distance { get; }

        public FallData(IElement element, GridPosition from, GridPosition to)
        {
            Element = element;
            From = from;
            To = to;
            Distance = from.Y - to.Y;
        }
    }
}
```

---

### 8.2 GravityCalculator.cs

**Файл:** `Assets/Scripts/Gravity/GravityCalculator.cs`

```csharp
using System.Collections.Generic;
using Match3.Core;
using Match3.Grid;

namespace Match3.Gravity
{
    public class GravityCalculator
    {
        private readonly List<FallData> _falls = new();

        public List<FallData> CalculateFalls(GridData grid, IEnumerable<int> affectedColumns)
        {
            _falls.Clear();

            foreach (int column in affectedColumns)
                ProcessColumn(grid, column);

            return _falls;
        }

        private void ProcessColumn(GridData grid, int column)
        {
            int writeY = 0;

            for (int readY = 0; readY < grid.Height; readY++)
            {
                var pos = new GridPosition(column, readY);
                var element = grid.GetElement(pos);

                if (element == null) continue;

                if (readY != writeY)
                {
                    var newPos = new GridPosition(column, writeY);
                    _falls.Add(new FallData(element, pos, newPos));
                }

                writeY++;
            }
        }

        public int CountEmptyInColumn(GridData grid, int column)
        {
            int empty = 0;
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.GetElement(new GridPosition(column, y)) == null)
                    empty++;
            }
            return empty;
        }
    }
}
```

**Алгоритм ProcessColumn:**

```
Исходная колонка:     После расчёта:
   y=4  [A]              y=4  [_] ←── spawn
   y=3  [_]              y=3  [_] ←── spawn
   y=2  [B]              y=2  [A] (was y=4, distance=2)
   y=1  [_]              y=1  [B] (was y=2, distance=1)
   y=0  [C]              y=0  [C] (no move)

writeY pointer движется только когда встречает элемент.
Элементы "проваливаются" на позицию writeY.
```

---

### 8.3 GravityController.cs

**Файл:** `Assets/Scripts/Gravity/GravityController.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using Match3.Spawn;
using UnityEngine;

namespace Match3.Gravity
{
    public class GravityController : MonoBehaviour
    {
        public event Action<List<GridPosition>> OnGravityComplete;

        [SerializeField] private GridView _gridView;
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private GridConfig _config;
        private GridPositionConverter _converter;
        private GravityCalculator _calculator;

        private readonly List<GridPosition> _affectedPositions = new();
        private readonly HashSet<int> _affectedColumns = new();
        private int _pendingAnimations;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _config = _gridView.Config;
            _converter = _gridView.PositionConverter;
            _calculator = new GravityCalculator();
        }

        public void ApplyGravity(HashSet<GridPosition> destroyedPositions)
        {
            CollectAffectedColumns(destroyedPositions);

            var falls = _calculator.CalculateFalls(_grid, _affectedColumns);

            _affectedPositions.Clear();

            if (falls.Count == 0 && !HasEmptySpaces())
            {
                OnGravityComplete?.Invoke(_affectedPositions);
                return;
            }

            UpdateGridData(falls);
            var newElements = SpawnNewElements();

            int totalAnimations = falls.Count + newElements.Count;
            if (totalAnimations == 0)
            {
                OnGravityComplete?.Invoke(_affectedPositions);
                return;
            }

            _pendingAnimations = totalAnimations;
            AnimateFallsWave(falls);
            AnimateNewElementsWave(newElements);
        }

        private void CollectAffectedColumns(HashSet<GridPosition> positions)
        {
            _affectedColumns.Clear();
            foreach (var pos in positions)
                _affectedColumns.Add(pos.X);
        }

        private bool HasEmptySpaces()
        {
            foreach (int column in _affectedColumns)
            {
                if (_calculator.CountEmptyInColumn(_grid, column) > 0)
                    return true;
            }
            return false;
        }

        private void UpdateGridData(List<FallData> falls)
        {
            foreach (var fall in falls)
            {
                _grid.RemoveElement(fall.From);
            }

            foreach (var fall in falls)
            {
                fall.Element.Position = fall.To;
                _grid.SetElement(fall.To, fall.Element);
                _affectedPositions.Add(fall.To);
            }
        }

        private List<(IElement element, GridPosition target, int spawnOffset)> SpawnNewElements()
        {
            var newElements = new List<(IElement, GridPosition, int)>();

            foreach (int column in _affectedColumns)
            {
                int emptyCount = _calculator.CountEmptyInColumn(_grid, column);

                for (int i = 0; i < emptyCount; i++)
                {
                    int targetY = _config.Height - emptyCount + i;
                    var targetPos = new GridPosition(column, targetY);

                    int spawnOffset = emptyCount - i;
                    var spawnWorldPos = _converter.GridToWorld(
                        new GridPosition(column, _config.Height - 1 + spawnOffset)
                    );

                    var element = _spawnController.SpawnAtTop(column, spawnOffset);
                    element.Position = targetPos;
                    _grid.SetElement(targetPos, element);

                    newElements.Add((element, targetPos, spawnOffset));
                    _affectedPositions.Add(targetPos);
                }
            }

            return newElements;
        }

        private void AnimateFallsWave(List<FallData> falls)
        {
            var sorted = falls.OrderByDescending(f => f.From.Y).ToList();

            float waveDelay = 0.03f;

            for (int i = 0; i < sorted.Count; i++)
            {
                var fall = sorted[i];
                float delay = i * waveDelay;
                float duration = fall.Distance / _config.FallSpeed;
                var targetWorld = _converter.GridToWorld(fall.To);

                StartCoroutine(AnimateWithDelay(fall.Element, targetWorld, duration, delay));
            }
        }

        private void AnimateNewElementsWave(List<(IElement element, GridPosition target, int spawnOffset)> newElements)
        {
            var sorted = newElements.OrderByDescending(e => e.spawnOffset).ToList();

            float waveDelay = 0.03f;
            float baseDelay = 0.1f;

            for (int i = 0; i < sorted.Count; i++)
            {
                var (element, target, spawnOffset) = sorted[i];
                float delay = baseDelay + i * waveDelay;
                float duration = spawnOffset / _config.FallSpeed;
                var targetWorld = _converter.GridToWorld(target);

                StartCoroutine(AnimateWithDelay(element, targetWorld, duration, delay));
            }
        }

        private System.Collections.IEnumerator AnimateWithDelay(
            IElement element, Vector3 target, float duration, float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            element.MoveTo(target, duration, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            _pendingAnimations--;
            if (_pendingAnimations <= 0)
                OnGravityComplete?.Invoke(_affectedPositions);
        }
    }
}
```

**Волновая анимация:**

```
Время:  0ms   30ms   60ms   90ms
        ↓     ↓      ↓      ↓
y=4     ●────────────────────→ падает первым
y=3           ●──────────────→ падает вторым
y=2                  ●───────→ падает третьим
y=1                         ●→ падает последним

● = старт анимации
→ = движение вниз

Элементы сверху начинают падать раньше, создавая эффект волны.
Новые элементы появляются с дополнительной задержкой (baseDelay).
```

**API:**
```csharp
event Action<List<GridPosition>> OnGravityComplete;  // Позиции для проверки матчей

void Initialize(GridData grid);
void ApplyGravity(HashSet<GridPosition> destroyedPositions);
```

---

## Phase 9: Game Loop

### Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    GameStateMachine                          │
│                     (MonoBehaviour)                          │
│                                                              │
│  Состояния:                                                  │
│    INITIALIZING → IDLE ←───────────────────┐                │
│         ↓          ↓                        │                │
│         └──→ SWAPPING                       │                │
│                  ↓                          │                │
│              MATCHING ──→ no matches ───────┤                │
│                  ↓                          │                │
│              DESTROYING                     │                │
│                  ↓                          │                │
│              FALLING                        │                │
│                  ↓                          │                │
│              MATCHING ──→ has matches ──────┘ (каскад)      │
│                  ↓                          │                │
│              CHECKING ──→ has moves ────────┤                │
│                  ↓                          │                │
│              GAME_OVER                      │                │
└─────────────────────────────────────────────────────────────┘
```

### 9.1 GameState.cs

**Файл:** `Assets/Scripts/GameLoop/GameState.cs`

```csharp
namespace Match3.GameLoop
{
    public enum GameState
    {
        Initializing,
        Idle,
        Swapping,
        Matching,
        Destroying,
        Falling,
        Checking,
        GameOver
    }
}
```

---

### 9.2 DeadlockChecker.cs

**Файл:** `Assets/Scripts/GameLoop/DeadlockChecker.cs`

```csharp
using Match3.Core;
using Match3.Grid;
using Match3.Match;

namespace Match3.GameLoop
{
    public class DeadlockChecker
    {
        private readonly IMatchFinder _matchFinder;

        public DeadlockChecker(IMatchFinder matchFinder)
        {
            _matchFinder = matchFinder;
        }

        public bool HasPossibleMoves(GridData grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new GridPosition(x, y);

                    if (CheckSwap(grid, pos, GridPosition.Right)) return true;
                    if (CheckSwap(grid, pos, GridPosition.Up)) return true;
                }
            }
            return false;
        }

        private bool CheckSwap(GridData grid, GridPosition pos, GridPosition direction)
        {
            var neighbor = pos + direction;
            if (!grid.IsValidPosition(neighbor)) return false;

            var elementA = grid.GetElement(pos);
            var elementB = grid.GetElement(neighbor);
            if (elementA == null || elementB == null) return false;
            if (elementA.Type == elementB.Type) return false;

            grid.SwapElements(pos, neighbor);
            var matches = _matchFinder.FindMatchesAt(grid, new[] { pos, neighbor });
            grid.SwapElements(pos, neighbor);

            return matches.Count > 0;
        }
    }
}
```

**Оптимизация:**
- Проверяем только Right и Up (избегаем дублирования Left/Down)
- Пропускаем одинаковые типы (свап не изменит ситуацию)
- Early return при первом найденном ходе

**Сложность:** O(W × H) в худшем случае

---

### 9.3 GameStateMachine.cs

**Файл:** `Assets/Scripts/GameLoop/GameStateMachine.cs`

```csharp
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
        private List<GridPosition> _lastAffectedPositions;

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

        // === Event Handlers ===

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
            _lastAffectedPositions = affectedPositions;
            SetState(GameState.Matching);

            if (affectedPositions.Count > 0)
                _matchController.CheckAt(affectedPositions);
            else
                _matchController.CheckAll();
        }

        private void CheckForDeadlock()
        {
            if (_deadlockChecker.HasPossibleMoves(_grid))
            {
                SetState(GameState.Idle);
            }
            else
            {
                SetState(GameState.GameOver);
            }
        }
    }
}
```

---

### 9.4 Обновлённый GameBootstrap.cs

**Файл:** `Assets/Scripts/Game/GameBootstrap.cs`

```csharp
using Match3.Destruction;
using Match3.GameLoop;
using Match3.Gravity;
using Match3.Grid;
using Match3.Match;
using Match3.Spawn;
using Match3.Swap;
using UnityEngine;

namespace Match3.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private GridView _gridView;

        [Header("Controllers")]
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private MatchController _matchController;
        [SerializeField] private SwapController _swapController;
        [SerializeField] private DestructionController _destructionController;
        [SerializeField] private GravityController _gravityController;
        [SerializeField] private GameStateMachine _stateMachine;

        private GridData _gridData;

        private void Start()
        {
            InitializeGrid();
            InitializeControllers();
            StartGame();
        }

        private void InitializeGrid()
        {
            var config = _gridView.Config;
            _gridData = new GridData(config.Width, config.Height);
            _gridView.CreateVisualGrid();
        }

        private void InitializeControllers()
        {
            _spawnController.Initialize(_gridData);
            _matchController.Initialize(_gridData);
            _swapController.Initialize(_gridData);
            _destructionController.Initialize(_gridData);
            _gravityController.Initialize(_gridData);
            _stateMachine.Initialize(_gridData);

            _stateMachine.OnGameOver += OnGameOver;
        }

        private void StartGame()
        {
            _spawnController.FillGrid();
            Debug.Log("[Match3] Game started!");
        }

        private void OnGameOver()
        {
            Debug.Log("[Match3] GAME OVER - No possible moves!");
            // TODO: показать UI Game Over
        }
    }
}
```

---

## Scene Setup

### Иерархия объектов:

```
Scene
├── Main Camera              [Camera]
├── Grid                     [GridView]
│   └── Cells
├── ElementPool              [ElementPool]
├── Elements                 (parent)
├── ElementFactory           [ElementFactory]
├── Controllers
│   ├── SpawnController      [SpawnController]
│   ├── MatchController      [MatchController]
│   ├── SwipeInputHandler    [SwipeInputHandler]
│   ├── SwapAnimator         [SwapAnimator]
│   ├── SwapController       [SwapController]
│   ├── DestructionController [DestructionController]  ← NEW
│   ├── GravityController    [GravityController]       ← NEW
│   └── GameStateMachine     [GameStateMachine]        ← NEW
└── GameBootstrap            [GameBootstrap]
```

### Связи в Inspector:

**DestructionController:**
- `_factory` → ElementFactory

**GravityController:**
- `_gridView` → Grid
- `_spawnController` → SpawnController
- `_factory` → ElementFactory

**GameStateMachine:**
- `_swapController` → SwapController
- `_matchController` → MatchController
- `_destructionController` → DestructionController
- `_gravityController` → GravityController

**GameBootstrap:**
- Все контроллеры

---

## Полный Game Loop Flow

```
START
  │
  ▼
[INITIALIZING]
  │ SpawnController.FillGrid()
  ▼
[IDLE] ◄───────────────────────────────────────┐
  │ Ожидание свайпа                            │
  │ Input enabled                              │
  ▼                                            │
[SWAPPING]                                     │
  │ SwapController анимирует                   │
  │                                            │
  ├── Invalid swap ────────────────────────────┤
  │   (откат анимация)                         │
  │                                            │
  ▼ Valid swap                                 │
[MATCHING]                                     │
  │ MatchController.CheckAt()                  │
  │                                            │
  ├── No matches ──► [CHECKING] ───────────────┤
  │                      │                     │
  │                      ├── Has moves ────────┤
  │                      │                     │
  │                      ▼                     │
  │                 [GAME_OVER]                │
  │                                            │
  ▼ Has matches                                │
[DESTROYING]                                   │
  │ DestructionController                      │
  │ Параллельные анимации scale→0             │
  │ Возврат в пул                             │
  ▼                                            │
[FALLING]                                      │
  │ GravityController                          │
  │ Волновая анимация падений                  │
  │ Спаун новых элементов сверху              │
  ▼                                            │
[MATCHING] ─── Has matches ──► [DESTROYING]    │
  │                            (каскад)        │
  │                                            │
  └── No matches ──► [CHECKING] ───────────────┘
```

---

## Интеграция с существующим кодом

### Изменения в существующих файлах:

**SpawnController.cs** — добавить метод:
```csharp
public IElement SpawnAtTop(int column, int offsetAboveGrid = 1)
{
    // Уже реализован в Phase 4
}
```

**SwapController.cs** — убрать `EnableInput()` из OnSwapComplete:
```csharp
// Было:
private void OnSwapComplete(...)
{
    _swapController.EnableInput();  // УДАЛИТЬ
}

// Теперь GameStateMachine управляет input
```

### Удалить из GameBootstrap:

```csharp
// Удалить эти строки — теперь это делает GameStateMachine:
_swapController.OnSwapComplete += OnSwapComplete;
private void OnSwapComplete(...) { ... }
```

---

## Checklist

### Phase 7: Destruction
- [ ] Создать `Assets/Scripts/Destruction/`
- [ ] Реализовать `DestructionController.cs`
- [ ] Добавить в сцену, связать с ElementFactory

### Phase 8: Gravity
- [ ] Создать `Assets/Scripts/Gravity/`
- [ ] Реализовать `FallData.cs`
- [ ] Реализовать `GravityCalculator.cs`
- [ ] Реализовать `GravityController.cs`
- [ ] Добавить в сцену, связать зависимости

### Phase 9: Game Loop
- [ ] Создать `Assets/Scripts/GameLoop/`
- [ ] Реализовать `GameState.cs`
- [ ] Реализовать `DeadlockChecker.cs`
- [ ] Реализовать `GameStateMachine.cs`
- [ ] Обновить `GameBootstrap.cs`
- [ ] Добавить в сцену, связать все контроллеры

### Тестирование
- [ ] Тест: свап создаёт матч → элементы исчезают
- [ ] Тест: после исчезновения элементы падают волной
- [ ] Тест: новые элементы появляются сверху
- [ ] Тест: каскадные матчи работают
- [ ] Тест: при отсутствии ходов — Game Over
- [ ] Тест: невалидный свап → возврат в Idle

---

## Возможные расширения (будущее)

### Scoring System
```csharp
public class ScoreController : MonoBehaviour
{
    public event Action<int> OnScoreChanged;

    private int _score;
    private int _cascadeMultiplier = 1;

    public void OnMatchDestroyed(MatchData match)
    {
        int points = match.Length * 10 * _cascadeMultiplier;
        _score += points;
        OnScoreChanged?.Invoke(_score);
    }

    public void IncreaseCascade() => _cascadeMultiplier++;
    public void ResetCascade() => _cascadeMultiplier = 1;
}
```

### Shuffle on Deadlock (альтернатива Game Over)
```csharp
public class BoardShuffler
{
    public void Shuffle(GridData grid, GridPositionConverter converter)
    {
        // Собрать все элементы
        // Fisher-Yates shuffle позиций
        // Проверить что нет начальных матчей
        // Анимировать перемещение
    }
}
```

### Hints System
```csharp
public class HintController : MonoBehaviour
{
    [SerializeField] private float _hintDelay = 5f;

    public void ShowHint()
    {
        var move = FindBestMove();
        HighlightElements(move.from, move.to);
    }
}
```
