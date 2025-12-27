# Match3 Core Mechanics - Decomposition Plan

## Overview
Пошаговая декомпозиция базовых механик Match3 игры без бонусов и мета-механик.

## Config Defaults
- **Grid**: 8x8
- **Piece Types**: 6 (Red, Blue, Green, Yellow, Purple, Orange)
- **Input**: Drag & Swipe

---

## Progress (Прогресс реализации)

| Step | Название | Статус | Файлы |
|------|----------|--------|-------|
| 1 | Grid System | ✅ DONE | 14 файлов |
| 2 | Pieces | ✅ DONE | 3 файла |
| 3 | Spawner | ⏳ TODO | — |
| 4 | Gravity/Fall | ⏳ TODO | — |
| 5 | Swap | ⏳ TODO | — |
| 6 | Match/Destroy | ⏳ TODO | — |
| 7 | Game Loop | ⏳ TODO | — |

### Реализованные файлы

```
Assets/Scripts/
├── Core/                          # Step 1 ✅
│   ├── GridPosition.cs
│   ├── PieceType.cs
│   └── Interfaces/
│       ├── IPiece.cs
│       ├── IGrid.cs
│       ├── IBoardState.cs
│       ├── IMatchChecker.cs
│       ├── IMatchDetector.cs
│       ├── ISpawner.cs
│       ├── ISwappable.cs
│       ├── IScoreHandler.cs
│       └── IGameEvents.cs
│
├── Grid/                          # Step 1 ✅
│   ├── GridConfig.cs              # SO: 8x8, cellSize, spacing
│   ├── CellComponent.cs           # Ячейка сетки
│   └── GridComponent.cs           # IGrid + IBoardState
│
├── Pieces/                        # Step 2 ✅
│   ├── PieceConfig.cs             # SO: спрайты, цвета
│   ├── PieceView.cs               # Только визуал
│   └── PieceComponent.cs          # IPiece implementation
│
└── Editor/
    ├── Step1_GridSetup.cs         # Setup меню
    └── Step2_PiecesSetup.cs       # Setup меню

Assets/Configs/
├── GridConfig.asset               # ✅
└── PieceConfig.asset              # ✅

Assets/Prefabs/
├── Grid.prefab                    # ✅
├── Cell.prefab                    # ✅
└── Pieces/
    └── Piece.prefab               # ✅

Assets/Sprites/
└── WhiteSquare.png                # ✅ 64x64 белый квадрат
```

### Editor Commands

| Команда | Описание |
|---------|----------|
| `Match3/Setup/Step 1 - Create Grid Assets` | GridConfig, Cell, Grid prefabs |
| `Match3/Setup/Step 1 - Create Grid in Scene` | Спавн сетки 8x8 |
| `Match3/Setup/Step 2 - Create Pieces Assets` | PieceConfig, Piece prefab |
| `Match3/Setup/Step 2 - Test Piece in Scene` | Тест 6 фишек |
| `Match3/Setup/Step 1+2 - Full Scene Setup` | Всё вместе |

---

## 1. Grid System (Сетка)

### 1.1 Структуры данных
- `GridConfig` (ScriptableObject) - размер сетки, отступы, размер ячейки
- `GridPosition` (struct) - координаты (x, y) в сетке

### 1.2 Компоненты
- `GridComponent` - хранит 2D массив ссылок на ячейки, конвертация grid<->world координат
- `CellComponent` - отдельная ячейка, хранит ссылку на текущий элемент (или null)

### 1.3 Stubs для независимости
```csharp
// Stub: IPiece - заглушка для элемента
public interface IPiece
{
    GridPosition Position { get; set; }
    void SetWorldPosition(Vector3 pos);
}
```

### 1.4 События
- `OnGridInitialized` - сетка создана и готова

---

## 2. Pieces (Элементы)

### 2.1 Структуры данных
- `PieceType` (enum) - типы элементов (Red, Blue, Green, Yellow, Purple, Orange)
- `PieceConfig` (ScriptableObject) - спрайты, цвета для каждого типа

### 2.2 Компоненты
- `PieceComponent : MonoBehaviour, IPiece` - тип, визуал (SpriteRenderer)
- `PieceView` - отвечает только за визуальное представление

### 2.3 Stubs для независимости
```csharp
// Stub: IGrid - заглушка для сетки
public interface IGrid
{
    Vector3 GridToWorld(GridPosition pos);
    bool IsValidPosition(GridPosition pos);
}
```

### 2.4 События
- `OnPieceCreated(IPiece piece)`
- `OnPieceDestroyed(IPiece piece)`

---

## 3. Spawner (Спаун)

### 3.1 Компоненты
- `PieceSpawner` - создаёт элементы, использует Object Pool
- `PiecePool` - пул объектов для переиспользования
- `SpawnConfig` (ScriptableObject) - вероятности типов, начальное заполнение

### 3.2 Stubs для независимости
```csharp
// Stub: IMatchChecker - проверка матчей при спауне
public interface IMatchChecker
{
    bool WouldCreateMatch(GridPosition pos, PieceType type);
}
```

### 3.3 API
```csharp
IPiece SpawnPiece(PieceType type, GridPosition position);
IPiece SpawnRandomPiece(GridPosition position, IMatchChecker checker = null);
void ReturnToPool(IPiece piece);
```

### 3.4 События
- `OnPieceSpawned(IPiece piece, GridPosition pos)`

---

## 4. Gravity / Fall (Падение)

### 4.1 Компоненты
- `GravityController` - обнаруживает пустые ячейки, инициирует падение
- `FallAnimator` - анимация падения через DOTween

### 4.2 Stubs для независимости
```csharp
// Stub: ISpawner - для спауна новых элементов сверху
public interface ISpawner
{
    IPiece SpawnAtTop(int column);
}

// Stub: IBoardState - состояние доски
public interface IBoardState
{
    IPiece GetPieceAt(GridPosition pos);
    void SetPieceAt(GridPosition pos, IPiece piece);
    void ClearCell(GridPosition pos);
    int Height { get; }
    int Width { get; }
}
```

### 4.3 Алгоритм
1. Сканирование снизу вверх по столбцам
2. Поиск пустых ячеек
3. Сдвиг элементов вниз
4. Спаун новых элементов сверху
5. Анимация падения (параллельно для всех)

### 4.4 События
- `OnFallStarted`
- `OnFallCompleted` - все элементы упали

---

## 5. Swap (Обмен элементов)

### 5.1 Компоненты
- `InputHandler` - обработка Drag & Swipe (тянешь элемент в направлении свапа)
- `SwapController` - логика обмена двух соседних элементов
- `SwapAnimator` - анимация обмена через DOTween

### 5.2 Stubs для независимости
```csharp
// Stub: IMatchDetector - проверка матча после свапа
public interface IMatchDetector
{
    bool HasMatch(GridPosition pos1, GridPosition pos2);
    List<GridPosition> FindMatches();
}

// Stub: ISwappable
public interface ISwappable
{
    bool CanSwap { get; }
    GridPosition Position { get; }
}
```

### 5.3 Логика свапа
1. Проверка соседства (только горизонталь/вертикаль)
2. Анимация обмена
3. Проверка матча
4. Если нет матча - обратный свап
5. Если есть матч - передача управления MatchController

### 5.4 События
- `OnSwapStarted(GridPosition from, GridPosition to)`
- `OnSwapCompleted(bool matched)`
- `OnSwapReverted`

---

## 6. Match Detection & Destruction (Обнаружение и уничтожение)

### 6.1 Компоненты
- `MatchDetector : IMatchDetector` - поиск 3+ в ряд
- `DestructionController` - удаление matched элементов
- `DestructionAnimator` - анимации уничтожения (scale, fade, particles)

### 6.2 Stubs для независимости
```csharp
// Stub: IScoreHandler
public interface IScoreHandler
{
    void AddScore(int matchCount, PieceType type);
}
```

### 6.3 Алгоритм Match Detection
1. Горизонтальное сканирование (3+ одинаковых подряд)
2. Вертикальное сканирование
3. Объединение пересекающихся матчей
4. Возврат списка позиций для уничтожения

### 6.4 События
- `OnMatchFound(List<GridPosition> positions, PieceType type)`
- `OnDestructionStarted`
- `OnDestructionCompleted`

---

## 7. Game Loop (Игровой цикл)

### 7.1 Компоненты
- `GameController` - главный оркестратор
- `BoardController` - управление доской (связывает Grid + Pieces)
- `TurnStateMachine` - состояния хода

### 7.2 Состояния (TurnState)
```csharp
public enum TurnState
{
    WaitingForInput,  // Ожидание действия игрока
    Swapping,         // Анимация обмена
    Matching,         // Проверка и уничтожение матчей
    Falling,          // Падение элементов
    CheckingCascade   // Проверка каскадных матчей
}
```

### 7.3 Stubs для независимости
```csharp
// Stub: IGameEvents - глобальные события игры
public interface IGameEvents
{
    event Action OnGameStarted;
    event Action OnGameEnded;
    event Action<int> OnScoreChanged;
}
```

### 7.4 Цикл хода
```
WaitingForInput
    ↓ (player swap)
Swapping
    ↓ (animation done)
Matching
    ↓ (matches found?)
    ├─ NO → Revert swap → WaitingForInput
    └─ YES → Destroy → Falling
                         ↓
                   CheckingCascade
                         ↓ (new matches?)
                         ├─ YES → Matching (loop)
                         └─ NO → WaitingForInput
```

### 7.5 События
- `OnTurnStarted`
- `OnTurnEnded`
- `OnCascade(int cascadeLevel)`

---

## File Structure (Итоговая структура)

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GridPosition.cs
│   │   └── Interfaces/
│   │       ├── IPiece.cs
│   │       ├── IGrid.cs
│   │       ├── IBoardState.cs
│   │       ├── IMatchDetector.cs
│   │       ├── IMatchChecker.cs
│   │       ├── ISpawner.cs
│   │       ├── ISwappable.cs
│   │       ├── IScoreHandler.cs
│   │       └── IGameEvents.cs
│   │
│   ├── Grid/
│   │   ├── GridComponent.cs
│   │   ├── GridConfig.cs (SO)
│   │   └── CellComponent.cs
│   │
│   ├── Pieces/
│   │   ├── PieceComponent.cs
│   │   ├── PieceConfig.cs (SO)
│   │   ├── PieceType.cs
│   │   └── PieceView.cs
│   │
│   ├── Spawn/
│   │   ├── PieceSpawner.cs
│   │   ├── PiecePool.cs
│   │   └── SpawnConfig.cs (SO)
│   │
│   ├── Gravity/
│   │   ├── GravityController.cs
│   │   └── FallAnimator.cs
│   │
│   ├── Swap/
│   │   ├── InputHandler.cs
│   │   ├── SwapController.cs
│   │   └── SwapAnimator.cs
│   │
│   ├── Match/
│   │   ├── MatchDetector.cs
│   │   ├── DestructionController.cs
│   │   └── DestructionAnimator.cs
│   │
│   └── GameLoop/
│       ├── GameController.cs
│       ├── BoardController.cs
│       └── TurnStateMachine.cs
│
├── Configs/
│   ├── GridConfig.asset
│   ├── PieceConfig.asset
│   └── SpawnConfig.asset
│
└── Prefabs/
    ├── Board.prefab
    ├── Cell.prefab
    └── Pieces/
        └── Piece.prefab
```

---

## Implementation Order (Рекомендуемый порядок)

1. **Core/Interfaces** - все интерфейсы и структуры
2. **Grid** - сетка без элементов
3. **Pieces** - элементы (создание, визуал)
4. **Spawner** - спаун и пул
5. **Match Detection** - поиск матчей
6. **Destruction** - уничтожение
7. **Gravity/Fall** - падение
8. **Swap** - обмен и инпут
9. **Game Loop** - оркестрация всего

---

## Key Principles

- **Каждый модуль независим** благодаря интерфейсам-заглушкам
- **Event-driven** - компоненты общаются через события
- **DOTween** для всех анимаций
- **ScriptableObjects** для конфигов (легко менять в редакторе)
- **Object Pooling** для элементов (performance)
