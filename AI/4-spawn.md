# Phase 4: Spawn System — Реализация

## Статус: ГОТОВО

## Обзор

Система спауна элементов. Заполнение сетки БЕЗ начальных матчей (3+ в ряд).

```
Assets/Scripts/
├── Spawn/
│   ├── ISpawnStrategy.cs        # Интерфейс стратегии
│   ├── NoMatchSpawnStrategy.cs  # Спаун без матчей
│   └── SpawnController.cs       # MonoBehaviour контроллер
└── Game/
    └── GameBootstrap.cs         # Entry point
```

**Зависимости:** GridData, GridConfig, ElementFactory, GridPositionConverter

---

## Реализованные файлы

### 4.1 ISpawnStrategy.cs

```csharp
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Spawn
{
    public interface ISpawnStrategy
    {
        ElementType GetElementType(GridPosition position, GridData grid, GridConfig config);
    }
}
```

**Назначение:** Контракт для стратегий выбора типа элемента.

**Почему интерфейс:**
- Тестирование: детерминированная стратегия для тестов
- Расширяемость: weighted spawn, tutorial spawn
- DIP: SpawnController зависит от абстракции

---

### 4.2 NoMatchSpawnStrategy.cs

```csharp
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;
using UnityEngine;

namespace Match3.Spawn
{
    public class NoMatchSpawnStrategy : ISpawnStrategy
    {
        private readonly List<ElementType> _availableTypes = new();

        public ElementType GetElementType(GridPosition position, GridData grid, GridConfig config)
        {
            _availableTypes.Clear();

            foreach (var type in config.ElementTypes)
            {
                if (!WouldCreateMatch(position, type, grid))
                {
                    _availableTypes.Add(type);
                }
            }

            if (_availableTypes.Count == 0)
            {
                return config.ElementTypes[Random.Range(0, config.ElementTypes.Count)];
            }

            return _availableTypes[Random.Range(0, _availableTypes.Count)];
        }

        private bool WouldCreateMatch(GridPosition pos, ElementType type, GridData grid)
        {
            return CheckHorizontalMatch(pos, type, grid) || CheckVerticalMatch(pos, type, grid);
        }

        private bool CheckHorizontalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            var left1 = grid.GetElement(pos + GridPosition.Left);
            var left2 = grid.GetElement(pos + GridPosition.Left + GridPosition.Left);

            return left1 != null && left2 != null && left1.Type == type && left2.Type == type;
        }

        private bool CheckVerticalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            var down1 = grid.GetElement(pos + GridPosition.Down);
            var down2 = grid.GetElement(pos + GridPosition.Down + GridPosition.Down);

            return down1 != null && down2 != null && down1.Type == type && down2.Type == type;
        }
    }
}
```

### Алгоритм

При заполнении сетки снизу-вверх, слева-направо:
1. Получить список всех доступных типов
2. Отфильтровать типы, которые создадут матч (проверка 2 слева + 2 снизу)
3. Выбрать случайный из оставшихся

```
Проверка для позиции (x, y):

Горизонталь:         Вертикаль:
[?][?][NEW]          [NEW]
                     [?]
                     [?]

Если (x-1) и (x-2) того же типа → исключить
Если (y-1) и (y-2) того же типа → исключить
```

### Edge Cases

| Ситуация | Решение |
|----------|---------|
| Позиция (0, y) или (1, y) | GetElement вернёт null → проверка пройдёт |
| Позиция (x, 0) или (x, 1) | GetElement вернёт null → проверка пройдёт |
| Все типы создают матч | Fallback на случайный (при 5 типах невозможно) |

---

### 4.3 SpawnController.cs

```csharp
using System;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Spawn
{
    public class SpawnController : MonoBehaviour
    {
        public event Action OnFillComplete;
        public event Action<int> OnSpawnedInColumn;

        [SerializeField] private GridView _gridView;
        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private GridConfig _config;
        private GridPositionConverter _converter;
        private ISpawnStrategy _strategy;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _config = _gridView.Config;
            _converter = _gridView.PositionConverter;
            _strategy = new NoMatchSpawnStrategy();
        }

        public void SetStrategy(ISpawnStrategy strategy)
        {
            _strategy = strategy ?? new NoMatchSpawnStrategy();
        }

        public void FillGrid()
        {
            for (int y = 0; y < _config.Height; y++)
            {
                for (int x = 0; x < _config.Width; x++)
                {
                    var pos = new GridPosition(x, y);
                    if (_grid.GetElement(pos) != null) continue;
                    SpawnElement(pos);
                }
            }

            OnFillComplete?.Invoke();
        }

        public IElement SpawnAtTop(int column, int offsetAboveGrid = 1)
        {
            var gridPos = new GridPosition(column, _config.Height - 1);
            var spawnWorldPos = _converter.GridToWorld(
                new GridPosition(column, _config.Height - 1 + offsetAboveGrid)
            );

            var type = _strategy.GetElementType(gridPos, _grid, _config);
            var element = _factory.CreateElement(type, gridPos, spawnWorldPos);

            OnSpawnedInColumn?.Invoke(column);
            return element;
        }

        private void SpawnElement(GridPosition pos)
        {
            var worldPos = _converter.GridToWorld(pos);
            var type = _strategy.GetElementType(pos, _grid, _config);
            var element = _factory.CreateElement(type, pos, worldPos);
            _grid.SetElement(pos, element);
        }
    }
}
```

### API

```csharp
// События
event Action OnFillComplete;              // Начальное заполнение завершено
event Action<int> OnSpawnedInColumn;      // Заспаунен элемент в колонке

// Методы
void Initialize(GridData grid);           // Инициализация с данными сетки
void FillGrid();                          // Начальное заполнение (без анимации)
IElement SpawnAtTop(int column, int offset); // Спаун сверху колонки (для Gravity)
void SetStrategy(ISpawnStrategy strategy); // Смена стратегии
```

---

### 4.4 GameBootstrap.cs

```csharp
using Match3.Grid;
using Match3.Spawn;
using UnityEngine;

namespace Match3.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GridView _gridView;
        [SerializeField] private SpawnController _spawnController;

        private GridData _gridData;

        private void Start()
        {
            var config = _gridView.Config;
            _gridData = new GridData(config.Width, config.Height);

            _gridView.CreateVisualGrid();

            _spawnController.Initialize(_gridData);
            _spawnController.OnFillComplete += OnGridFilled;
            _spawnController.FillGrid();
        }

        private void OnGridFilled()
        {
            Debug.Log("[Match3] Grid filled without initial matches!");
        }
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
└── GameBootstrap           [GameBootstrap]
```

### Автоматическая настройка:

```
Unity Menu → Match3 → Setup Scene
```

Editor script `Match3SceneSetup.cs` автоматически создаёт все объекты и связывает зависимости.

### Ручная настройка:

1. **SpawnController:**
   - `_gridView` → Grid
   - `_factory` → ElementFactory

2. **GameBootstrap:**
   - `_gridView` → Grid
   - `_spawnController` → SpawnController

---

## Интеграция

### Использует из Phase 1-3:

| Компонент | Назначение |
|-----------|-----------|
| `GridPosition` | Координаты, направления (Left, Down) |
| `GridData` | GetElement, SetElement |
| `GridConfig` | ElementTypes[], Width, Height |
| `GridPositionConverter` | GridToWorld |
| `ElementFactory` | CreateElement |
| `GridView` | Config, PositionConverter |

### Предоставляет для Phase 5+:

| Метод | Использует |
|-------|-----------|
| `SpawnController.FillGrid()` | GameBootstrap (начальное заполнение) |
| `SpawnController.SpawnAtTop()` | GravityController (Phase 6) |
| `ISpawnStrategy` | Точка расширения |

---

## Будущее использование

### В GravityController (Phase 6):

```csharp
// После падения элементов — спаун новых сверху
foreach (int column in columnsWithEmptyTop)
{
    var newElement = _spawnController.SpawnAtTop(column, offsetAboveGrid: emptyCount);
    // Элемент создан выше сетки, анимируем падение...
}
```

---

## Возможные расширения

### WeightedSpawnStrategy
```csharp
// Разные веса для разных типов (для уровней)
public class WeightedSpawnStrategy : ISpawnStrategy
{
    private Dictionary<ElementType, float> _weights;
}
```

### TutorialSpawnStrategy
```csharp
// Предопределённое заполнение для обучения
public class TutorialSpawnStrategy : ISpawnStrategy
{
    private ElementType[,] _predefinedGrid;
}
```

---

## Checklist

- [x] Создать папку `Assets/Scripts/Spawn/`
- [x] Реализовать `ISpawnStrategy.cs`
- [x] Реализовать `NoMatchSpawnStrategy.cs`
- [x] Реализовать `SpawnController.cs`
- [x] Создать `GameBootstrap.cs`
- [x] Обновить `Match3SceneSetup.cs`
- [ ] Тест в Unity: запустить сцену, проверить отсутствие начальных матчей

---

## Следующие шаги

**Phase 5: Match Detection** — поиск линий 3+ элементов
- `IMatchFinder` — интерфейс
- `LineMatchFinder` — поиск горизонтальных/вертикальных линий
- `MatchResult` — данные о найденном матче
