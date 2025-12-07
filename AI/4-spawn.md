# Phase 4: Spawn System — Детальный План Реализации

## Обзор

Система спауна элементов. Ключевая задача — заполнение сетки БЕЗ начальных матчей (3+ в ряд).

```
Assets/Scripts/Spawn/
├── ISpawnStrategy.cs        # Интерфейс стратегии (~15 строк)
├── NoMatchSpawnStrategy.cs  # Спаун без матчей (~80 строк)
└── SpawnController.cs       # MonoBehaviour контроллер (~90 строк)
```

**Зависимости:** GridData, GridConfig, ElementFactory, GridPositionConverter

---

## 4.1 ISpawnStrategy (Interface)

**Файл:** `Assets/Scripts/Spawn/ISpawnStrategy.cs`

Контракт для стратегий выбора типа элемента. Позволяет легко подменять логику (тесты, разные режимы игры).

```csharp
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Spawn
{
    public interface ISpawnStrategy
    {
        /// <summary>
        /// Выбирает тип элемента для указанной позиции.
        /// </summary>
        /// <param name="position">Позиция на сетке</param>
        /// <param name="grid">Текущее состояние сетки</param>
        /// <param name="config">Конфиг с доступными типами</param>
        /// <returns>Выбранный тип элемента</returns>
        ElementType GetElementType(GridPosition position, GridData grid, GridConfig config);
    }
}
```

**Почему интерфейс:**
- Тестирование: можно создать детерминированную стратегию для тестов
- Расширяемость: weighted spawn, level-specific spawn, tutorial spawn
- DIP: SpawnController зависит от абстракции

---

## 4.2 NoMatchSpawnStrategy (Pure C# Class)

**Файл:** `Assets/Scripts/Spawn/NoMatchSpawnStrategy.cs`

Выбирает случайный тип, гарантируя отсутствие матча при размещении.

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

### Реализация

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

            // Fallback: если все типы создают матч (редкий edge case) — берём случайный
            if (_availableTypes.Count == 0)
            {
                return config.ElementTypes[Random.Range(0, config.ElementTypes.Count)];
            }

            return _availableTypes[Random.Range(0, _availableTypes.Count)];
        }

        private bool WouldCreateMatch(GridPosition pos, ElementType type, GridData grid)
        {
            return CheckHorizontalMatch(pos, type, grid) ||
                   CheckVerticalMatch(pos, type, grid);
        }

        private bool CheckHorizontalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            // Проверяем 2 элемента слева
            var left1 = grid.GetElement(pos + GridPosition.Left);
            var left2 = grid.GetElement(pos + GridPosition.Left + GridPosition.Left);

            return left1 != null && left2 != null &&
                   left1.Type == type && left2.Type == type;
        }

        private bool CheckVerticalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            // Проверяем 2 элемента снизу
            var down1 = grid.GetElement(pos + GridPosition.Down);
            var down2 = grid.GetElement(pos + GridPosition.Down + GridPosition.Down);

            return down1 != null && down2 != null &&
                   down1.Type == type && down2.Type == type;
        }
    }
}
```

### Edge Cases

| Ситуация | Решение |
|----------|---------|
| Позиция (0, y) или (1, y) | Нет 2 элементов слева → горизонталь не проверяется |
| Позиция (x, 0) или (x, 1) | Нет 2 элементов снизу → вертикаль не проверяется |
| Все типы создают матч | Fallback на случайный (теоретически при 5 типах невозможно) |

---

## 4.3 SpawnController (MonoBehaviour)

**Файл:** `Assets/Scripts/Spawn/SpawnController.cs`

Контроллер спауна. Связывает стратегию, фабрику и сетку.

### Inspector поля

```csharp
[SerializeField] private GridView _gridView;           // Для конфига и конвертера
[SerializeField] private ElementFactory _factory;      // Создание элементов
```

### API

```csharp
// События
event Action OnFillComplete;              // Начальное заполнение завершено
event Action<int> OnSpawnedInColumn;      // Заспаунен элемент в колонке (для анимации)

// Методы
void Initialize(GridData grid);           // Инициализация с данными сетки
void FillGrid();                          // Начальное заполнение (без анимации)
IElement SpawnAtTop(int column);          // Спаун сверху колонки (для Gravity)
void SetStrategy(ISpawnStrategy strategy); // Смена стратегии (опционально)
```

### Реализация

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

        /// <summary>
        /// Заполняет всю сетку элементами. Без анимации — мгновенно.
        /// Заполнение идёт снизу-вверх, слева-направо для корректной работы NoMatchSpawnStrategy.
        /// </summary>
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

        /// <summary>
        /// Спаунит элемент сверху колонки. Используется GravityController.
        /// Элемент создаётся выше видимой области для анимации падения.
        /// </summary>
        /// <param name="column">Индекс колонки (0 to Width-1)</param>
        /// <param name="offsetAboveGrid">На сколько ячеек выше сетки создать</param>
        /// <returns>Созданный элемент</returns>
        public IElement SpawnAtTop(int column, int offsetAboveGrid = 1)
        {
            // Позиция в сетке — верхняя ячейка колонки
            var gridPos = new GridPosition(column, _config.Height - 1);

            // World позиция — выше сетки для анимации падения
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

---

## Интеграция с существующим кодом

### Что нужно от Phase 1-3:

| Компонент | Используется для |
|-----------|-----------------|
| `GridPosition` | Координаты, направления (Left, Down) |
| `GridData` | Хранение элементов, GetElement, SetElement |
| `GridConfig` | ElementTypes[], Width, Height |
| `GridPositionConverter` | GridToWorld для позиционирования |
| `ElementFactory` | CreateElement(type, pos, worldPos) |
| `GridView` | Доступ к Config и PositionConverter |

### Что предоставляет для Phase 5+:

| Компонент | Предоставляет |
|-----------|--------------|
| `SpawnController.FillGrid()` | Начальное заполнение без матчей |
| `SpawnController.SpawnAtTop()` | Создание элементов для GravityController |
| `ISpawnStrategy` | Точка расширения для weighted spawn |

---

## Scene Setup

### Иерархия объектов (дополнение):

```
Scene
├── Grid                    [GridView]
├── ElementPool             [ElementPool]
├── Elements                (parent)
├── ElementFactory          [ElementFactory]
└── SpawnController         [SpawnController]  ← NEW
```

### Настройка SpawnController:

1. Создать пустой GameObject "SpawnController"
2. Добавить компонент `SpawnController`
3. В Inspector назначить:
   - `_gridView` → Grid объект
   - `_factory` → ElementFactory объект

---

## Пример использования

### GameBootstrap (обновлённый):

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GridView _gridView;
    [SerializeField] private SpawnController _spawnController;

    private GridData _gridData;

    void Start()
    {
        // 1. Создать данные сетки
        var config = _gridView.Config;
        _gridData = new GridData(config.Width, config.Height);

        // 2. Создать визуальную сетку (фон)
        _gridView.CreateVisualGrid();

        // 3. Инициализировать спаун
        _spawnController.Initialize(_gridData);
        _spawnController.OnFillComplete += OnGridFilled;

        // 4. Заполнить сетку элементами
        _spawnController.FillGrid();
    }

    private void OnGridFilled()
    {
        Debug.Log("Grid filled without initial matches!");
        // Здесь можно запустить игровой цикл
    }
}
```

### Будущее использование в GravityController:

```csharp
// После падения элементов — спаун новых сверху
foreach (int column in columnsWithEmptyTop)
{
    var newElement = _spawnController.SpawnAtTop(column, offsetAboveGrid: emptyCount);
    // newElement уже создан выше сетки, анимируем падение...
}
```

---

## Тестирование

### Unit-тесты для NoMatchSpawnStrategy:

```csharp
[Test]
public void GetElementType_WithTwoSameLeft_ExcludesThatType()
{
    // Arrange
    var grid = new GridData(8, 8);
    var redType = CreateElementType("red");

    // Размещаем 2 красных слева от (2, 0)
    grid.SetElement(new GridPosition(0, 0), CreateMockElement(redType));
    grid.SetElement(new GridPosition(1, 0), CreateMockElement(redType));

    var strategy = new NoMatchSpawnStrategy();
    var config = CreateConfig(redType, blueType, greenType);

    // Act — вызываем много раз
    for (int i = 0; i < 100; i++)
    {
        var result = strategy.GetElementType(new GridPosition(2, 0), grid, config);

        // Assert — никогда не должен вернуть красный
        Assert.AreNotEqual(redType, result);
    }
}

[Test]
public void GetElementType_WithTwoSameBelow_ExcludesThatType()
{
    // Аналогично для вертикали
}

[Test]
public void GetElementType_AtCorner_WorksWithoutErrors()
{
    // Проверка edge case: позиция (0, 0) — нет соседей слева и снизу
}
```

### Integration-тест:

```csharp
[Test]
public void FillGrid_ProducesNoInitialMatches()
{
    // Arrange
    var grid = new GridData(8, 8);
    var spawnController = CreateSpawnController(grid);

    // Act
    spawnController.FillGrid();

    // Assert — проверяем все позиции
    for (int x = 0; x < 8; x++)
    {
        for (int y = 0; y < 8; y++)
        {
            var pos = new GridPosition(x, y);
            Assert.IsFalse(HasMatchAt(grid, pos), $"Match found at {pos}");
        }
    }
}
```

---

## Возможные расширения (не реализуем сейчас)

### WeightedSpawnStrategy
```csharp
// Разные веса для разных типов (например, для уровней)
public class WeightedSpawnStrategy : ISpawnStrategy
{
    private Dictionary<ElementType, float> _weights;
    // ...
}
```

### TutorialSpawnStrategy
```csharp
// Предопределённое заполнение для обучения
public class TutorialSpawnStrategy : ISpawnStrategy
{
    private ElementType[,] _predefinedGrid;
    // ...
}
```

---

## Checklist реализации

- [ ] Создать папку `Assets/Scripts/Spawn/`
- [ ] Реализовать `ISpawnStrategy.cs`
- [ ] Реализовать `NoMatchSpawnStrategy.cs`
- [ ] Реализовать `SpawnController.cs`
- [ ] Добавить SpawnController на сцену
- [ ] Настроить зависимости в Inspector
- [ ] Создать GameBootstrap или обновить существующий
- [ ] Тест: запустить сцену, убедиться что нет начальных матчей
- [ ] (Опционально) Написать unit-тесты

---

## Следующие шаги

**Phase 5: Match Detection** — поиск линий 3+ элементов
- `IMatchFinder` — интерфейс
- `LineMatchFinder` — поиск горизонтальных/вертикальных линий
- `MatchResult` — данные о найденном матче
