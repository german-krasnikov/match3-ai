# Match3 Game Mechanics Decomposition

## Core Systems Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      GAME MANAGER                           │
│  (GameState, Level Flow, Score, Win/Lose Conditions)        │
└─────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│    BOARD    │ │   INPUT     │ │    UI       │ │   AUDIO     │
│   SYSTEM    │ │   SYSTEM    │ │   SYSTEM    │ │   SYSTEM    │
└─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘
```

---

## 1. Board System (Игровое поле)

### 1.1 Grid (Сетка)
- **GridComponent** - хранит 2D массив ячеек
  - `Cell[,] cells` - матрица ячеек
  - `int width, height` - размеры поля
  - Методы: `GetCell(x,y)`, `GetNeighbors(cell)`

### 1.2 Cell (Ячейка)
- **CellComponent** - одна ячейка поля
  - `Vector2Int position` - позиция в сетке
  - `CellType type` - обычная/блокированная/пустая
  - `Tile currentTile` - текущий тайл в ячейке
  - События: `OnTileChanged`

### 1.3 Tile (Фишка/Тайл)
- **TileComponent** - игровая фишка
  - `TileType type` - цвет/тип фишки (Red, Blue, Green, Yellow, Purple...)
  - `bool isMoving` - в движении
  - `bool isMatched` - помечен для удаления
  - События: `OnDestroyed`, `OnMoved`

### 1.4 Tile Types (Типы фишек)
```csharp
public enum TileType
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Orange,
    // Special
    None,
    Blocker
}
```

---

## 2. Match System (Система совпадений)

### 2.1 Match Detection (Поиск совпадений)
- **MatchDetector** - ищет совпадения на поле
  - Горизонтальные линии (3+)
  - Вертикальные линии (3+)
  - L-образные (3+3)
  - T-образные (3+3)
  - Крест (3+3)
  - Квадрат 2x2 (опционально)

### 2.2 Match Data
```csharp
public class MatchData
{
    public List<Tile> tiles;      // Все тайлы в совпадении
    public MatchType type;         // Тип совпадения
    public Vector2Int center;      // Центр (для спецэффектов)
}

public enum MatchType
{
    Line3,          // Обычная линия 3
    Line4,          // Линия 4 (бонус)
    Line5,          // Линия 5 (супер бонус)
    LShape,         // L-образное
    TShape,         // T-образное
    Cross,          // Крест
    Square          // Квадрат 2x2
}
```

### 2.3 Match Resolution Order
1. Найти все совпадения
2. Объединить пересекающиеся
3. Определить тип каждого совпадения
4. Создать спецтайлы если нужно
5. Уничтожить обычные тайлы

---

## 3. Swap System (Система обмена)

### 3.1 Swap Logic
- **SwapController** - контролирует обмен фишек
  - Проверка валидности обмена (только соседние)
  - Анимация обмена
  - Откат если нет совпадения
  - События: `OnSwapStarted`, `OnSwapCompleted`, `OnSwapFailed`

### 3.2 Swap Validation
```csharp
bool IsValidSwap(Cell a, Cell b)
{
    // 1. Соседние клетки?
    // 2. Обе содержат movable тайлы?
    // 3. Результат даёт совпадение?
}
```

---

## 4. Cascade System (Система каскадов)

### 4.1 Gravity (Падение)
- **GravityController** - опускает тайлы вниз
  - После уничтожения - тайлы падают
  - Анимация падения
  - События: `OnFallStarted`, `OnFallCompleted`

### 4.2 Tile Spawner (Генератор)
- **TileSpawner** - создаёт новые тайлы
  - Спавн сверху после падения
  - Случайный выбор типа
  - Гарантия отсутствия начальных совпадений
  - События: `OnTileSpawned`

### 4.3 Cascade Flow
```
[Match Found] → [Destroy Tiles] → [Apply Gravity] → [Spawn New] → [Check Matches] → Loop
```

---

## 5. Special Tiles (Спецэффекты)

### 5.1 Bonus Tiles (создаются при совпадениях)
| Совпадение | Бонус | Эффект |
|------------|-------|--------|
| 4 в ряд | Striped (полосатый) | Уничтожает ряд/колонку |
| L или T | Wrapped (обёрнутый) | Взрыв 3x3 |
| 5 в ряд | Color Bomb | Уничтожает все тайлы одного цвета |

### 5.2 Special Tile Components
- **StripedTile** : TileComponent
  - `Direction direction` - горизонталь/вертикаль
  - При активации - линия уничтожения

- **WrappedTile** : TileComponent
  - При активации - взрыв 3x3
  - Двойной взрыв при повторном матче

- **ColorBomb** : TileComponent
  - При свапе с любым - уничтожает все такого цвета
  - Комбо с другими бонусами

### 5.3 Bonus Combinations
| Комбо | Эффект |
|-------|--------|
| Striped + Striped | Крест уничтожения |
| Striped + Wrapped | 3 ряда + 3 колонки |
| Wrapped + Wrapped | Взрыв 5x5 |
| ColorBomb + Striped | Все тайлы цвета → Striped |
| ColorBomb + Wrapped | Все тайлы цвета → Wrapped |
| ColorBomb + ColorBomb | Очистка всего поля |

---

## 6. Input System (Ввод)

### 6.1 Input Handler
- **InputController** - обработка ввода
  - Touch/Mouse down - выбор тайла
  - Drag - направление свапа
  - Release - выполнение свапа
  - События: `OnTileSelected`, `OnSwipeDetected`

### 6.2 Input States
```csharp
public enum InputState
{
    Idle,           // Ожидание
    TileSelected,   // Тайл выбран
    Dragging,       // Перетаскивание
    Blocked         // Анимация/обработка
}
```

---

## 7. Game Flow (Игровой цикл)

### 7.1 Game States
```csharp
public enum GameState
{
    Loading,        // Загрузка уровня
    Ready,          // Готов к игре
    PlayerInput,    // Ожидание ввода
    Swapping,       // Анимация обмена
    Matching,       // Поиск совпадений
    Destroying,     // Уничтожение
    Falling,        // Падение
    Spawning,       // Генерация
    CheckingBoard,  // Проверка поля
    Win,            // Победа
    Lose            // Поражение
}
```

### 7.2 State Flow Diagram
```
[Loading] → [Ready] → [PlayerInput] → [Swapping]
                              ↑              ↓
                              │         [Matching]
                              │              ↓
                       [CheckingBoard] ← [Destroying]
                              │              ↓
                              │         [Falling]
                              │              ↓
                              └──────── [Spawning]

[CheckingBoard] → [Win] or [Lose] (if conditions met)
```

---

## 8. Level System (Система уровней)

### 8.1 Level Data (ScriptableObject)
```csharp
[CreateAssetMenu]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;
    public int moves;               // Лимит ходов
    public int targetScore;         // Целевые очки
    public TileType[] availableTypes;
    public GoalData[] goals;        // Цели уровня
    public CellType[,] layout;      // Карта ячеек
}
```

### 8.2 Goals (Цели уровня)
```csharp
public enum GoalType
{
    Score,              // Набрать N очков
    CollectTiles,       // Собрать N тайлов типа X
    ClearBlockers,      // Убрать N блокеров
    ReachBottom,        // Опустить предмет вниз
    CollectSpecial      // Собрать N спецтайлов
}

[Serializable]
public class GoalData
{
    public GoalType type;
    public TileType tileType;   // Для CollectTiles
    public int amount;
}
```

---

## 9. Score System (Очки)

### 9.1 Score Rules
| Действие | Очки |
|----------|------|
| Match 3 | 50 |
| Match 4 | 100 |
| Match 5 | 200 |
| L/T Match | 150 |
| Cascade bonus | x1.5 за каждый каскад |
| Special activation | 100-500 |

### 9.2 Score Component
- **ScoreManager** - подсчёт очков
  - Текущий счёт
  - Множитель каскада
  - События: `OnScoreChanged`, `OnComboChanged`

---

## 10. Obstacles & Blockers (Препятствия)

### 10.1 Blocker Types
| Тип | Описание | HP |
|-----|----------|-----|
| Ice | Замороженный тайл | 1 |
| Double Ice | Двойной лёд | 2 |
| Chain | Цепь (тайл не двигается) | 1-2 |
| Stone | Камень (пустая блокировка) | 1-3 |
| Crate | Ящик (опускается вниз) | 1 |
| Honey | Распространяется | 1 |

### 10.2 Blocker Component
- **BlockerComponent** - препятствие
  - `BlockerType type`
  - `int hitPoints`
  - `bool blocksTile` - блокирует тайл внутри
  - `bool blocksCell` - занимает ячейку
  - События: `OnDamaged`, `OnDestroyed`

---

## 11. Power-ups & Boosters (Усилители)

### 11.1 Pre-game Boosters
- Extra moves (+5 ходов)
- Start with special (начать со спецтайлом)
- Shuffle (перемешать в начале)

### 11.2 In-game Boosters
| Бустер | Эффект |
|--------|--------|
| Hammer | Уничтожить 1 тайл |
| Swap | Поменять любые 2 тайла |
| Shuffle | Перемешать поле |
| Color clear | Убрать все тайлы цвета |
| Row/Column | Убрать ряд/колонку |

---

## 12. Visual & Animation System

### 12.1 Animation Types
- Tile swap (обмен)
- Tile fall (падение)
- Tile destroy (уничтожение)
- Special activation (активация бонуса)
- Score popup
- Goal progress

### 12.2 Visual Feedback
- Tile selection highlight
- Valid swap hint
- Possible match hint (after idle)
- Cascade multiplier display
- Goal completion celebration

---

## 13. Audio System

### 13.1 Sound Events
- Tile select
- Swap success/fail
- Match (разные для 3/4/5)
- Special activation
- Cascade combo
- Win/Lose
- Button clicks

---

## 14. UI System

### 14.1 Screens
- Main Menu
- Level Select
- Gameplay HUD
- Pause Menu
- Win Screen
- Lose Screen
- Shop (boosters)

### 14.2 HUD Elements
- Score display
- Moves counter
- Goals progress
- Boosters panel
- Pause button

---

## 15. Component Architecture (Unity Way)

### 15.1 Core Components
```
Scripts/
├── Components/
│   ├── Board/
│   │   ├── GridComponent.cs
│   │   ├── CellComponent.cs
│   │   └── TileComponent.cs
│   ├── Tiles/
│   │   ├── StripedTile.cs
│   │   ├── WrappedTile.cs
│   │   └── ColorBomb.cs
│   ├── Blockers/
│   │   └── BlockerComponent.cs
│   └── Effects/
│       └── TileAnimator.cs
├── Interfaces/
│   ├── IMatchable.cs
│   ├── IDestroyable.cs
│   ├── IMovable.cs
│   └── IActivatable.cs
├── Core/
│   ├── GameManager.cs
│   ├── BoardController.cs
│   ├── MatchDetector.cs
│   ├── SwapController.cs
│   ├── GravityController.cs
│   ├── ScoreManager.cs
│   └── LevelManager.cs
├── Input/
│   └── InputController.cs
├── Data/
│   ├── LevelData.cs
│   └── TileData.cs
└── UI/
    ├── HUDController.cs
    └── ScreenManager.cs
```

### 15.2 Event-Driven Communication
```csharp
// Пример событийной связи
public class TileComponent : MonoBehaviour, IMatchable, IDestroyable
{
    public event Action<TileComponent> OnMatched;
    public event Action<TileComponent> OnDestroyed;
    public event Action<Vector3, Vector3> OnMoveStarted;
}

public class BoardController : MonoBehaviour
{
    private void OnEnable()
    {
        _matchDetector.OnMatchFound += HandleMatch;
        _gravityController.OnFallComplete += CheckForMatches;
    }
}
```

---

## Implementation Priority (Порядок реализации)

### Phase 1: Core Board
1. Grid + Cell + Tile components
2. Board initialization
3. Basic tile spawning

### Phase 2: Input & Swap
4. Input handling
5. Tile selection
6. Swap mechanic

### Phase 3: Match & Destroy
7. Match detection (3+ lines)
8. Tile destruction
9. Score basics

### Phase 4: Cascade
10. Gravity system
11. New tile spawning
12. Chain reactions

### Phase 5: Specials
13. Bonus tiles (Striped, Wrapped)
14. Color Bomb
15. Bonus combinations

### Phase 6: Levels
16. Level data structure
17. Goals system
18. Win/Lose conditions

### Phase 7: Polish
19. Blockers
20. Boosters
21. Full UI
22. Audio
23. Particle effects
