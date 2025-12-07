# Phase 5: Match Detection — Реализация

## Статус: ✅ ГОТОВО

## Обзор

Система поиска матчей. Находит горизонтальные и вертикальные линии 3+ элементов одного типа.

```
Assets/Scripts/
├── Data/
│   └── MatchData.cs              # Данные о матче
└── Match/
    ├── IMatchFinder.cs           # Интерфейс поиска
    ├── LineMatchFinder.cs        # Поиск линий 3+
    └── MatchController.cs        # MonoBehaviour контроллер
```

**Решения:**
- Только линии 3+ (без L/T форм)
- Без бонусов за 4/5 в ряд (можно добавить позже)
- Пересекающиеся линии — отдельные матчи

**Зависимости:** GridData, GridPosition, IElement, ElementType

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                     MatchController                         │
│                    (MonoBehaviour)                          │
│  • Точка входа для других систем                           │
│  • События: OnMatchesFound, OnNoMatches                    │
└─────────────────────┬───────────────────────────────────────┘
                      │ использует
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                     IMatchFinder                            │
│                     (Interface)                             │
│  • FindAllMatches(grid)                                    │
│  • FindMatchesAt(grid, positions)                          │
└─────────────────────┬───────────────────────────────────────┘
                      │ реализует
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   LineMatchFinder                           │
│                    (Pure C#)                                │
│  • Поиск горизонтальных линий                              │
│  • Поиск вертикальных линий                                │
│  • Расширение от точки в обе стороны                       │
└─────────────────────────────────────────────────────────────┘
```

**Unity Way принципы:**
- `LineMatchFinder` — Pure C#, легко тестировать
- `MatchController` — MonoBehaviour, точка входа
- Event-driven коммуникация
- Interface для возможности замены алгоритма

---

## Реализованные файлы

### 5.1 MatchData.cs

**Файл:** `Assets/Scripts/Data/MatchData.cs`

Immutable структура данных о найденном матче.

```csharp
using System.Collections.Generic;
using Match3.Core;

namespace Match3.Data
{
    public readonly struct MatchData
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public ElementType Type { get; }
        public bool IsHorizontal { get; }
        public int Length => Positions.Count;

        public MatchData(IReadOnlyList<GridPosition> positions, ElementType type, bool isHorizontal)
        {
            Positions = positions;
            Type = type;
            IsHorizontal = isHorizontal;
        }
    }
}
```

**Почему readonly struct:**
- Value type — нет аллокаций
- Immutable — безопасно передавать
- Содержит только данные

**Поля:**
| Поле | Тип | Описание |
|------|-----|----------|
| `Positions` | `IReadOnlyList<GridPosition>` | Позиции элементов в матче |
| `Type` | `ElementType` | Тип элементов |
| `IsHorizontal` | `bool` | true = горизонталь, false = вертикаль |
| `Length` | `int` | Количество элементов (3+) |

---

### 5.2 IMatchFinder.cs

**Файл:** `Assets/Scripts/Match/IMatchFinder.cs`

Интерфейс для алгоритмов поиска матчей.

```csharp
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Match
{
    public interface IMatchFinder
    {
        /// <summary>
        /// Поиск всех матчей на сетке.
        /// Используется после каскадов для полной проверки.
        /// </summary>
        List<MatchData> FindAllMatches(GridData grid);

        /// <summary>
        /// Поиск матчей только в указанных позициях.
        /// Используется после свапа для оптимизации.
        /// </summary>
        List<MatchData> FindMatchesAt(GridData grid, IEnumerable<GridPosition> positions);
    }
}
```

**Два метода:**
1. `FindAllMatches` — полный скан сетки (после gravity)
2. `FindMatchesAt` — проверка конкретных позиций (после swap)

**Зачем интерфейс:**
- Можно заменить алгоритм (Pattern matcher, Shape matcher)
- Легко мокать в тестах
- DIP — контроллер зависит от абстракции

---

### 5.3 LineMatchFinder.cs

**Файл:** `Assets/Scripts/Match/LineMatchFinder.cs`

Pure C# класс. Основной алгоритм поиска линий.

```csharp
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Match
{
    public class LineMatchFinder : IMatchFinder
    {
        private const int MinMatchLength = 3;

        private readonly HashSet<GridPosition> _visitedH = new();
        private readonly HashSet<GridPosition> _visitedV = new();
        private readonly List<GridPosition> _buffer = new();

        public List<MatchData> FindAllMatches(GridData grid)
        {
            var matches = new List<MatchData>();
            _visitedH.Clear();
            _visitedV.Clear();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    CheckPosition(grid, new GridPosition(x, y), matches);
                }
            }

            return matches;
        }

        public List<MatchData> FindMatchesAt(GridData grid, IEnumerable<GridPosition> positions)
        {
            var matches = new List<MatchData>();
            _visitedH.Clear();
            _visitedV.Clear();

            foreach (var pos in positions)
            {
                CheckPosition(grid, pos, matches);
            }

            return matches;
        }

        private void CheckPosition(GridData grid, GridPosition pos, List<MatchData> matches)
        {
            var element = grid.GetElement(pos);
            if (element == null) return;

            // Horizontal
            if (!_visitedH.Contains(pos))
            {
                var line = GetLine(grid, pos, GridPosition.Left, GridPosition.Right, element.Type);
                if (line.Count >= MinMatchLength)
                {
                    matches.Add(new MatchData(line.ToArray(), element.Type, true));
                    MarkVisited(_visitedH, line);
                }
            }

            // Vertical
            if (!_visitedV.Contains(pos))
            {
                var line = GetLine(grid, pos, GridPosition.Down, GridPosition.Up, element.Type);
                if (line.Count >= MinMatchLength)
                {
                    matches.Add(new MatchData(line.ToArray(), element.Type, false));
                    MarkVisited(_visitedV, line);
                }
            }
        }

        private List<GridPosition> GetLine(GridData grid, GridPosition start, GridPosition negDir, GridPosition posDir, ElementType type)
        {
            _buffer.Clear();
            _buffer.Add(start);

            Extend(grid, start, negDir, type);
            Extend(grid, start, posDir, type);

            return _buffer;
        }

        private void Extend(GridData grid, GridPosition start, GridPosition dir, ElementType type)
        {
            var current = start + dir;
            while (grid.IsValidPosition(current))
            {
                var el = grid.GetElement(current);
                if (el == null || el.Type != type) break;
                _buffer.Add(current);
                current = current + dir;
            }
        }

        private void MarkVisited(HashSet<GridPosition> visited, List<GridPosition> positions)
        {
            foreach (var pos in positions)
                visited.Add(pos);
        }
    }
}
```

### Алгоритм

```
Для каждой позиции (x, y):
1. Получить элемент
2. Если не посещена по горизонтали:
   a. Расширить влево пока тот же тип
   b. Расширить вправо пока тот же тип
   c. Если длина >= 3 → добавить матч
3. Если не посещена по вертикали:
   a. Расширить вниз пока тот же тип
   b. Расширить вверх пока тот же тип
   c. Если длина >= 3 → добавить матч
```

**Визуализация:**

```
Исходная сетка:        Поиск от (2,1):
  0 1 2 3 4
0 R B G R B            Горизонталь: [R R R] ✓
1 R R R R G      →     Вертикаль: [R R] ✗
2 G B R G R
3 B G B R B

Результат: MatchData {
  Positions: [(1,1), (2,1), (3,1)],
  Type: Red,
  IsHorizontal: true
}
```

### Оптимизации

1. **HashSet для visited** — O(1) проверка, избегаем дубликатов
2. **Два отдельных visited** — позиция может быть в двух матчах (пересечение)
3. **Buffer для линии** — переиспользуем List, меньше аллокаций
4. **FindMatchesAt** — проверяем только затронутые позиции после свапа

### Сложность

| Метод | Время | Память |
|-------|-------|--------|
| `FindAllMatches` | O(W × H) | O(W × H) для visited |
| `FindMatchesAt` | O(N × MaxLineLength) | O(N) |

---

### 5.4 MatchController.cs

**Файл:** `Assets/Scripts/Match/MatchController.cs`

MonoBehaviour контроллер. Точка входа для других систем.

```csharp
using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;
using UnityEngine;

namespace Match3.Match
{
    public class MatchController : MonoBehaviour
    {
        public event Action<List<MatchData>> OnMatchesFound;
        public event Action OnNoMatches;

        private GridData _grid;
        private IMatchFinder _matchFinder;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _matchFinder = new LineMatchFinder();
        }

        public void SetMatchFinder(IMatchFinder finder)
        {
            _matchFinder = finder ?? new LineMatchFinder();
        }

        /// <summary>
        /// Проверить всю сетку на матчи.
        /// </summary>
        public List<MatchData> CheckAll()
        {
            var matches = _matchFinder.FindAllMatches(_grid);
            NotifyResults(matches);
            return matches;
        }

        /// <summary>
        /// Проверить матчи только в указанных позициях (после свапа).
        /// </summary>
        public List<MatchData> CheckAt(GridPosition posA, GridPosition posB)
        {
            var positions = new[] { posA, posB };
            var matches = _matchFinder.FindMatchesAt(_grid, positions);
            NotifyResults(matches);
            return matches;
        }

        /// <summary>
        /// Проверить матчи в списке позиций (после gravity).
        /// </summary>
        public List<MatchData> CheckAt(IEnumerable<GridPosition> positions)
        {
            var matches = _matchFinder.FindMatchesAt(_grid, positions);
            NotifyResults(matches);
            return matches;
        }

        /// <summary>
        /// Проверить без событий (для валидации свапа).
        /// </summary>
        public bool HasMatchAt(GridPosition posA, GridPosition posB)
        {
            var positions = new[] { posA, posB };
            var matches = _matchFinder.FindMatchesAt(_grid, positions);
            return matches.Count > 0;
        }

        private void NotifyResults(List<MatchData> matches)
        {
            if (matches.Count > 0)
            {
                OnMatchesFound?.Invoke(matches);
            }
            else
            {
                OnNoMatches?.Invoke();
            }
        }
    }
}
```

### API

```csharp
// События
event Action<List<MatchData>> OnMatchesFound;  // Найдены матчи
event Action OnNoMatches;                       // Матчей нет

// Инициализация
void Initialize(GridData grid);
void SetMatchFinder(IMatchFinder finder);

// Методы проверки
List<MatchData> CheckAll();                              // Вся сетка
List<MatchData> CheckAt(GridPosition a, GridPosition b); // После свапа
List<MatchData> CheckAt(IEnumerable<GridPosition> pos);  // После gravity
bool HasMatchAt(GridPosition a, GridPosition b);         // Валидация (без событий)
```

---

## Интеграция

### Использует из Phase 1-4:

| Компонент | Назначение |
|-----------|-----------|
| `GridPosition` | Координаты, направления |
| `GridData` | GetElement, IsValidPosition, Width, Height |
| `IElement` | Type для сравнения |
| `ElementType` | Сравнение типов элементов |

### Предоставляет для Phase 6+:

| Метод/Событие | Использует |
|---------------|-----------|
| `CheckAll()` | GameStateMachine (после gravity) |
| `CheckAt()` | SwapController (после свапа) |
| `HasMatchAt()` | SwapValidator (проверка валидности свапа) |
| `OnMatchesFound` | DestructionController (Phase 8) |
| `OnNoMatches` | GameStateMachine (переход в Idle) |

### Пример использования в SwapController:

```csharp
public class SwapController : MonoBehaviour
{
    [SerializeField] private MatchController _matchController;

    public void TrySwap(GridPosition a, GridPosition b)
    {
        // Временный свап для проверки
        _grid.SwapElements(a, b);

        if (_matchController.HasMatchAt(a, b))
        {
            // Валидный свап — запустить анимацию
            AnimateSwap(a, b, onComplete: () =>
            {
                _matchController.CheckAt(a, b); // Fire events
            });
        }
        else
        {
            // Невалидный — откатить
            _grid.SwapElements(a, b);
            AnimateInvalidSwap(a, b);
        }
    }
}
```

### Пример использования в GameStateMachine:

```csharp
public class GameStateMachine : MonoBehaviour
{
    [SerializeField] private MatchController _matchController;
    [SerializeField] private DestructionController _destructionController;

    private void OnEnable()
    {
        _matchController.OnMatchesFound += OnMatchesFound;
        _matchController.OnNoMatches += OnNoMatches;
    }

    private void OnMatchesFound(List<MatchData> matches)
    {
        SetState(GameState.Destroying);
        _destructionController.DestroyMatches(matches);
    }

    private void OnNoMatches()
    {
        SetState(GameState.Idle);
    }
}
```

---

## Scene Setup

### Иерархия объектов:

```
Scene
├── Main Camera
├── Grid                    [GridView]
│   └── Cells
├── ElementPool             [ElementPool]
├── Elements                (parent)
├── ElementFactory          [ElementFactory]
├── SpawnController         [SpawnController]
├── MatchController         [MatchController]    ← NEW
└── GameBootstrap           [GameBootstrap]
```

### Обновление GameBootstrap:

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GridView _gridView;
    [SerializeField] private SpawnController _spawnController;
    [SerializeField] private MatchController _matchController;  // NEW

    private GridData _gridData;

    private void Start()
    {
        var config = _gridView.Config;
        _gridData = new GridData(config.Width, config.Height);

        _gridView.CreateVisualGrid();

        _spawnController.Initialize(_gridData);
        _matchController.Initialize(_gridData);  // NEW

        _spawnController.OnFillComplete += OnGridFilled;
        _spawnController.FillGrid();
    }

    private void OnGridFilled()
    {
        // Проверка что нет начальных матчей
        var matches = _matchController.CheckAll();
        Debug.Log($"[Match3] Initial matches: {matches.Count}");
    }
}
```

---

## Тестирование

### Unit Tests (Pure C#):

```csharp
[Test]
public void FindsHorizontalMatch()
{
    var grid = new GridData(5, 5);
    var redType = CreateElementType("red");

    // Создаём горизонтальную линию
    grid.SetElement(new GridPosition(0, 0), CreateElement(redType));
    grid.SetElement(new GridPosition(1, 0), CreateElement(redType));
    grid.SetElement(new GridPosition(2, 0), CreateElement(redType));

    var finder = new LineMatchFinder();
    var matches = finder.FindAllMatches(grid);

    Assert.AreEqual(1, matches.Count);
    Assert.AreEqual(3, matches[0].Length);
    Assert.IsTrue(matches[0].IsHorizontal);
}

[Test]
public void FindsIntersectingMatchesAsSeparate()
{
    // Крест из 5 элементов = 2 матча
    var grid = CreateCrossPattern();
    var finder = new LineMatchFinder();

    var matches = finder.FindAllMatches(grid);

    Assert.AreEqual(2, matches.Count);  // Горизонталь + Вертикаль
}

[Test]
public void DoesNotFindMatchOfTwo()
{
    var grid = new GridData(5, 5);
    var redType = CreateElementType("red");

    grid.SetElement(new GridPosition(0, 0), CreateElement(redType));
    grid.SetElement(new GridPosition(1, 0), CreateElement(redType));

    var finder = new LineMatchFinder();
    var matches = finder.FindAllMatches(grid);

    Assert.AreEqual(0, matches.Count);
}
```

### Manual Testing в Unity:

1. Запустить сцену
2. NoMatchSpawnStrategy должен исключить начальные матчи → `matches.Count = 0`
3. (Временно) Заменить на RandomSpawnStrategy → должны появиться матчи
4. Проверить в консоли логи матчей

---

## Edge Cases

| Ситуация | Поведение |
|----------|-----------|
| Пустая ячейка в середине | Линия прерывается |
| Линия 5+ элементов | Один матч с 5 позициями |
| Крест (пересечение) | Два отдельных матча |
| Угол сетки | Корректно обрабатывается через IsValidPosition |
| Вся строка одного типа | Один горизонтальный матч |

---

## Checklist

- [x] Создать папку `Assets/Scripts/Match/`
- [x] Создать `Assets/Scripts/Data/MatchData.cs`
- [x] Создать `Assets/Scripts/Match/IMatchFinder.cs`
- [x] Создать `Assets/Scripts/Match/LineMatchFinder.cs`
- [x] Создать `Assets/Scripts/Match/MatchController.cs`
- [x] Добавить MatchController в сцену (Match3SceneSetup)
- [x] Обновить GameBootstrap
- [x] Тест: запустить сцену, проверить что начальных матчей нет
- [x] Тест: временно сломать NoMatchSpawnStrategy, убедиться что матчи находятся

---

## Следующие шаги

**Phase 6: Input & Swap System**
- `TouchInputHandler` — обработка ввода
- `SwapValidator` — проверка валидности свапа
- `SwapController` — анимация свапа + откат

---

## Возможные расширения (будущее)

### BonusMatchFinder (4+ элементов)
```csharp
// Определяет какой бонус создать
public enum BonusType { None, LineH, LineV, Bomb, Rainbow }

public class BonusMatchFinder : IMatchFinder
{
    public BonusType GetBonusType(MatchData match)
    {
        if (match.Length == 4) return match.IsHorizontal ? BonusType.LineV : BonusType.LineH;
        if (match.Length >= 5) return BonusType.Bomb;
        return BonusType.None;
    }
}
```

### ShapeMatchFinder (L/T формы)
```csharp
public class ShapeMatchFinder : IMatchFinder
{
    // Объединяет пересекающиеся линии в один матч
    // Определяет форму: L, T, или крест
}
```
