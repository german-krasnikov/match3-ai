# Match3 MVP Plan

## Архитектура

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            GameEntryPoint                                │
│                         (Script Execution: -100)                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            ▼                       ▼                       ▼
    ┌───────────────┐      ┌───────────────┐      ┌───────────────┐
    │ ServiceLocator │      │   GameConfig  │      │    Views      │
    │               │      │     (SO)      │      │ .Initialize() │
    └───────────────┘      └───────────────┘      └───────────────┘
            │
    ┌───────┴───────┐
    ▼               ▼
IInputService  ISpawnService
```

### Game Loop State Machine

```
     ┌──────────────────────────────────────────────────────────┐
     │                   GameplayCoordinator                     │
     │                                                          │
     │  ┌─────────┐    ┌──────┐    ┌───────┐    ┌─────────┐    │
     │  │ WaitingForInput │───▶│ Swapping │───▶│ Matching │───▶│ Destroying │
     │  └─────────┘    └──────┘    └───────┘    └─────────┘    │
     │       ▲                                        │         │
     │       │         ┌──────────┐    ┌─────────┐   │         │
     │       └─────────│ Refilling │◀───│ Falling │◀──┘         │
     │                 └──────────┘    └─────────┘             │
     │                        │                                 │
     │                        ▼ (if matches found)              │
     │                   [back to Matching]                     │
     └──────────────────────────────────────────────────────────┘
```

### MVP Pattern

```
┌────────────────────┐     events      ┌────────────────────┐
│   BoardModel       │◀───────────────│   BoardPresenter   │
│   (Pure C#)        │                 │   (Pure C#)        │
│                    │────────────────▶│                    │
│ - Cell[,] grid     │   read state    │ - IBoardView       │
│ - FindMatches()    │                 │ - Handle events    │
│ - RemoveElements() │                 │ - Coordinate flow  │
└────────────────────┘                 └────────────────────┘
                                              │
                                              │ interface
                                              ▼
                                       ┌────────────────────┐
                                       │   BoardView        │
                                       │   (MonoBehaviour)  │
                                       │                    │
                                       │ - ElementViews     │
                                       │ - Animations       │
                                       │ - Input handling   │
                                       └────────────────────┘
```

---

## Структура проекта

```
Assets/
├── Scripts/
│   ├── Game.asmdef
│   ├── Core/
│   │   ├── ServiceLocator.cs
│   │   ├── Services.cs
│   │   └── GameEntryPoint.cs
│   ├── Common/
│   │   ├── GridPosition.cs
│   │   └── ElementType.cs
│   ├── Features/
│   │   └── Board/
│   │       ├── Models/
│   │       │   ├── BoardModel.cs
│   │       │   ├── Cell.cs
│   │       │   └── Element.cs
│   │       ├── Presenters/
│   │       │   └── BoardPresenter.cs
│   │       ├── Views/
│   │       │   ├── IBoardView.cs
│   │       │   ├── BoardView.cs
│   │       │   ├── IElementView.cs
│   │       │   └── ElementView.cs
│   │       └── Services/
│   │           ├── IInputService.cs
│   │           ├── InputService.cs
│   │           ├── ISpawnService.cs
│   │           ├── SpawnService.cs
│   │           ├── IMatchService.cs
│   │           ├── MatchService.cs
│   │           ├── IFallService.cs
│   │           └── FallService.cs
│   ├── Configs/
│   │   └── GameConfig.cs
│   ├── Gameplay/
│   │   ├── GameplayCoordinator.cs
│   │   └── GameState.cs
│   └── Editor/
│       ├── Editor.asmdef
│       └── Setup/
│           └── SceneSetup.cs
└── Tests/
    └── EditMode/
        ├── Tests.EditMode.asmdef
        └── Features/
            └── Board/
                ├── BoardModelTests.cs
                ├── BoardPresenterTests.cs
                ├── MatchServiceTests.cs
                └── FallServiceTests.cs
```

---

## Модули и ответственности

| Модуль | Тип | Ответственность |
|--------|-----|-----------------|
| **BoardModel** | Pure C# | Состояние сетки 8x8, CRUD элементов |
| **BoardPresenter** | Pure C# | Связь Model↔View, координация анимаций |
| **BoardView** | MonoBehaviour | Отображение сетки, пул ElementView |
| **ElementView** | MonoBehaviour | Визуал одного элемента, SpriteRenderer |
| **GameplayCoordinator** | Pure C# | State Machine, управление циклом |
| **IInputService** | Interface | Drag & Drop ввод |
| **ISpawnService** | Interface | Генерация элементов |
| **IMatchService** | Interface | Поиск матчей (линии 3+) |
| **IFallService** | Interface | Расчёт падения |
| **GameConfig** | ScriptableObject | Все настройки игры |

---

## Интерфейсы и Stub-реализации

### Common/ElementType.cs
```csharp
namespace Common
{
    public enum ElementType
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5
    }
}
```

### Common/GridPosition.cs
```csharp
namespace Common
{
    public readonly struct GridPosition
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y) { X = x; Y = y; }

        public static GridPosition Invalid => new(-1, -1);
        public bool IsValid => X >= 0 && Y >= 0;
    }
}
```

### Gameplay/GameState.cs
```csharp
namespace Gameplay
{
    public enum GameState
    {
        WaitingForInput,
        Swapping,
        Matching,
        Destroying,
        Falling,
        Refilling
    }
}
```

### Features/Board/Services/IInputService.cs
```csharp
namespace Features.Board.Services
{
    public interface IInputService
    {
        event Action<GridPosition, GridPosition> OnSwapRequested;
        void SetInputEnabled(bool enabled);
    }
}
```

### Features/Board/Services/ISpawnService.cs
```csharp
namespace Features.Board.Services
{
    public interface ISpawnService
    {
        ElementType GetRandomElement();
        ElementType GetRandomElementExcluding(ElementType[] excluded);
    }
}
```

### Features/Board/Services/IMatchService.cs
```csharp
namespace Features.Board.Services
{
    public interface IMatchService
    {
        List<GridPosition> FindAllMatches(BoardModel board);
        List<GridPosition> FindMatchesAt(BoardModel board, GridPosition pos);
        bool WouldCreateMatch(BoardModel board, GridPosition pos, ElementType type);
    }
}
```

### Features/Board/Services/IFallService.cs
```csharp
namespace Features.Board.Services
{
    public interface IFallService
    {
        // Returns list of (from, to) movements
        List<(GridPosition from, GridPosition to)> CalculateFalls(BoardModel board);
        // Returns positions that need new elements (top of columns)
        List<GridPosition> GetEmptyTopPositions(BoardModel board);
    }
}
```

### Features/Board/Views/IBoardView.cs
```csharp
namespace Features.Board.Views
{
    public interface IBoardView
    {
        void Initialize(int width, int height);
        void CreateElement(GridPosition pos, ElementType type);
        void DestroyElement(GridPosition pos, Action onComplete);
        void SwapElements(GridPosition from, GridPosition to, Action onComplete);
        void MoveElement(GridPosition from, GridPosition to, Action onComplete);
        void SpawnElement(GridPosition pos, ElementType type, Action onComplete);

        event Action<GridPosition> OnElementClicked;
        event Action<GridPosition, GridPosition> OnElementDragged;
    }
}
```

### Features/Board/Views/IElementView.cs
```csharp
namespace Features.Board.Views
{
    public interface IElementView
    {
        GridPosition Position { get; }
        ElementType Type { get; }
        void Initialize(GridPosition pos, ElementType type);
        void MoveTo(GridPosition newPos, float duration, Action onComplete);
        void PlayDestroyAnimation(Action onComplete);
        void SetSprite(Sprite sprite);
        void Reset();
    }
}
```

### Features/Board/Models/Element.cs
```csharp
namespace Features.Board.Models
{
    public class Element
    {
        public ElementType Type { get; private set; }

        public Element(ElementType type) => Type = type;

        public bool CanMatch => Type != ElementType.None;
        public bool Matches(Element other) => other != null && Type == other.Type && CanMatch;
    }
}
```

### Features/Board/Models/Cell.cs
```csharp
namespace Features.Board.Models
{
    public class Cell
    {
        public GridPosition Position { get; }
        public Element Element { get; private set; }

        public Cell(GridPosition pos) => Position = pos;

        public bool IsEmpty => Element == null;
        public void SetElement(Element element) => Element = element;
        public Element RemoveElement() { var e = Element; Element = null; return e; }
    }
}
```

### Features/Board/Models/BoardModel.cs
```csharp
namespace Features.Board.Models
{
    public class BoardModel
    {
        public int Width { get; }
        public int Height { get; }
        private readonly Cell[,] _cells;

        public event Action<GridPosition, ElementType> OnElementAdded;
        public event Action<GridPosition> OnElementRemoved;
        public event Action<GridPosition, GridPosition> OnElementsSwapped;

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];
            InitializeCells();
        }

        public Cell GetCell(GridPosition pos) => IsValidPosition(pos) ? _cells[pos.X, pos.Y] : null;
        public Element GetElement(GridPosition pos) => GetCell(pos)?.Element;
        public bool IsValidPosition(GridPosition pos) =>
            pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

        public void SetElement(GridPosition pos, Element element) { /* stub */ }
        public void RemoveElement(GridPosition pos) { /* stub */ }
        public void SwapElements(GridPosition a, GridPosition b) { /* stub */ }

        private void InitializeCells() { /* stub */ }
    }
}
```

### Gameplay/GameplayCoordinator.cs
```csharp
namespace Gameplay
{
    public class GameplayCoordinator : IDisposable
    {
        private GameState _state = GameState.WaitingForInput;

        private readonly BoardModel _board;
        private readonly IBoardView _view;
        private readonly IInputService _input;
        private readonly IMatchService _matcher;
        private readonly IFallService _faller;
        private readonly ISpawnService _spawner;

        public event Action<GameState> OnStateChanged;

        public GameplayCoordinator(
            BoardModel board,
            IBoardView view,
            IInputService input,
            IMatchService matcher,
            IFallService faller,
            ISpawnService spawner)
        {
            _board = board;
            _view = view;
            _input = input;
            _matcher = matcher;
            _faller = faller;
            _spawner = spawner;

            _input.OnSwapRequested += HandleSwapRequested;
        }

        private void SetState(GameState state) { /* stub */ }
        private void HandleSwapRequested(GridPosition a, GridPosition b) { /* stub */ }
        private void ProcessSwap(GridPosition a, GridPosition b) { /* stub */ }
        private void ProcessMatches() { /* stub */ }
        private void ProcessDestroy(List<GridPosition> matches) { /* stub */ }
        private void ProcessFall() { /* stub */ }
        private void ProcessRefill() { /* stub */ }

        public void Dispose() => _input.OnSwapRequested -= HandleSwapRequested;
    }
}
```

---

## Порядок реализации (шаги)

### Step 1: Core + Grid Infrastructure
**Цель:** Базовая инфраструктура и сетка без визуала

**Файлы:**
- `Core/ServiceLocator.cs`
- `Core/Services.cs`
- `Common/ElementType.cs`
- `Common/GridPosition.cs`
- `Features/Board/Models/Element.cs`
- `Features/Board/Models/Cell.cs`
- `Features/Board/Models/BoardModel.cs`

**Тесты:**
- `BoardModelTests.cs` — создание сетки, добавление/удаление элементов

**Критерий готовности:** BoardModel создаётся, хранит элементы, тесты проходят

---

### Step 2: Spawn Service + Initial Fill
**Цель:** Заполнение сетки без матчей

**Файлы:**
- `Features/Board/Services/ISpawnService.cs`
- `Features/Board/Services/SpawnService.cs`
- `Configs/GameConfig.cs` (базовый)

**Зависимости:** Step 1 (BoardModel)

**Тесты:**
- `SpawnServiceTests.cs` — генерация без матчей

**Критерий готовности:** Сетка заполняется без начальных матчей

---

### Step 3: Match Detection
**Цель:** Поиск матчей (горизонтальные + вертикальные линии 3+)

**Файлы:**
- `Features/Board/Services/IMatchService.cs`
- `Features/Board/Services/MatchService.cs`

**Зависимости:** Step 1 (BoardModel)

**Тесты:**
- `MatchServiceTests.cs` — линии 3/4/5, горизонтальные, вертикальные, пересечения

**Критерий готовности:** Все типы линейных матчей находятся корректно

---

### Step 4: View + Visual Representation
**Цель:** Отображение сетки через View

**Файлы:**
- `Features/Board/Views/IElementView.cs`
- `Features/Board/Views/ElementView.cs`
- `Features/Board/Views/IBoardView.cs`
- `Features/Board/Views/BoardView.cs`
- `Features/Board/Presenters/BoardPresenter.cs` (базовый)
- Обновить `GameConfig.cs` (спрайты, размеры)

**Зависимости:** Step 1-3

**Тесты:**
- `BoardPresenterTests.cs` — вызовы view при изменении model (с mock)

**Критерий готовности:** Сетка 8x8 отображается со спрайтами

---

### Step 5: Input + Drag & Drop
**Цель:** Обработка ввода игрока

**Файлы:**
- `Features/Board/Services/IInputService.cs`
- `Features/Board/Services/InputService.cs`
- Обновить `BoardView.cs` (drag handling)
- Обновить `ElementView.cs` (input events)

**Зависимости:** Step 4

**Тесты:**
- `InputServiceTests.cs` — валидация соседних позиций

**Критерий готовности:** Drag элемента на соседа генерирует событие swap

---

### Step 6: Swap Logic + Validation
**Цель:** Свап с валидацией (только если создаёт матч)

**Файлы:**
- Обновить `BoardModel.cs` (SwapElements)
- Обновить `BoardPresenter.cs` (swap logic)
- Обновить `BoardView.cs` (swap animation)
- Обновить `GameConfig.cs` (swap duration)

**Зависимости:** Step 3 (MatchService), Step 5 (Input)

**Тесты:**
- Обновить `BoardModelTests.cs` — swap логика
- Обновить `BoardPresenterTests.cs` — валидация матчей при свапе

**Критерий готовности:** Свап работает, невалидный свап откатывается

---

### Step 7: Destroy + Animation
**Цель:** Уничтожение матчей с анимацией

**Файлы:**
- Обновить `BoardModel.cs` (RemoveElements batch)
- Обновить `BoardPresenter.cs` (destroy coordination)
- Обновить `ElementView.cs` (destroy animation: scale+fade)

**Зависимости:** Step 3, Step 6

**Тесты:**
- Обновить `BoardPresenterTests.cs` — destroy вызывается для матчей

**Критерий готовности:** Матчи уничтожаются с анимацией

---

### Step 8: Fall Logic
**Цель:** Падение элементов вниз

**Файлы:**
- `Features/Board/Services/IFallService.cs`
- `Features/Board/Services/FallService.cs`
- Обновить `BoardModel.cs` (MoveElement)
- Обновить `BoardPresenter.cs` (fall coordination)
- Обновить `BoardView.cs` (fall animation)

**Зависимости:** Step 7

**Тесты:**
- `FallServiceTests.cs` — расчёт падений

**Критерий готовности:** Элементы падают на пустые места

---

### Step 9: Refill + Cascade
**Цель:** Спаун новых элементов + каскадная проверка

**Файлы:**
- Обновить `BoardPresenter.cs` (refill + cascade loop)
- Обновить `BoardView.cs` (spawn animation)

**Зависимости:** Step 2 (SpawnService), Step 8 (Fall)

**Тесты:**
- Обновить `BoardPresenterTests.cs` — каскады

**Критерий готовности:** Новые элементы спаунятся, каскады работают

---

### Step 10: GameplayCoordinator Integration
**Цель:** Полный Game Loop

**Файлы:**
- `Gameplay/GameState.cs`
- `Gameplay/GameplayCoordinator.cs`
- `Core/GameEntryPoint.cs`
- `Editor/Setup/SceneSetup.cs`

**Зависимости:** Step 1-9

**Тесты:**
- `GameplayCoordinatorTests.cs` — state transitions

**Критерий готовности:** Полный цикл работает: Input → Swap → Match → Destroy → Fall → Refill → Input

---

## Точки интеграции

| Компонент A | Компонент B | Интеграция через |
|-------------|-------------|------------------|
| BoardModel | BoardPresenter | События OnElement* |
| BoardPresenter | IBoardView | Интерфейс IBoardView |
| InputService | GameplayCoordinator | Событие OnSwapRequested |
| MatchService | GameplayCoordinator | Метод FindAllMatches |
| FallService | GameplayCoordinator | Метод CalculateFalls |
| SpawnService | GameplayCoordinator | Метод GetRandomElement* |
| GameConfig | Все компоненты | ServiceLocator (IGameConfig) |

---

## GameConfig (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Grid")]
    public int GridWidth = 8;
    public int GridHeight = 8;

    [Header("Elements")]
    public Sprite[] ElementSprites; // index = ElementType value
    public int ElementTypeCount = 5;

    [Header("Animations")]
    public float SwapDuration = 0.3f;
    public float FallDuration = 0.2f;
    public float DestroyDuration = 0.2f;
    public float SpawnDelay = 0.1f;

    [Header("Matching")]
    public int MinMatchLength = 3;
}
```

---

## Валидация

После каждого шага:
1. `run_tests(testMode: "EditMode")` — все тесты проходят
2. `read_console` — нет ошибок компиляции
3. В Unity: Menu → Setup → Step N (если применимо)

**Финальная валидация:**
1. Запуск игры в Unity Editor
2. Drag & Drop элементов
3. Матчи находятся и уничтожаются
4. Элементы падают
5. Новые спаунятся
6. Каскады работают
7. Input блокируется во время анимаций
