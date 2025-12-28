# Step 5: Match Detection System

> **Модуль поиска матчей (совпадений) на игровом поле**

## Обзор

Match Detection — ключевая механика Match-3. Компонент сканирует сетку и находит все последовательности из 3+ одинаковых элементов по горизонтали и вертикали.

---

## Зависимости

### Из предыдущих шагов (полагаемся на их реализацию)

| Шаг | Файл | Что используем |
|-----|------|----------------|
| 1 | `IGrid.cs` | Интерфейс сетки: `Width`, `Height`, `GetElementAt()`, `IsValidPosition()` |
| 1 | `IGridElement.cs` | Интерфейс элемента: `Type` |
| 1 | `ElementType.cs` | Enum типов элементов |
| 1 | `IMatchDetection.cs` | Интерфейс для этого компонента |
| 2 | `GridComponent.cs` | Реализация сетки |

### STUB для изолированного тестирования

```csharp
// STUB: Имитация сетки для тестов (НЕ включать в продакшн)
public class StubGrid : IGrid
{
    private readonly IGridElement[,] _elements;

    public int Width => 8;
    public int Height => 8;
    public float CellSize => 1f;

    public StubGrid()
    {
        _elements = new IGridElement[Width, Height];
    }

    public void SetupTestPattern(IGridElement[,] pattern)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _elements[x, y] = pattern[x, y];
    }

    public IGridElement GetElementAt(Vector2Int pos)
        => IsValidPosition(pos) ? _elements[pos.x, pos.y] : null;

    public bool IsValidPosition(Vector2Int pos)
        => pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

    // Остальные методы - заглушки
    public Vector3 GridToWorld(Vector2Int gridPos) => Vector3.zero;
    public Vector2Int WorldToGrid(Vector3 worldPos) => Vector2Int.zero;
    public void SetElementAt(Vector2Int pos, IGridElement element) { }
    public void ClearCell(Vector2Int pos) { }

    public event Action<Vector2Int, IGridElement> OnElementPlaced;
    public event Action<Vector2Int> OnCellCleared;
}

// STUB: Элемент с заданным типом
public class StubElement : IGridElement
{
    public Vector2Int GridPosition { get; set; }
    public ElementType Type { get; }
    public GameObject GameObject => null;

    public StubElement(ElementType type)
    {
        Type = type;
    }
}
```

---

## Файловая структура

```
Assets/Scripts/
├── Core/
│   └── Interfaces/
│       └── IMatchDetection.cs    ← Интерфейс (шаг 1)
└── Match/
    └── MatchDetectionComponent.cs ← Реализация (этот шаг)
```

---

## Интерфейс IMatchDetection (шаг 1)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IMatchDetection
{
    /// <summary>
    /// Минимальная длина для матча (обычно 3)
    /// </summary>
    int MinMatchLength { get; }

    /// <summary>
    /// Найти все матчи на всём поле
    /// </summary>
    List<Vector2Int> FindAllMatches();

    /// <summary>
    /// Найти матчи, проходящие через указанную позицию
    /// </summary>
    List<Vector2Int> FindMatchesAt(Vector2Int pos);

    /// <summary>
    /// Есть ли хотя бы один матч на поле
    /// </summary>
    bool HasAnyMatch();

    /// <summary>
    /// Проверить, создаст ли элемент типа type матч в позиции pos
    /// (используется SpawnSystem для предотвращения начальных матчей)
    /// </summary>
    bool WouldCreateMatch(Vector2Int pos, ElementType type);

    /// <summary>
    /// Событие при обнаружении матчей
    /// </summary>
    event Action<List<Vector2Int>> OnMatchesFound;
}
```

---

## Реализация MatchDetectionComponent

### Структура класса

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Match
{
    /// <summary>
    /// Компонент поиска матчей на игровом поле.
    /// Находит последовательности из 3+ одинаковых элементов.
    /// </summary>
    public class MatchDetectionComponent : MonoBehaviour, IMatchDetection
    {
        // === СОБЫТИЯ ===
        public event Action<List<Vector2Int>> OnMatchesFound;

        // === НАСТРОЙКИ ===
        [Header("Settings")]
        [SerializeField] private int _minMatchLength = 3;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;

        // === ПУБЛИЧНЫЕ СВОЙСТВА ===
        public int MinMatchLength => _minMatchLength;

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Найти все матчи на поле
        /// </summary>
        public List<Vector2Int> FindAllMatches()
        {
            var matches = new HashSet<Vector2Int>();

            FindHorizontalMatches(matches);
            FindVerticalMatches(matches);

            var result = new List<Vector2Int>(matches);

            if (result.Count > 0)
            {
                OnMatchesFound?.Invoke(result);
            }

            return result;
        }

        /// <summary>
        /// Найти матчи, проходящие через позицию
        /// </summary>
        public List<Vector2Int> FindMatchesAt(Vector2Int pos)
        {
            var matches = new HashSet<Vector2Int>();

            // Горизонтальная линия через pos
            var horizontal = GetMatchLineThrough(pos, Vector2Int.right);
            if (horizontal.Count >= _minMatchLength)
            {
                foreach (var p in horizontal)
                    matches.Add(p);
            }

            // Вертикальная линия через pos
            var vertical = GetMatchLineThrough(pos, Vector2Int.up);
            if (vertical.Count >= _minMatchLength)
            {
                foreach (var p in vertical)
                    matches.Add(p);
            }

            return new List<Vector2Int>(matches);
        }

        /// <summary>
        /// Есть ли матчи на поле
        /// </summary>
        public bool HasAnyMatch()
        {
            // Оптимизация: прерываем поиск при первом найденном матче

            // Горизонтальные
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x <= _grid.Width - _minMatchLength; x++)
                {
                    if (CheckLineMatch(new Vector2Int(x, y), Vector2Int.right, _minMatchLength))
                        return true;
                }
            }

            // Вертикальные
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y <= _grid.Height - _minMatchLength; y++)
                {
                    if (CheckLineMatch(new Vector2Int(x, y), Vector2Int.up, _minMatchLength))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверить, создаст ли элемент матч (для SpawnSystem)
        /// </summary>
        public bool WouldCreateMatch(Vector2Int pos, ElementType type)
        {
            // Проверяем горизонталь: 2 слева
            if (CountSameTypeInDirection(pos, Vector2Int.left, type) >= 2)
                return true;

            // Проверяем горизонталь: 2 справа
            if (CountSameTypeInDirection(pos, Vector2Int.right, type) >= 2)
                return true;

            // Проверяем горизонталь: 1 слева + 1 справа
            if (CountSameTypeInDirection(pos, Vector2Int.left, type) >= 1 &&
                CountSameTypeInDirection(pos, Vector2Int.right, type) >= 1)
                return true;

            // Проверяем вертикаль: 2 снизу
            if (CountSameTypeInDirection(pos, Vector2Int.down, type) >= 2)
                return true;

            // Проверяем вертикаль: 2 сверху
            if (CountSameTypeInDirection(pos, Vector2Int.up, type) >= 2)
                return true;

            // Проверяем вертикаль: 1 снизу + 1 сверху
            if (CountSameTypeInDirection(pos, Vector2Int.down, type) >= 1 &&
                CountSameTypeInDirection(pos, Vector2Int.up, type) >= 1)
                return true;

            return false;
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Найти все горизонтальные матчи
        /// </summary>
        private void FindHorizontalMatches(HashSet<Vector2Int> matches)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x <= _grid.Width - _minMatchLength; x++)
                {
                    var startPos = new Vector2Int(x, y);
                    var line = GetMatchLine(startPos, Vector2Int.right);

                    if (line.Count >= _minMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);

                        // Пропускаем уже найденные позиции
                        x += line.Count - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Найти все вертикальные матчи
        /// </summary>
        private void FindVerticalMatches(HashSet<Vector2Int> matches)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y <= _grid.Height - _minMatchLength; y++)
                {
                    var startPos = new Vector2Int(x, y);
                    var line = GetMatchLine(startPos, Vector2Int.up);

                    if (line.Count >= _minMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);

                        // Пропускаем уже найденные позиции
                        y += line.Count - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Получить линию одинаковых элементов начиная с позиции
        /// </summary>
        private List<Vector2Int> GetMatchLine(Vector2Int start, Vector2Int direction)
        {
            var result = new List<Vector2Int>();

            var startElement = _grid.GetElementAt(start);
            if (startElement == null || startElement.Type == ElementType.None)
                return result;

            result.Add(start);

            var current = start + direction;
            while (_grid.IsValidPosition(current))
            {
                var element = _grid.GetElementAt(current);
                if (element == null || element.Type != startElement.Type)
                    break;

                result.Add(current);
                current += direction;
            }

            return result;
        }

        /// <summary>
        /// Получить полную линию матча, проходящую через позицию
        /// (ищет в обе стороны от позиции)
        /// </summary>
        private List<Vector2Int> GetMatchLineThrough(Vector2Int pos, Vector2Int direction)
        {
            var result = new List<Vector2Int>();

            var element = _grid.GetElementAt(pos);
            if (element == null || element.Type == ElementType.None)
                return result;

            var type = element.Type;

            // Ищем начало линии (в обратном направлении)
            var start = pos;
            var checkPos = pos - direction;
            while (_grid.IsValidPosition(checkPos))
            {
                var el = _grid.GetElementAt(checkPos);
                if (el == null || el.Type != type)
                    break;
                start = checkPos;
                checkPos -= direction;
            }

            // Собираем всю линию от начала
            var current = start;
            while (_grid.IsValidPosition(current))
            {
                var el = _grid.GetElementAt(current);
                if (el == null || el.Type != type)
                    break;
                result.Add(current);
                current += direction;
            }

            return result;
        }

        /// <summary>
        /// Проверить наличие матча заданной длины
        /// </summary>
        private bool CheckLineMatch(Vector2Int start, Vector2Int direction, int length)
        {
            var startElement = _grid.GetElementAt(start);
            if (startElement == null || startElement.Type == ElementType.None)
                return false;

            var type = startElement.Type;

            for (int i = 1; i < length; i++)
            {
                var pos = start + direction * i;
                var element = _grid.GetElementAt(pos);

                if (element == null || element.Type != type)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Подсчитать количество элементов того же типа в направлении
        /// </summary>
        private int CountSameTypeInDirection(Vector2Int start, Vector2Int direction, ElementType type)
        {
            int count = 0;
            var current = start + direction;

            while (_grid.IsValidPosition(current))
            {
                var element = _grid.GetElementAt(current);
                if (element == null || element.Type != type)
                    break;

                count++;
                current += direction;
            }

            return count;
        }
    }
}
```

---

## Алгоритмы

### 1. FindAllMatches — Полный поиск

```
Алгоритм:
1. Создать HashSet<Vector2Int> для уникальных позиций
2. Пройти все горизонтальные линии:
   - Для каждой строки y от 0 до Height-1
   - Для каждой позиции x от 0 до Width-3
   - Найти линию одинаковых элементов вправо
   - Если длина >= 3, добавить все позиции в HashSet
3. Пройти все вертикальные линии (аналогично)
4. Вернуть List из HashSet

Сложность: O(Width * Height)
```

### 2. GetMatchLine — Поиск линии

```
Алгоритм:
1. Получить элемент в стартовой позиции
2. Если null или None — вернуть пустой список
3. Добавить стартовую позицию в результат
4. Двигаться в направлении direction
5. Пока позиция валидна И элемент того же типа:
   - Добавить позицию
   - Сдвинуться дальше
6. Вернуть список

Сложность: O(max(Width, Height))
```

### 3. WouldCreateMatch — Превентивная проверка

```
Алгоритм (для SpawnSystem):
1. Проверить 2 элемента слева от pos
2. Проверить 2 элемента справа
3. Проверить 1 слева + 1 справа
4. Проверить 2 снизу
5. Проверить 2 сверху
6. Проверить 1 снизу + 1 сверху
7. Если любая проверка true — вернуть true

Примечание: не проверяем сам элемент в pos,
так как он ещё не размещён (используется при спауне)
```

### Визуализация поиска

```
Сетка 8x8:
  0 1 2 3 4 5 6 7
7 . . . . . . . .
6 . . . . . . . .
5 . . R R R . . .   ← Горизонтальный матч (2,5), (3,5), (4,5)
4 . . . . . . . .
3 . G . . . . . .
2 . G . . B B B B   ← Горизонтальный матч (4,2), (5,2), (6,2), (7,2)
1 . G . . . . . .
0 . . . . . . . .
    ↑
    Вертикальный матч (1,1), (1,2), (1,3)

FindAllMatches() вернёт:
[(2,5), (3,5), (4,5), (4,2), (5,2), (6,2), (7,2), (1,1), (1,2), (1,3)]
```

---

## Тестирование

### Тест 1: Пустая сетка

```csharp
// Setup: сетка без элементов
// Expected: FindAllMatches() = []
// Expected: HasAnyMatch() = false
```

### Тест 2: Горизонтальный матч

```csharp
// Setup: три красных элемента в ряд
grid[0,0] = Red, grid[1,0] = Red, grid[2,0] = Red

// Expected: FindAllMatches() содержит (0,0), (1,0), (2,0)
// Expected: HasAnyMatch() = true
```

### Тест 3: Вертикальный матч

```csharp
// Setup: три синих элемента в столбец
grid[0,0] = Blue, grid[0,1] = Blue, grid[0,2] = Blue

// Expected: FindAllMatches() содержит (0,0), (0,1), (0,2)
```

### Тест 4: L-образный матч (пересечение)

```csharp
// Setup:
//   R R R
//   R
//   R
grid[0,0]=R, grid[0,1]=R, grid[0,2]=R  // вертикаль
grid[0,2]=R, grid[1,2]=R, grid[2,2]=R  // горизонталь

// Expected: уникальные позиции (без дублей для [0,2])
// Result: [(0,0), (0,1), (0,2), (1,2), (2,2)]
```

### Тест 5: WouldCreateMatch

```csharp
// Setup:
grid[0,0] = Red, grid[1,0] = Red, grid[2,0] = null

// Expected: WouldCreateMatch((2,0), Red) = true
// Expected: WouldCreateMatch((2,0), Blue) = false
```

### Тест 6: Матч 4+ элемента

```csharp
// Setup: четыре зелёных элемента
grid[0,0]=G, grid[1,0]=G, grid[2,0]=G, grid[3,0]=G

// Expected: все 4 позиции в результате
```

### Тест 7: FindMatchesAt — точечный поиск

```csharp
// Setup:
//   . . . . .
//   R R R . .    ← матч на y=1
//   . . R . .    ← R в (2,0)
//   . . R . .    ← R в (2,-1) - за границей

grid[0,1]=R, grid[1,1]=R, grid[2,1]=R
grid[2,0]=R

// FindMatchesAt((1,1)) должен найти горизонталь [(0,1), (1,1), (2,1)]
// Вертикаль (2,0), (2,1) — только 2 элемента, не матч
```

---

## Интеграция с GameLoop (шаг 9)

```csharp
// В GameLoopComponent:
private async Task ProcessSwap(Vector2Int pos1, Vector2Int pos2)
{
    // ... свап ...

    var matches = _matchDetection.FindAllMatches();

    if (matches.Count == 0)
    {
        // Откат свапа
        return;
    }

    // Каскадный цикл
    while (matches.Count > 0)
    {
        await _destruction.DestroyElements(matches);
        await _gravity.ApplyGravity();
        matches = _matchDetection.FindAllMatches();
    }
}
```

---

## Чеклист реализации

### Подготовка
- [ ] Убедиться что IMatchDetection создан в шаге 1
- [ ] Создать папку `Assets/Scripts/Match/`

### Реализация
- [ ] Создать `MatchDetectionComponent.cs`
- [ ] Реализовать `FindAllMatches()` с HashSet
- [ ] Реализовать `GetMatchLine()` — базовый алгоритм
- [ ] Реализовать `GetMatchLineThrough()` — поиск в обе стороны
- [ ] Реализовать `FindMatchesAt()`
- [ ] Реализовать `HasAnyMatch()` с ранним выходом
- [ ] Реализовать `WouldCreateMatch()` для SpawnSystem
- [ ] Добавить событие `OnMatchesFound`

### Валидация
- [ ] Проверка null-элементов
- [ ] Проверка ElementType.None
- [ ] Проверка границ сетки
- [ ] HashSet для уникальности позиций

### Тестирование
- [ ] Тест пустой сетки
- [ ] Тест горизонтального матча
- [ ] Тест вертикального матча
- [ ] Тест L-образного пересечения
- [ ] Тест матча 4+ элементов
- [ ] Тест WouldCreateMatch
- [ ] Тест FindMatchesAt

### Unity Setup
- [ ] Добавить компонент на GameObject
- [ ] Связать GridComponent через Inspector
- [ ] Настроить _minMatchLength = 3

---

## Возможные расширения (не в scope)

1. **Поиск специальных форм** (T, L, крест) — для бонусов
2. **Weighted matching** — приоритет длинных матчей
3. **Цепочки матчей** — отслеживание combo
4. **Предсказание возможных ходов** — подсказки игроку

---

## Примечания

- `HashSet<Vector2Int>` гарантирует уникальность позиций при пересечениях
- Метод `WouldCreateMatch` критичен для SpawnSystem — без него будут начальные матчи
- Событие `OnMatchesFound` используется для UI/звуков/эффектов
- Компонент stateless — не хранит состояние между вызовами
