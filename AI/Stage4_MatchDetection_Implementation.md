# Этап 4: Match Detection — Подробный План Реализации

## Обзор

Match Detection — система поиска совпадений на игровом поле. Отвечает за:
- Поиск 3+ одинаковых элементов в ряд (горизонталь/вертикаль)
- Хранение данных о найденных матчах
- Объединение пересекающихся матчей (L-образные, T-образные)

**Принцип Unity Way:** MatchDetector — чистый C# класс без MonoBehaviour. Не имеет состояния между вызовами, получает GridComponent и возвращает результат. Можно легко тестировать.

---

## Архитектура

```
MatchData (plain class)      — данные одного матча
MatchDetector (plain class)  — алгоритм поиска
```

**Связи:**
```
MatchDetector.FindMatches(GridComponent) → List<MatchData>
                    ↓
            Итерация по Cell[,]
                    ↓
            Проверка Cell.Element.Type
```

**Поток данных:**
```
[GameLoop вызывает FindMatches]
              ↓
[MatchDetector сканирует сетку]
              ↓
[Находит горизонтальные матчи]
              ↓
[Находит вертикальные матчи]
              ↓
[Объединяет пересекающиеся]
              ↓
[Возвращает List<MatchData>]
```

---

## 4.1 MatchData (plain class)

### Назначение
Контейнер данных для одного матча. Хранит ячейки, тип элемента, направление.

### Путь файла
`Assets/Scripts/Match/MatchData.cs`

### Код

```csharp
using System.Collections.Generic;

public class MatchData
{
    public List<Cell> Cells { get; }
    public ElementType Type { get; }

    public int Length => Cells.Count;
    public bool IsHorizontal { get; private set; }
    public bool IsVertical { get; private set; }

    // L-образный или T-образный матч
    public bool IsSpecial => IsHorizontal && IsVertical;

    public MatchData(ElementType type)
    {
        Type = type;
        Cells = new List<Cell>();
    }

    public void AddCell(Cell cell)
    {
        if (!Cells.Contains(cell))
        {
            Cells.Add(cell);
        }
    }

    public void AddCells(IEnumerable<Cell> cells)
    {
        foreach (var cell in cells)
        {
            AddCell(cell);
        }
    }

    public bool ContainsCell(Cell cell) => Cells.Contains(cell);

    public bool ContainsCell(int x, int y)
    {
        foreach (var cell in Cells)
        {
            if (cell.X == x && cell.Y == y) return true;
        }
        return false;
    }

    public void SetHorizontal() => IsHorizontal = true;
    public void SetVertical() => IsVertical = true;

    /// <summary>
    /// Попытка объединить с другим матчем (если пересекаются и одного типа)
    /// </summary>
    public bool TryMerge(MatchData other)
    {
        if (other.Type != Type) return false;

        // Проверяем пересечение
        bool hasIntersection = false;
        foreach (var cell in other.Cells)
        {
            if (ContainsCell(cell))
            {
                hasIntersection = true;
                break;
            }
        }

        if (!hasIntersection) return false;

        // Объединяем
        AddCells(other.Cells);
        if (other.IsHorizontal) SetHorizontal();
        if (other.IsVertical) SetVertical();

        return true;
    }

    public override string ToString()
    {
        string dir = IsSpecial ? "L/T" : (IsHorizontal ? "H" : "V");
        return $"Match({Type}, {Length}, {dir})";
    }
}
```

### Примечания
- `Cells` как List, не HashSet — порядок может быть важен для анимаций
- `ContainsCell` проверка перед добавлением — избегаем дубликатов
- `TryMerge` — ключевой метод для объединения L/T-образных матчей

---

## 4.2 MatchDetector (plain class)

### Назначение
Алгоритм поиска всех матчей на сетке. Stateless — не хранит состояние между вызовами.

### Путь файла
`Assets/Scripts/Match/MatchDetector.cs`

### Алгоритм поиска

```
1. Сканируем сетку слева направо, снизу вверх
2. Для каждой ячейки:
   - Ищем горизонтальный матч (вправо)
   - Ищем вертикальный матч (вверх)
3. Собираем все найденные матчи
4. Объединяем пересекающиеся матчи одного типа
5. Возвращаем финальный список
```

**Визуализация горизонтального поиска:**
```
[R][R][R][B][B]
 ^
 Начинаем с (0,0)
 Идём вправо пока тип совпадает
 Найдено 3 Red → создаём матч
```

**Визуализация объединения:**
```
  [R]
  [R]
[R][R][R]    →   L-образный матч из 5 ячеек
```

### Код

```csharp
using System.Collections.Generic;

public class MatchDetector
{
    private const int MinMatchLength = 3;

    public List<MatchData> FindMatches(GridComponent grid)
    {
        var horizontalMatches = FindHorizontalMatches(grid);
        var verticalMatches = FindVerticalMatches(grid);

        var allMatches = new List<MatchData>();
        allMatches.AddRange(horizontalMatches);
        allMatches.AddRange(verticalMatches);

        return MergeIntersectingMatches(allMatches);
    }

    private List<MatchData> FindHorizontalMatches(GridComponent grid)
    {
        var matches = new List<MatchData>();

        for (int y = 0; y < grid.Height; y++)
        {
            int x = 0;
            while (x < grid.Width)
            {
                var startCell = grid.GetCell(x, y);

                // Пропускаем пустые ячейки
                if (startCell.IsEmpty)
                {
                    x++;
                    continue;
                }

                var type = startCell.Element.Type;
                var matchCells = new List<Cell> { startCell };

                // Ищем совпадения вправо
                int nextX = x + 1;
                while (nextX < grid.Width)
                {
                    var nextCell = grid.GetCell(nextX, y);
                    if (nextCell.IsEmpty || nextCell.Element.Type != type)
                        break;

                    matchCells.Add(nextCell);
                    nextX++;
                }

                // Создаём матч если >= 3
                if (matchCells.Count >= MinMatchLength)
                {
                    var match = new MatchData(type);
                    match.AddCells(matchCells);
                    match.SetHorizontal();
                    matches.Add(match);
                }

                // Продолжаем с позиции после матча
                x = nextX;
            }
        }

        return matches;
    }

    private List<MatchData> FindVerticalMatches(GridComponent grid)
    {
        var matches = new List<MatchData>();

        for (int x = 0; x < grid.Width; x++)
        {
            int y = 0;
            while (y < grid.Height)
            {
                var startCell = grid.GetCell(x, y);

                if (startCell.IsEmpty)
                {
                    y++;
                    continue;
                }

                var type = startCell.Element.Type;
                var matchCells = new List<Cell> { startCell };

                // Ищем совпадения вверх
                int nextY = y + 1;
                while (nextY < grid.Height)
                {
                    var nextCell = grid.GetCell(x, nextY);
                    if (nextCell.IsEmpty || nextCell.Element.Type != type)
                        break;

                    matchCells.Add(nextCell);
                    nextY++;
                }

                if (matchCells.Count >= MinMatchLength)
                {
                    var match = new MatchData(type);
                    match.AddCells(matchCells);
                    match.SetVertical();
                    matches.Add(match);
                }

                y = nextY;
            }
        }

        return matches;
    }

    private List<MatchData> MergeIntersectingMatches(List<MatchData> matches)
    {
        if (matches.Count <= 1) return matches;

        var merged = new List<MatchData>();
        var used = new bool[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            if (used[i]) continue;

            var current = matches[i];

            // Пытаемся объединить с оставшимися
            for (int j = i + 1; j < matches.Count; j++)
            {
                if (used[j]) continue;

                if (current.TryMerge(matches[j]))
                {
                    used[j] = true;
                }
            }

            merged.Add(current);
        }

        return merged;
    }

    /// <summary>
    /// Быстрая проверка: есть ли хоть один матч на поле?
    /// </summary>
    public bool HasAnyMatch(GridComponent grid)
    {
        // Горизонтальная проверка
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x <= grid.Width - MinMatchLength; x++)
            {
                if (IsHorizontalMatch(grid, x, y))
                    return true;
            }
        }

        // Вертикальная проверка
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y <= grid.Height - MinMatchLength; y++)
            {
                if (IsVerticalMatch(grid, x, y))
                    return true;
            }
        }

        return false;
    }

    private bool IsHorizontalMatch(GridComponent grid, int startX, int y)
    {
        var first = grid.GetCell(startX, y);
        if (first.IsEmpty) return false;

        var type = first.Element.Type;

        for (int i = 1; i < MinMatchLength; i++)
        {
            var cell = grid.GetCell(startX + i, y);
            if (cell.IsEmpty || cell.Element.Type != type)
                return false;
        }

        return true;
    }

    private bool IsVerticalMatch(GridComponent grid, int x, int startY)
    {
        var first = grid.GetCell(x, startY);
        if (first.IsEmpty) return false;

        var type = first.Element.Type;

        for (int i = 1; i < MinMatchLength; i++)
        {
            var cell = grid.GetCell(x, startY + i);
            if (cell.IsEmpty || cell.Element.Type != type)
                return false;
        }

        return true;
    }
}
```

### Примечания
- `MinMatchLength = 3` — константа, легко изменить
- Горизонтальный и вертикальный поиск идентичны по логике, просто разные оси
- `while` вместо `for` — сразу прыгаем за найденный матч, избегая повторной обработки
- `HasAnyMatch` — оптимизированный метод для быстрой проверки (без создания объектов)

---

## 4.3 MatchDetectorComponent (MonoBehaviour) — Опционально

### Назначение
Враппер для удобной интеграции в Unity. Позволяет тестировать через Inspector.

### Путь файла
`Assets/Scripts/Match/MatchDetectorComponent.cs`

### Код

```csharp
using System.Collections.Generic;
using UnityEngine;

public class MatchDetectorComponent : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;

    private MatchDetector _detector;

    private void Awake()
    {
        _detector = new MatchDetector();
    }

    public List<MatchData> FindMatches()
    {
        return _detector.FindMatches(_grid);
    }

    public bool HasAnyMatch()
    {
        return _detector.HasAnyMatch(_grid);
    }

#if UNITY_EDITOR
    [ContextMenu("Find Matches (Debug)")]
    private void DebugFindMatches()
    {
        if (_detector == null) _detector = new MatchDetector();

        var matches = FindMatches();

        if (matches.Count == 0)
        {
            Debug.Log("<color=yellow>No matches found</color>");
            return;
        }

        Debug.Log($"<color=green>Found {matches.Count} matches:</color>");
        foreach (var match in matches)
        {
            string cells = "";
            foreach (var cell in match.Cells)
            {
                cells += $"({cell.X},{cell.Y}) ";
            }
            Debug.Log($"  {match}: {cells}");
        }
    }

    [ContextMenu("Check Has Any Match")]
    private void DebugHasAnyMatch()
    {
        if (_detector == null) _detector = new MatchDetector();

        bool has = HasAnyMatch();
        Debug.Log(has
            ? "<color=red>Matches exist on board</color>"
            : "<color=green>No matches on board</color>");
    }
#endif
}
```

---

## Использование

### Простой вызов

```csharp
var detector = new MatchDetector();
List<MatchData> matches = detector.FindMatches(gridComponent);

foreach (var match in matches)
{
    Debug.Log($"Found {match.Type} match of {match.Length} cells");

    foreach (var cell in match.Cells)
    {
        // Уничтожить элемент или запустить анимацию
        cell.Element.DestroyElement();
    }
}
```

### В GameLoop (будущий этап)

```csharp
public class GameLoopController : MonoBehaviour
{
    private MatchDetector _matchDetector = new MatchDetector();

    private void CheckForMatches()
    {
        var matches = _matchDetector.FindMatches(_grid);

        if (matches.Count > 0)
        {
            // Переход к состоянию уничтожения
            StartDestruction(matches);
        }
        else
        {
            // Откат свапа или переход к Idle
        }
    }
}
```

---

## Тестирование

### Ручной тест через Inspector

1. Запустить Play Mode (поле заполняется BoardInitializer — без матчей)
2. В Scene View выбрать Board
3. Добавить MatchDetectorComponent если ещё нет
4. ПКМ → "Find Matches (Debug)"
5. Должно быть: "No matches found"

### Тест с принудительным матчем

```csharp
// Добавить в BoardInitializer для тестирования
[ContextMenu("Create Test Match")]
private void CreateTestMatch()
{
    // Создаём горизонтальный матч Red в нижнем ряду
    _spawner.SpawnAt(0, 0, ElementType.Red);
    _spawner.SpawnAt(1, 0, ElementType.Red);
    _spawner.SpawnAt(2, 0, ElementType.Red);

    Debug.Log("Created test match at row 0");
}
```

### Unit-тесты (EditMode)

```csharp
using NUnit.Framework;
using UnityEngine;

public class MatchDetectorTests
{
    [Test]
    public void FindMatches_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var detector = new MatchDetector();
        var grid = CreateMockGrid(3, 3);
        FillGridCheckerboard(grid);

        // Act
        var matches = detector.FindMatches(grid);

        // Assert
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_HorizontalThree_ReturnsOneMatch()
    {
        var detector = new MatchDetector();
        var grid = CreateMockGrid(5, 5);

        // Создаём горизонтальный матч
        SetElement(grid, 0, 0, ElementType.Red);
        SetElement(grid, 1, 0, ElementType.Red);
        SetElement(grid, 2, 0, ElementType.Red);

        var matches = detector.FindMatches(grid);

        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual(3, matches[0].Length);
        Assert.IsTrue(matches[0].IsHorizontal);
    }

    [Test]
    public void FindMatches_LShape_ReturnsMergedMatch()
    {
        var detector = new MatchDetector();
        var grid = CreateMockGrid(5, 5);

        // L-образный матч
        SetElement(grid, 0, 0, ElementType.Blue);
        SetElement(grid, 1, 0, ElementType.Blue);
        SetElement(grid, 2, 0, ElementType.Blue);
        SetElement(grid, 0, 1, ElementType.Blue);
        SetElement(grid, 0, 2, ElementType.Blue);

        var matches = detector.FindMatches(grid);

        Assert.AreEqual(1, matches.Count);  // Один объединённый матч
        Assert.AreEqual(5, matches[0].Length);
        Assert.IsTrue(matches[0].IsSpecial);  // L-образный
    }

    // Helper methods для создания mock grid...
}
```

---

## Визуализация матчей (Debug)

### Gizmos для отображения найденных матчей

```csharp
// Добавить в MatchDetectorComponent

#if UNITY_EDITOR
private List<MatchData> _lastMatches = new List<MatchData>();
private Color[] _matchColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

[ContextMenu("Find And Visualize")]
private void FindAndVisualize()
{
    _detector ??= new MatchDetector();
    _lastMatches = FindMatches();
    UnityEditor.SceneView.RepaintAll();
}

private void OnDrawGizmos()
{
    if (_lastMatches == null || _lastMatches.Count == 0) return;
    if (_grid == null) return;

    for (int i = 0; i < _lastMatches.Count; i++)
    {
        var match = _lastMatches[i];
        Gizmos.color = _matchColors[i % _matchColors.Length];

        foreach (var cell in match.Cells)
        {
            Vector3 pos = _grid.GridToWorld(cell.X, cell.Y);
            Gizmos.DrawWireSphere(pos, 0.4f);
        }
    }
}
#endif
```

---

## Настройка сцены

### Шаг 1: Создать файлы

```
Assets/Scripts/Match/
├── MatchData.cs
├── MatchDetector.cs
└── MatchDetectorComponent.cs (опционально)
```

### Шаг 2: Добавить MatchDetectorComponent

```
Board (GameObject)
├── GridComponent
├── SpawnComponent
├── BoardInitializer
└── MatchDetectorComponent    [добавить]
```

### Шаг 3: Связать в Inspector

**MatchDetectorComponent:**
- Grid: Board (GridComponent)

---

## Чеклист готовности

- [ ] MatchData.cs создан
- [ ] MatchDetector.cs создан
- [ ] MatchDetectorComponent.cs создан (опционально)
- [ ] Компонент добавлен на сцену
- [ ] "Find Matches (Debug)" на чистом поле → "No matches found"
- [ ] "Create Test Match" + "Find Matches" → находит матч
- [ ] L-образные матчи объединяются корректно
- [ ] Код компилируется без ошибок

---

## Возможные проблемы и решения

### Проблема: Находит дублирующиеся матчи

**Причина:** Горизонтальный и вертикальный матч не объединились
**Решение:** Проверить логику `TryMerge` — матчи должны быть одного типа и пересекаться

### Проблема: Длинные матчи (5+) разбиваются на несколько

**Причина:** Неправильная логика while-цикла
**Решение:** Убедиться что `x = nextX` после нахождения матча

### Проблема: NullReferenceException

**Причина:** Пустые ячейки или элементы
**Решение:** Всегда проверять `cell.IsEmpty` перед доступом к `cell.Element`

---

## Оптимизации (на будущее)

### 1. Проверка только изменённых областей

После свапа проверять только затронутые строки/колонки:

```csharp
public List<MatchData> FindMatchesAt(GridComponent grid, int x, int y)
{
    // Проверить только строку y и колонку x
}
```

### 2. Кэширование результатов

Если сетка не менялась — вернуть кэшированный результат.

### 3. Job System для больших сеток

Для сеток 20x20+ можно распараллелить поиск через Unity Job System.

---

## Следующий этап

После завершения Match Detection → **Этап 5: Input & Swap**
- InputComponent (обработка свайпов)
- SwapComponent (обмен элементов)
- SwapAnimationComponent (DOTween анимация)
