# Этап 1: Grid System — Детальный план реализации

## Обзор

Базовая система сетки для Match-3. Отвечает за:
- Хранение параметров сетки (ScriptableObject)
- Конвертацию координат grid ↔ world
- Debug-визуализацию в Editor

---

## Файлы

```
Assets/Scripts/Grid/
├── GridData.cs        # ScriptableObject с параметрами
├── Cell.cs            # Struct ячейки (минимальный, без isBlocked)
└── GridComponent.cs   # MonoBehaviour + Gizmos
```

---

## 1. GridData.cs — ScriptableObject

### Назначение
Конфигурация сетки. Один ассет можно переиспользовать для разных уровней или изменять параметры без перекомпиляции.

### Код

```csharp
using UnityEngine;

namespace Match3.Grid
{
    [CreateAssetMenu(fileName = "GridData", menuName = "Match3/Grid Data")]
    public class GridData : ScriptableObject
    {
        [Header("Dimensions")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _spacing = 0.1f;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public float Spacing => _spacing;

        /// <summary>
        /// Расстояние между центрами соседних ячеек
        /// </summary>
        public float Step => _cellSize + _spacing;
    }
}
```

### Валидация (опционально)

```csharp
private void OnValidate()
{
    _width = Mathf.Max(1, _width);
    _height = Mathf.Max(1, _height);
    _cellSize = Mathf.Max(0.1f, _cellSize);
    _spacing = Mathf.Max(0f, _spacing);
}
```

---

## 2. Cell.cs — Struct

### Назначение
Минимальная структура данных ячейки. Пока только позиция, `isBlocked` добавим позже.

### Код

```csharp
using UnityEngine;

namespace Match3.Grid
{
    public readonly struct Cell
    {
        public Vector2Int Position { get; }

        public Cell(Vector2Int position)
        {
            Position = position;
        }

        public Cell(int x, int y) : this(new Vector2Int(x, y)) { }
    }
}
```

### Почему struct?
- Маленький объект (только Vector2Int)
- Value-семантика — нет аллокаций в куче
- Immutable (readonly) — безопасно

---

## 3. GridComponent.cs — MonoBehaviour

### Назначение
- Инициализация сетки
- Конвертация координат
- Debug-отрисовка (Gizmos)

### Публичный API

| Метод | Описание |
|-------|----------|
| `Vector3 GridToWorld(Vector2Int pos)` | Grid координаты → World позиция центра ячейки |
| `Vector2Int WorldToGrid(Vector3 pos)` | World позиция → Grid координаты (округление) |
| `bool IsValidPosition(Vector2Int pos)` | Проверка границ сетки |
| `Cell GetCell(Vector2Int pos)` | Получить ячейку по позиции |

### События

| Событие | Когда вызывается |
|---------|------------------|
| `event Action OnGridReady` | После инициализации в `Awake()` |

### Код

```csharp
using System;
using UnityEngine;

namespace Match3.Grid
{
    public class GridComponent : MonoBehaviour
    {
        // === СОБЫТИЯ ===
        public event Action OnGridReady;

        // === ЗАВИСИМОСТИ ===
        [Header("Configuration")]
        [SerializeField] private GridData _gridData;

        // === ПРИВАТНЫЕ ПОЛЯ ===
        private Cell[,] _cells;

        // === СВОЙСТВА ===
        public int Width => _gridData.Width;
        public int Height => _gridData.Height;
        public GridData Data => _gridData;

        // === UNITY CALLBACKS ===
        private void Awake()
        {
            InitializeGrid();
            OnGridReady?.Invoke();
        }

        // === ИНИЦИАЛИЗАЦИЯ ===
        private void InitializeGrid()
        {
            _cells = new Cell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _cells[x, y] = new Cell(x, y);
                }
            }
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Конвертирует grid-координаты в world-позицию (центр ячейки)
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float step = _gridData.Step;
            float halfCell = _gridData.CellSize * 0.5f;

            float x = transform.position.x + gridPos.x * step + halfCell;
            float y = transform.position.y + gridPos.y * step + halfCell;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Конвертирует world-позицию в grid-координаты
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float step = _gridData.Step;

            Vector3 localPos = worldPos - transform.position;

            int x = Mathf.FloorToInt(localPos.x / step);
            int y = Mathf.FloorToInt(localPos.y / step);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Проверяет, находится ли позиция в пределах сетки
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width &&
                   pos.y >= 0 && pos.y < Height;
        }

        /// <summary>
        /// Получить ячейку по grid-координатам
        /// </summary>
        public Cell GetCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), $"Position {pos} is outside grid bounds");

            return _cells[pos.x, pos.y];
        }

        // === GIZMOS ===
        private void OnDrawGizmos()
        {
            if (_gridData == null) return;

            DrawGridGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (_gridData == null) return;

            // Более яркий цвет когда выбран
            DrawGridGizmos(selected: true);
        }

        private void DrawGridGizmos(bool selected = false)
        {
            Gizmos.color = selected ? Color.cyan : new Color(0.5f, 0.5f, 0.5f, 0.5f);

            float cellSize = _gridData.CellSize;
            float step = _gridData.Step;

            for (int x = 0; x < _gridData.Width; x++)
            {
                for (int y = 0; y < _gridData.Height; y++)
                {
                    Vector3 center = GridToWorldEditor(x, y);
                    Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
                }
            }

            // Внешняя граница сетки
            if (selected)
            {
                Gizmos.color = Color.yellow;
                Vector3 gridCenter = transform.position + new Vector3(
                    _gridData.Width * step * 0.5f - _gridData.Spacing * 0.5f,
                    _gridData.Height * step * 0.5f - _gridData.Spacing * 0.5f,
                    0f
                );
                Vector3 gridSize = new Vector3(
                    _gridData.Width * step - _gridData.Spacing,
                    _gridData.Height * step - _gridData.Spacing,
                    0f
                );
                Gizmos.DrawWireCube(gridCenter, gridSize);
            }
        }

        /// <summary>
        /// GridToWorld для Editor (когда _cells ещё не инициализирован)
        /// </summary>
        private Vector3 GridToWorldEditor(int x, int y)
        {
            float step = _gridData.Step;
            float halfCell = _gridData.CellSize * 0.5f;

            return new Vector3(
                transform.position.x + x * step + halfCell,
                transform.position.y + y * step + halfCell,
                0f
            );
        }
    }
}
```

---

## Порядок реализации

| # | Задача | Время |
|---|--------|-------|
| 1 | Создать папку `Assets/Scripts/Grid/` | — |
| 2 | Создать `GridData.cs` | 2 мин |
| 3 | Создать `Cell.cs` | 1 мин |
| 4 | Создать `GridComponent.cs` (без Gizmos) | 5 мин |
| 5 | Добавить Gizmos | 3 мин |
| 6 | Создать GridData ассет | 1 мин |
| 7 | Тестирование в сцене | 3 мин |

---

## Тестирование

### Ручное тестирование

1. **Создать GridData ассет:**
   - ПКМ в Project → Create → Match3 → Grid Data
   - Установить: Width=8, Height=8, CellSize=1, Spacing=0.1

2. **Создать GameObject "Grid":**
   - Добавить `GridComponent`
   - Назначить GridData ассет
   - Позиционировать в (0, 0, 0)

3. **Проверить Gizmos:**
   - В Scene View должна отображаться сетка 8x8
   - При выделении — жёлтая рамка вокруг всей сетки

4. **Проверить конвертацию координат:**
   - Временный тест-скрипт:

```csharp
// Временный тест (удалить после проверки)
private void Start()
{
    // Тест GridToWorld
    Vector3 pos00 = GridToWorld(new Vector2Int(0, 0));
    Vector3 pos77 = GridToWorld(new Vector2Int(7, 7));
    Debug.Log($"Cell (0,0) → World {pos00}"); // Ожидаем: (0.5, 0.5, 0)
    Debug.Log($"Cell (7,7) → World {pos77}"); // Ожидаем: (8.2, 8.2, 0) при step=1.1

    // Тест WorldToGrid
    Vector2Int grid = WorldToGrid(new Vector3(0.5f, 0.5f, 0f));
    Debug.Log($"World (0.5, 0.5) → Grid {grid}"); // Ожидаем: (0, 0)

    // Тест IsValidPosition
    Debug.Log($"IsValid(0,0): {IsValidPosition(new Vector2Int(0, 0))}");   // true
    Debug.Log($"IsValid(7,7): {IsValidPosition(new Vector2Int(7, 7))}");   // true
    Debug.Log($"IsValid(8,8): {IsValidPosition(new Vector2Int(8, 8))}");   // false
    Debug.Log($"IsValid(-1,0): {IsValidPosition(new Vector2Int(-1, 0))}"); // false
}
```

---

## Диаграмма координат

```
World Y ↑
        │
   7.15 ┤  ┌───┐ ┌───┐ ┌───┐ ...  (y=7)
        │  │   │ │   │ │   │
   6.05 ┤  └───┘ └───┘ └───┘
        │
   ...  │  ...
        │
   1.05 ┤  ┌───┐ ┌───┐ ┌───┐ ...  (y=1)
        │  │   │ │   │ │   │
   0.0  ┤──└───┘─└───┘─└───┘──────→ World X
        │  (0,0) (1,0) (2,0)
        0  0.5  1.1  1.6  2.2
           ↑
           transform.position = (0,0)

step = cellSize + spacing = 1.0 + 0.1 = 1.1
Центр ячейки (0,0) = (0.5, 0.5)
Центр ячейки (1,0) = (1.6, 0.5)
```

---

## Следующий этап

После завершения Grid System переходим к **Этап 2: Elements** — типы элементов, спрайты, ElementComponent.

Grid System будет использоваться для:
- Позиционирования элементов: `element.transform.position = _grid.GridToWorld(gridPos)`
- Определения ячейки по клику: `var cell = _grid.WorldToGrid(clickWorldPos)`
- Валидации ходов: `_grid.IsValidPosition(targetPos)`
