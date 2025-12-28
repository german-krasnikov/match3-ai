# Step 2: GRID SYSTEM — Детальный план реализации

> **Статус:** ГОТОВО
> **Зависит от:** Step 1 (Core — интерфейсы IGrid, IGridElement)
> **Блокирует:** Steps 3, 4, 5, 6, 7, 8, 9

---

## Цель

Реализовать `GridComponent` — центральное хранилище данных игры. Сетка 8x8 с конвертацией координат grid↔world.

---

## Файловая структура

```
Assets/Scripts/Grid/
└── GridComponent.cs
```

---

## Зависимости (из Step 1)

```csharp
// Ожидаем из Step 1:
public interface IGrid
{
    int Width { get; }
    int Height { get; }
    float CellSize { get; }

    Vector3 GridToWorld(Vector2Int gridPos);
    Vector2Int WorldToGrid(Vector3 worldPos);
    bool IsValidPosition(Vector2Int pos);

    IGridElement GetElementAt(Vector2Int pos);
    void SetElementAt(Vector2Int pos, IGridElement element);
    void ClearCell(Vector2Int pos);

    event Action<Vector2Int, IGridElement> OnElementPlaced;
    event Action<Vector2Int> OnCellCleared;
}

public interface IGridElement
{
    Vector2Int GridPosition { get; set; }
    ElementType Type { get; }
    GameObject GameObject { get; }
}
```

---

## STUB для тестирования

До реализации Element System (Step 3), используем заглушку:

```csharp
// Временный STUB — удалить после интеграции со Step 3
public class StubGridElement : IGridElement
{
    public Vector2Int GridPosition { get; set; }
    public ElementType Type { get; }
    public GameObject GameObject { get; }

    public StubGridElement(ElementType type = ElementType.Red)
    {
        Type = type;
        GameObject = null;
    }
}
```

> **Важно:** STUB размещать в том же файле `GridComponent.cs` под `#if UNITY_EDITOR` или в отдельном файле `StubGridElement.cs` в папке `Grid/`.

---

## Реализация GridComponent

### Полный код

```csharp
using System;
using UnityEngine;

namespace Match3.Grid
{
    public class GridComponent : MonoBehaviour, IGrid
    {
        // === СОБЫТИЯ ===
        public event Action<Vector2Int, IGridElement> OnElementPlaced;
        public event Action<Vector2Int> OnCellCleared;

        // === НАСТРОЙКИ ===
        [Header("Grid Settings")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector2 _origin = Vector2.zero;

        // === ДАННЫЕ ===
        private IGridElement[,] _grid;

        // === СВОЙСТВА ===
        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;

        // === UNITY CALLBACKS ===
        private void Awake()
        {
            InitializeGrid();
        }

        // === ИНИЦИАЛИЗАЦИЯ ===
        private void InitializeGrid()
        {
            _grid = new IGridElement[_width, _height];
        }

        // === КОНВЕРТАЦИЯ КООРДИНАТ ===

        /// <summary>
        /// Grid → World: центр ячейки
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float x = _origin.x + gridPos.x * _cellSize + _cellSize * 0.5f;
            float y = _origin.y + gridPos.y * _cellSize + _cellSize * 0.5f;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// World → Grid: округление вниз
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _origin.x) / _cellSize);
            int y = Mathf.FloorToInt((worldPos.y - _origin.y) / _cellSize);
            return new Vector2Int(x, y);
        }

        // === ВАЛИДАЦИЯ ===
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width &&
                   pos.y >= 0 && pos.y < _height;
        }

        // === CRUD ОПЕРАЦИИ ===

        public IGridElement GetElementAt(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return null;

            return _grid[pos.x, pos.y];
        }

        public void SetElementAt(Vector2Int pos, IGridElement element)
        {
            if (!IsValidPosition(pos))
                return;

            _grid[pos.x, pos.y] = element;

            if (element != null)
            {
                element.GridPosition = pos;
                OnElementPlaced?.Invoke(pos, element);
            }
        }

        public void ClearCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return;

            _grid[pos.x, pos.y] = null;
            OnCellCleared?.Invoke(pos);
        }

        // === DEBUG ===
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _cellColor = new Color(0f, 1f, 0f, 0.2f);

        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            DrawGridGizmos();
        }

        private void DrawGridGizmos()
        {
            Gizmos.color = _gridColor;

            // Вертикальные линии
            for (int x = 0; x <= _width; x++)
            {
                Vector3 start = new Vector3(_origin.x + x * _cellSize, _origin.y, 0);
                Vector3 end = new Vector3(_origin.x + x * _cellSize, _origin.y + _height * _cellSize, 0);
                Gizmos.DrawLine(start, end);
            }

            // Горизонтальные линии
            for (int y = 0; y <= _height; y++)
            {
                Vector3 start = new Vector3(_origin.x, _origin.y + y * _cellSize, 0);
                Vector3 end = new Vector3(_origin.x + _width * _cellSize, _origin.y + y * _cellSize, 0);
                Gizmos.DrawLine(start, end);
            }

            // Заполненные ячейки (если есть данные)
            if (_grid != null)
            {
                Gizmos.color = _cellColor;
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_grid[x, y] != null)
                        {
                            Vector3 center = GridToWorld(new Vector2Int(x, y));
                            Vector3 size = new Vector3(_cellSize * 0.9f, _cellSize * 0.9f, 0.1f);
                            Gizmos.DrawCube(center, size);
                        }
                    }
                }
            }
        }
#endif
    }
}
```

---

## Подзадачи (чеклист)

### 2.1 Базовая структура
- [x] Создать папку `Assets/Scripts/Grid/`
- [x] Создать `GridComponent.cs`
- [x] Добавить namespace `Match3.Grid`
- [x] Реализовать `IGrid` интерфейс

### 2.2 Хранилище данных
- [x] Приватный 2D массив `IGridElement[,] _grid`
- [x] `InitializeGrid()` в `Awake()`
- [x] Публичные readonly свойства Width, Height, CellSize

### 2.3 Конвертация координат
- [x] `GridToWorld()` — возвращает центр ячейки
- [x] `WorldToGrid()` — округление вниз через `Mathf.FloorToInt`
- [x] Учёт `_origin` смещения

### 2.4 CRUD операции
- [x] `GetElementAt()` — с проверкой границ
- [x] `SetElementAt()` — обновляет GridPosition элемента, вызывает событие
- [x] `ClearCell()` — null + событие

### 2.5 Валидация
- [x] `IsValidPosition()` — проверка x/y в пределах 0..Width-1, 0..Height-1

### 2.6 События
- [x] `OnElementPlaced` — при SetElementAt (если element != null)
- [x] `OnCellCleared` — при ClearCell

### 2.7 Debug Gizmos
- [x] Отрисовка сетки линиями
- [x] Подсветка заполненных ячеек
- [x] Toggle через `_showGizmos`
- [x] Только в Editor (`#if UNITY_EDITOR`)

### 2.8 Тестирование
- [ ] Создать пустой GameObject "Grid" в сцене
- [ ] Добавить GridComponent
- [ ] Проверить Gizmos в Scene View
- [ ] Написать простой тест конвертации (опционально)

---

## Формулы конвертации

### Grid → World (центр ячейки)
```
worldX = origin.x + gridX * cellSize + cellSize * 0.5
worldY = origin.y + gridY * cellSize + cellSize * 0.5
```

### World → Grid
```
gridX = floor((worldX - origin.x) / cellSize)
gridY = floor((worldY - origin.y) / cellSize)
```

---

## Тестовый сценарий

```csharp
// Тест в отдельном MonoBehaviour или Unit Test
void TestGridConversion()
{
    var grid = GetComponent<GridComponent>();

    // Test 1: GridToWorld
    var worldPos = grid.GridToWorld(new Vector2Int(0, 0));
    Debug.Assert(worldPos == new Vector3(0.5f, 0.5f, 0f), "GridToWorld failed");

    // Test 2: WorldToGrid
    var gridPos = grid.WorldToGrid(new Vector3(0.7f, 0.3f, 0f));
    Debug.Assert(gridPos == new Vector2Int(0, 0), "WorldToGrid failed");

    // Test 3: Roundtrip
    var original = new Vector2Int(3, 5);
    var converted = grid.WorldToGrid(grid.GridToWorld(original));
    Debug.Assert(converted == original, "Roundtrip failed");

    // Test 4: Bounds
    Debug.Assert(grid.IsValidPosition(new Vector2Int(0, 0)), "Valid pos failed");
    Debug.Assert(grid.IsValidPosition(new Vector2Int(7, 7)), "Valid pos failed");
    Debug.Assert(!grid.IsValidPosition(new Vector2Int(-1, 0)), "Invalid pos failed");
    Debug.Assert(!grid.IsValidPosition(new Vector2Int(8, 0)), "Invalid pos failed");

    Debug.Log("All grid tests passed!");
}
```

---

## Интеграция с другими системами

| Система | Как использует Grid |
|---------|---------------------|
| **Elements (Step 3)** | Получает world позицию для размещения |
| **Spawn (Step 4)** | Заполняет ячейки через SetElementAt |
| **Match (Step 5)** | Читает элементы через GetElementAt |
| **Swap (Step 6)** | Обменивает элементы между ячейками |
| **Destruction (Step 7)** | Очищает ячейки через ClearCell |
| **Gravity (Step 8)** | Перемещает элементы вниз |
| **Input (Step 6)** | Конвертирует клик в grid координаты |

---

## Важные заметки

1. **Origin** — точка (0,0) сетки в world space. По умолчанию Vector2.zero. Позволяет центрировать сетку на экране.

2. **CellSize** — размер одной ячейки. При CellSize=1 и сетке 8x8, общий размер 8x8 units.

3. **GridToWorld возвращает центр** — важно для размещения спрайтов с pivot в центре.

4. **События** — позволяют другим системам реагировать на изменения без прямой связи.

5. **Gizmos только в Editor** — не влияют на production build.

---

## Критерии готовности

- [x] `GridComponent` компилируется без ошибок
- [x] Реализованы все методы интерфейса `IGrid`
- [x] Конвертация координат работает корректно
- [x] Gizmos отображаются в Scene View
- [x] События вызываются при изменении данных
- [x] Код соответствует Unity Way (SerializeField, события, single responsibility)
