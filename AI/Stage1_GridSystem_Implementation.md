# Этап 1: Grid System — Подробный План Реализации

## Обзор

Grid System — фундамент Match3. Отвечает за:
- Логическую структуру игрового поля
- Конвертацию grid ↔ world координат
- Хранение ссылок на элементы

**Принцип Unity Way:** Данные (Config) отделены от логики (Component), ячейка (Cell) — чистый класс без MonoBehaviour.

---

## 1.1 GridConfig (ScriptableObject)

### Назначение
Хранит настройки сетки. Позволяет менять параметры без перекомпиляции. Один конфиг — много уровней (при необходимости).

### Путь файла
`Assets/Scripts/Grid/GridConfig.cs`

### Код

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/GridConfig")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Size")]
    [SerializeField, Range(5, 12)] private int _width = 8;
    [SerializeField, Range(5, 12)] private int _height = 8;

    [Header("Cell Settings")]
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _originOffset = Vector2.zero;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public Vector2 OriginOffset => _originOffset;
}
```

### Создание ассета
1. `Assets/Configs/` → ПКМ → Create → Match3 → GridConfig
2. Настроить параметры в Inspector

---

## 1.2 Cell (Plain Class)

### Назначение
Логическая ячейка сетки. Хранит координаты и ссылку на элемент. **Не MonoBehaviour** — чистые данные.

### Путь файла
`Assets/Scripts/Grid/Cell.cs`

### Код

```csharp
using System;

public class Cell
{
    public int X { get; }
    public int Y { get; }

    private ElementComponent _element;

    public event Action<ElementComponent> OnElementChanged;

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    public ElementComponent Element
    {
        get => _element;
        set
        {
            if (_element != value)
            {
                _element = value;
                OnElementChanged?.Invoke(_element);
            }
        }
    }

    public bool IsEmpty => _element == null;

    public void Clear()
    {
        Element = null;
    }

    public override string ToString() => $"Cell({X}, {Y})";
}
```

### Почему Plain Class?
- Нет визуального представления
- Нет Update/FixedUpdate
- Минимум памяти
- Легко тестировать

---

## 1.3 GridComponent (MonoBehaviour)

### Назначение
Главный компонент сетки. Создаёт ячейки, конвертирует координаты, рисует Gizmos.

### Путь файла
`Assets/Scripts/Grid/GridComponent.cs`

### Код

```csharp
using UnityEngine;

public class GridComponent : MonoBehaviour
{
    public event System.Action OnGridCreated;

    [SerializeField] private GridConfig _config;

    private Cell[,] _cells;

    public int Width => _config.Width;
    public int Height => _config.Height;
    public GridConfig Config => _config;

    private void Awake()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        _cells = new Cell[_config.Width, _config.Height];

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                _cells[x, y] = new Cell(x, y);
            }
        }

        OnGridCreated?.Invoke();
    }

    // --- Доступ к ячейкам ---

    public Cell GetCell(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return _cells[x, y];
    }

    public Cell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < _config.Width &&
               y >= 0 && y < _config.Height;
    }

    // --- Конвертация координат ---

    public Vector3 GridToWorld(int x, int y)
    {
        float worldX = x * _config.CellSize + _config.OriginOffset.x;
        float worldY = y * _config.CellSize + _config.OriginOffset.y;
        return new Vector3(worldX, worldY, 0f);
    }

    public Vector3 GridToWorld(Vector2Int pos) => GridToWorld(pos.x, pos.y);

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - _config.OriginOffset.x) / _config.CellSize);
        int y = Mathf.RoundToInt((worldPos.y - _config.OriginOffset.y) / _config.CellSize);
        return new Vector2Int(x, y);
    }

    // --- Соседние ячейки ---

    public Cell GetNeighbor(Cell cell, Vector2Int direction)
    {
        return GetCell(cell.X + direction.x, cell.Y + direction.y);
    }

    public Cell GetNeighbor(int x, int y, Vector2Int direction)
    {
        return GetCell(x + direction.x, y + direction.y);
    }

    // --- Gizmos ---

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_config == null) return;

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                Gizmos.DrawWireCube(pos, Vector3.one * _config.CellSize * 0.95f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;

        Gizmos.color = Color.cyan;

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                Gizmos.DrawWireCube(pos, Vector3.one * _config.CellSize * 0.95f);

                // Координаты
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(pos, $"{x},{y}");
                #endif
            }
        }
    }
#endif
}
```

---

## Настройка сцены

### Шаг 1: Создать структуру папок

```
Assets/
├── Scripts/
│   └── Grid/
│       ├── GridConfig.cs
│       ├── Cell.cs
│       └── GridComponent.cs
├── Configs/
│   └── GridConfig.asset
└── Prefabs/
```

### Шаг 2: Создать GameObject

1. Создать пустой GameObject → назвать `Board`
2. Добавить `GridComponent`
3. Создать `GridConfig.asset` в `Assets/Configs/`
4. Присвоить конфиг в Inspector

### Шаг 3: Настроить камеру

Для центрирования сетки 8x8 с cellSize=1:
- Camera Position: (3.5, 3.5, -10)
- Orthographic Size: 5

Или использовать `OriginOffset` в GridConfig.

---

## Вспомогательные утилиты (опционально)

### GridDirections.cs

```csharp
using UnityEngine;

public static class GridDirections
{
    public static readonly Vector2Int Up = new(0, 1);
    public static readonly Vector2Int Down = new(0, -1);
    public static readonly Vector2Int Left = new(-1, 0);
    public static readonly Vector2Int Right = new(1, 0);

    public static readonly Vector2Int[] All = { Up, Down, Left, Right };
    public static readonly Vector2Int[] Horizontal = { Left, Right };
    public static readonly Vector2Int[] Vertical = { Up, Down };
}
```

---

## Тестирование

### Визуальная проверка

1. Запустить Play Mode
2. В Scene View должна отобразиться сетка (Gizmos)
3. Выбрать Board → сетка подсвечивается cyan с координатами

### Код-тест (Console)

Добавить временно в `GridComponent.Awake()`:

```csharp
// Тест после CreateGrid()
Debug.Log($"Grid created: {Width}x{Height}");
Debug.Log($"Cell (0,0) world pos: {GridToWorld(0, 0)}");
Debug.Log($"Cell (7,7) world pos: {GridToWorld(7, 7)}");
Debug.Log($"World (3.5, 3.5) → Grid: {WorldToGrid(new Vector3(3.5f, 3.5f, 0))}");
```

### Ожидаемый результат

```
Grid created: 8x8
Cell (0,0) world pos: (0.0, 0.0, 0.0)
Cell (7,7) world pos: (7.0, 7.0, 0.0)
World (3.5, 3.5) → Grid: (4, 4)
```

---

## Чеклист готовности

- [ ] GridConfig.cs создан
- [ ] GridConfig.asset создан и настроен
- [ ] Cell.cs создан
- [ ] GridComponent.cs создан
- [ ] Board GameObject в сцене с GridComponent
- [ ] Gizmos отображаются в Scene View
- [ ] Тест координат проходит
- [ ] Код компилируется без ошибок

---

## Следующий этап

После завершения Grid System → **Этап 2: Elements**
- ElementType enum
- ElementConfig (ScriptableObject)
- ElementComponent (MonoBehaviour)
- ElementFactory
