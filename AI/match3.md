# Match3 Base Mechanics - Implementation Plan

## Summary
Настраиваемая сетка NxM, 5 типов элементов, автокаскады, валидный свап с откатом.

---

## Phase 1: Core Data Structures

### 1.1 GridPosition (struct)
**File:** `Assets/Scripts/Core/GridPosition.cs` (~30 lines)
- Immutable координаты X, Y
- Операторы сложения для направлений
- Статические константы: Up, Down, Left, Right
- IEquatable для сравнений

### 1.2 ElementType (ScriptableObject)
**File:** `Assets/Scripts/Data/ElementType.cs` (~40 lines)
- ID, Sprite, Color
- DestroyVfxPrefab, DestroySound (опционально)

### 1.3 GridConfig (ScriptableObject)
**File:** `Assets/Scripts/Data/GridConfig.cs` (~50 lines)
- Width, Height (настраиваемые)
- CellSize, SwapDuration, FallSpeed, DestroyDuration
- ElementType[] - массив 5 типов

---

## Phase 2: Grid System

### 2.1 GridData (Pure C# class)
**File:** `Assets/Scripts/Grid/GridData.cs` (~80 lines)
- 2D массив IElement[,]
- События: OnElementSet, OnElementRemoved
- Методы: GetElement, SetElement, RemoveElement, SwapElements
- IsValidPosition, GetEmptyPositions

### 2.2 GridPositionConverter (Pure C# class)
**File:** `Assets/Scripts/Grid/GridPositionConverter.cs` (~40 lines)
- GridToWorld(GridPosition) → Vector3
- WorldToGrid(Vector3) → GridPosition

### 2.3 GridView (MonoBehaviour)
**File:** `Assets/Scripts/Grid/GridView.cs` (~60 lines)
- SerializeField: GridConfig, cellPrefab
- CreateVisualGrid() - создаёт фон сетки
- Хранит GridPositionConverter

---

## Phase 3: Element System

### 3.1 IElement (Interface)
**File:** `Assets/Scripts/Elements/IElement.cs` (~15 lines)
- Type, Position, Transform
- Initialize(), PlayDestroyAnimation(), MoveTo()

### 3.2 ElementView (MonoBehaviour)
**File:** `Assets/Scripts/Elements/ElementView.cs` (~90 lines)
- Реализует IElement
- SpriteRenderer для визуала
- DOTween анимации: MoveTo, PlayDestroyAnimation

### 3.3 ElementPool (MonoBehaviour)
**File:** `Assets/Scripts/Elements/ElementPool.cs` (~60 lines)
- Пул объектов для переиспользования
- Get(), Return()

### 3.4 ElementFactory (MonoBehaviour)
**File:** `Assets/Scripts/Elements/ElementFactory.cs` (~50 lines)
- CreateElement(type, position, worldPos)
- ReturnElement(element)

---

## Phase 4: Spawn System

### 4.1 ISpawnStrategy (Interface)
**File:** `Assets/Scripts/Spawn/ISpawnStrategy.cs` (~10 lines)
- GetNextType(position, grid, types)

### 4.2 RandomSpawnStrategy (Pure C# class)
**File:** `Assets/Scripts/Spawn/RandomSpawnStrategy.cs` (~70 lines)
- Случайный выбор типа БЕЗ создания матчей
- Проверяет 2 соседа слева и снизу

### 4.3 SpawnController (MonoBehaviour)
**File:** `Assets/Scripts/Spawn/SpawnController.cs` (~80 lines)
- FillGrid() - начальное заполнение
- SpawnAtTop(column) - спаун сверху после матчей
- Событие: OnSpawnComplete

---

## Phase 5: Match Detection

### 5.1 MatchResult (Data class)
**File:** `Assets/Scripts/Data/MatchResult.cs` (~25 lines)
- List<GridPosition> Positions
- ElementType MatchedType
- IsHorizontal

### 5.2 IMatchFinder (Interface)
**File:** `Assets/Scripts/Match/IMatchFinder.cs` (~10 lines)
- FindMatches(grid)
- FindMatches(grid, positions)

### 5.3 LineMatchFinder (Pure C# class)
**File:** `Assets/Scripts/Match/LineMatchFinder.cs` (~100 lines)
- Поиск линий 3+ по горизонтали/вертикали
- Расширение в обе стороны от точки

### 5.4 MatchMerger (Pure C# class)
**File:** `Assets/Scripts/Match/MatchMerger.cs` (~50 lines)
- Объединяет пересекающиеся матчи в HashSet

### 5.5 MatchController (MonoBehaviour)
**File:** `Assets/Scripts/Match/MatchController.cs` (~60 lines)
- CheckForMatches()
- События: OnMatchesFound, OnNoMatchesFound

---

## Phase 6:ознакомься с @AI//Fall System

### 6.1 FallData (Data class)
**File:** `Assets/Scripts/Gravity/FallData.cs` (~20 lines)
- Element, From, To, Distance

### 6.2 GravityCalculator (Pure C# class)
**File:** `Assets/Scripts/Gravity/GravityCalculator.cs` (~80 lines)
- CalculateFalls(grid) - какие элементы куда падают
- CountEmptyAbove(grid, column)

### 6.3 GravityController (MonoBehaviour)
**File:** `Assets/Scripts/Gravity/GravityController.cs` (~100 lines)
- ApplyGravity() - запускает падение
- Спаунит новые элементы сверху
- Рекурсивно проверяет новые падения
- Событие: OnFallComplete

---

## Phase 7: Input & Swap System

### 7.1 IInputHandler (Interface)
**File:** `Assets/Scripts/Input/IInputHandler.cs` (~10 lines)
- OnElementSelected, OnSwapRequested
- SetEnabled(bool)

### 7.2 TouchInputHandler (MonoBehaviour)
**File:** `Assets/Scripts/Input/TouchInputHandler.cs` (~90 lines)
- Обработка touch/mouse
- Определение направления свайпа
- События: OnElementSelected, OnSwapRequested

### 7.3 SwapValidator (Pure C# class)
**File:** `Assets/Scripts/Swap/SwapValidator.cs` (~30 lines)
- AreNeighbors(a, b)
- WouldCreateMatch(grid, a, b, matchFinder)

### 7.4 SwapController (MonoBehaviour)
**File:** `Assets/Scripts/Swap/SwapController.cs` (~100 lines)
- TrySwap(a, b)
- Анимация свапа (DOTween)
- Откат если нет матча
- События: OnSwapComplete, OnSwapRolledBack

---

## Phase 8: Destruction System

### 8.1 DestructionController (MonoBehaviour)
**File:** `Assets/Scripts/Destruction/DestructionController.cs` (~80 lines)
- DestroyElements(positions)
- Анимации уничтожения
- Возврат в пул
- События: OnElementsDestroyed, OnDestructionComplete

---

## Phase 9: Game Loop

### 9.1 GameState (Enum)
**File:** `Assets/Scripts/GameLoop/GameState.cs` (~15 lines)
- Initializing, Idle, Swapping, Matching, Destroying, Falling, CheckingBoard

### 9.2 DeadlockChecker (Pure C# class)
**File:** `Assets/Scripts/GameLoop/DeadlockChecker.cs` (~80 lines)
- HasPossibleMoves(grid) - проверка на тупик

### 9.3 GameStateMachine (MonoBehaviour)
**File:** `Assets/Scripts/GameLoop/GameStateMachine.cs` (~120 lines)
- Управляет состояниями
- Подписывается на все события контроллеров
- Включает/выключает ввод
- Событие: OnStateChanged, OnDeadlock

### 9.4 GameController (MonoBehaviour)
**File:** `Assets/Scripts/Game/GameController.cs` (~100 lines)
- Entry point
- Инициализирует все системы
- Связывает зависимости через Inspector
- Счёт

---

## Game Loop Flow

```
IDLE (ожидание ввода)
  │
  ▼ [Свайп]
SWAPPING (анимация свапа)
  │
  ├─ Нет матча → откат → IDLE
  │
  ▼ [Есть матч]
MATCHING (проверка матчей)
  │
  ▼
DESTROYING (анимация уничтожения)
  │
  ▼
FALLING (падение + спаун сверху)
  │
  ▼
MATCHING ← каскад пока есть матчи
  │
  ▼ [Нет матчей]
CHECKING_BOARD (проверка тупика)
  │
  ▼
IDLE
```

---

## Implementation Order (рекомендуемый)

1. **Core** → GridPosition, ElementType, GridConfig
2. **Grid** → GridData, GridPositionConverter, GridView
3. **Elements** → IElement, ElementView, Pool, Factory
4. **Spawn** → Strategy, Controller (тест: заполнение без матчей)
5. **Match** → LineMatchFinder, MatchController (тест: находит матчи)
6. **Input** → TouchInputHandler (тест: свайпы регистрируются)
7. **Swap** → SwapValidator, SwapController (тест: свап + откат)
8. **Destruction** → DestructionController (тест: элементы исчезают)
9. **Gravity** → Calculator, Controller (тест: падение + респаун)
10. **GameLoop** → StateMachine, GameController (полный цикл)

---

## File Structure

```
Assets/Scripts/
├── Core/
│   └── GridPosition.cs
├── Data/
│   ├── ElementType.cs
│   ├── GridConfig.cs
│   └── MatchResult.cs
├── Grid/
│   ├── GridData.cs
│   ├── GridPositionConverter.cs
│   └── GridView.cs
├── Elements/
│   ├── IElement.cs
│   ├── ElementView.cs
│   ├── ElementFactory.cs
│   └── ElementPool.cs
├── Spawn/
│   ├── ISpawnStrategy.cs
│   ├── RandomSpawnStrategy.cs
│   └── SpawnController.cs
├── Match/
│   ├── IMatchFinder.cs
│   ├── LineMatchFinder.cs
│   ├── MatchMerger.cs
│   └── MatchController.cs
├── Gravity/
│   ├── FallData.cs
│   ├── GravityCalculator.cs
│   └── GravityController.cs
├── Input/
│   ├── IInputHandler.cs
│   └── TouchInputHandler.cs
├── Swap/
│   ├── SwapValidator.cs
│   └── SwapController.cs
├── Destruction/
│   └── DestructionController.cs
├── GameLoop/
│   ├── GameState.cs
│   ├── DeadlockChecker.cs
│   └── GameStateMachine.cs
└── Game/
    └── GameController.cs
```

**Total: ~25 files, каждый <200 строк**
