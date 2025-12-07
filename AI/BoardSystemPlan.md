# Board System - План Реализации

## Обзор

Board System — фундамент Match3 игры. Отвечает за:
- Хранение и управление игровым полем
- Создание и размещение тайлов
- Связь между логикой и визуалом

---

## Архитектура (Unity Way)

```
┌─────────────────────────────────────────────────────────────┐
│                      BoardController                         │
│  (Оркестратор: связывает Grid, Spawner, инициализацию)      │
└─────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  GridComponent  │  │  TileSpawner    │  │  TilePool       │
│  (Сетка ячеек)  │  │  (Создание)     │  │  (Пулинг)       │
└─────────────────┘  └─────────────────┘  └─────────────────┘
          │
          ▼
┌─────────────────┐
│  CellComponent  │ ─── Cell[,] массив
│  (Одна ячейка)  │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│  TileComponent  │ ─── Фишка в ячейке
│  (Игровая фишка)│
└─────────────────┘
```

---

## Компоненты

### 1. TileType (Enum + Data)

**Файл:** `Scripts/Data/TileType.cs`

```csharp
public enum TileType
{
    None = 0,
    Red = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4,
    Purple = 5,
    Orange = 6
}
```

**Файл:** `Scripts/Data/TileData.cs` (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "TileData", menuName = "Match3/TileData")]
public class TileData : ScriptableObject
{
    public TileType type;
    public Sprite sprite;
    public Color color;
}
```

**Зачем:** Разделение типа и визуальных данных. Легко добавлять новые тайлы через Inspector.

---

### 2. CellType (Enum)

**Файл:** `Scripts/Data/CellType.cs`

```csharp
public enum CellType
{
    Normal,     // Обычная ячейка
    Empty,      // Пустая (дырка в поле)
    Blocked,    // Заблокированная
    Spawner     // Точка спавна (верхний ряд)
}
```

---

### 3. TileComponent

**Файл:** `Scripts/Components/Board/TileComponent.cs`

**Ответственность:** Одна игровая фишка. Хранит тип, состояние, публикует события.

```csharp
public class TileComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<TileComponent> OnDestroyed;
    public event Action<Vector2Int, Vector2Int> OnMoved; // from, to

    // === ДАННЫЕ ===
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private TileType _type;
    private Vector2Int _gridPosition;
    private bool _isMatched;
    private bool _isMoving;

    // === СВОЙСТВА ===
    public TileType Type => _type;
    public Vector2Int GridPosition => _gridPosition;
    public bool IsMatched { get => _isMatched; set => _isMatched = value; }
    public bool IsMoving => _isMoving;
    public bool IsMatchable => _type != TileType.None;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void Initialize(TileType type, TileData data, Vector2Int position)
    {
        _type = type;
        _gridPosition = position;
        _spriteRenderer.sprite = data.sprite;
        _spriteRenderer.color = data.color;
        _isMatched = false;
        _isMoving = false;
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        var oldPosition = _gridPosition;
        _gridPosition = newPosition;
        OnMoved?.Invoke(oldPosition, newPosition);
    }

    public void DestroySelf()
    {
        OnDestroyed?.Invoke(this);
        // Возврат в пул или Destroy
    }

    public void SetMoving(bool moving) => _isMoving = moving;
}
```

**Принципы:**
- Минимум логики — только данные и события
- Не знает о Grid или Cell напрямую
- Визуал через `[SerializeField]`

---

### 4. CellComponent

**Файл:** `Scripts/Components/Board/CellComponent.cs`

**Ответственность:** Одна ячейка поля. Хранит позицию, тип, текущий тайл.

```csharp
public class CellComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<CellComponent, TileComponent> OnTileChanged; // cell, newTile

    // === ДАННЫЕ ===
    [SerializeField] private SpriteRenderer _backgroundRenderer;

    private Vector2Int _gridPosition;
    private CellType _cellType;
    private TileComponent _currentTile;

    // === СВОЙСТВА ===
    public Vector2Int GridPosition => _gridPosition;
    public CellType CellType => _cellType;
    public TileComponent CurrentTile => _currentTile;
    public bool IsEmpty => _currentTile == null;
    public bool CanHoldTile => _cellType == CellType.Normal || _cellType == CellType.Spawner;
    public bool IsSpawner => _cellType == CellType.Spawner;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void Initialize(Vector2Int position, CellType type)
    {
        _gridPosition = position;
        _cellType = type;
        transform.position = GridToWorld(position);
        UpdateVisual();
    }

    public void SetTile(TileComponent tile)
    {
        var oldTile = _currentTile;
        _currentTile = tile;

        if (tile != null)
        {
            tile.SetGridPosition(_gridPosition);
        }

        OnTileChanged?.Invoke(this, tile);
    }

    public TileComponent RemoveTile()
    {
        var tile = _currentTile;
        _currentTile = null;
        OnTileChanged?.Invoke(this, null);
        return tile;
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private void UpdateVisual()
    {
        // Визуал в зависимости от CellType
        _backgroundRenderer.enabled = _cellType != CellType.Empty;
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        // Конверсия будет в GridComponent, здесь заглушка
        return new Vector3(gridPos.x, gridPos.y, 0);
    }
}
```

**Принципы:**
- Не создаёт/уничтожает тайлы — только хранит ссылку
- События для оповещения об изменениях
- Не знает о соседях — это задача Grid

---

### 5. GridComponent

**Файл:** `Scripts/Components/Board/GridComponent.cs`

**Ответственность:** 2D массив ячеек. Доступ к ячейкам, соседям, конверсия координат.

```csharp
public class GridComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnGridInitialized;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private int _width = 8;
    [SerializeField] private int _height = 8;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector3 _originOffset = Vector3.zero;

    [Header("Prefabs")]
    [SerializeField] private CellComponent _cellPrefab;

    // === ДАННЫЕ ===
    private CellComponent[,] _cells;

    // === СВОЙСТВА ===
    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void Initialize(CellType[,] layout = null)
    {
        _cells = new CellComponent[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var cellType = layout?[x, y] ?? GetDefaultCellType(x, y);
                CreateCell(x, y, cellType);
            }
        }

        OnGridInitialized?.Invoke();
    }

    public CellComponent GetCell(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return _cells[x, y];
    }

    public CellComponent GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public bool IsValidPosition(Vector2Int pos) => IsValidPosition(pos.x, pos.y);

    public List<CellComponent> GetNeighbors(Vector2Int position)
    {
        var neighbors = new List<CellComponent>(4);

        // Только 4 направления (без диагоналей)
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            var neighborPos = position + dir;
            var cell = GetCell(neighborPos);
            if (cell != null)
            {
                neighbors.Add(cell);
            }
        }

        return neighbors;
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * _cellSize,
            gridPos.y * _cellSize,
            0
        ) + _originOffset;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        var localPos = worldPos - _originOffset;
        return new Vector2Int(
            Mathf.RoundToInt(localPos.x / _cellSize),
            Mathf.RoundToInt(localPos.y / _cellSize)
        );
    }

    public IEnumerable<CellComponent> GetAllCells()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                yield return _cells[x, y];
            }
        }
    }

    public IEnumerable<CellComponent> GetRow(int y)
    {
        for (int x = 0; x < _width; x++)
        {
            yield return _cells[x, y];
        }
    }

    public IEnumerable<CellComponent> GetColumn(int x)
    {
        for (int y = 0; y < _height; y++)
        {
            yield return _cells[x, y];
        }
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private void CreateCell(int x, int y, CellType type)
    {
        var position = new Vector2Int(x, y);
        var worldPos = GridToWorld(position);

        var cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
        cell.name = $"Cell_{x}_{y}";
        cell.Initialize(position, type);

        _cells[x, y] = cell;
    }

    private CellType GetDefaultCellType(int x, int y)
    {
        // Верхний ряд — спавнеры
        if (y == _height - 1)
            return CellType.Spawner;

        return CellType.Normal;
    }
}
```

**Принципы:**
- Только управление сеткой, не знает о матчах/свапах
- Чистые методы доступа
- Конверсия координат в одном месте

---

### 6. TileSpawner

**Файл:** `Scripts/Components/Board/TileSpawner.cs`

**Ответственность:** Создание тайлов с гарантией отсутствия начальных матчей.

```csharp
public class TileSpawner : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<TileComponent> OnTileSpawned;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private TileData[] _availableTiles;
    [SerializeField] private TileComponent _tilePrefab;

    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private Transform _tileContainer;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public TileComponent SpawnTile(Vector2Int position)
    {
        var tileData = GetRandomTileData();
        return SpawnTile(position, tileData);
    }

    public TileComponent SpawnTile(Vector2Int position, TileData data)
    {
        var worldPos = _grid.GridToWorld(position);
        var tile = Instantiate(_tilePrefab, worldPos, Quaternion.identity, _tileContainer);

        tile.Initialize(data.type, data, position);
        tile.name = $"Tile_{data.type}_{position.x}_{position.y}";

        OnTileSpawned?.Invoke(tile);
        return tile;
    }

    public TileComponent SpawnTileWithoutMatch(Vector2Int position, Func<Vector2Int, TileType, bool> wouldMatch)
    {
        var availableTypes = GetAvailableTypesWithoutMatch(position, wouldMatch);

        if (availableTypes.Count == 0)
        {
            // Fallback: спавним любой
            return SpawnTile(position);
        }

        var randomIndex = UnityEngine.Random.Range(0, availableTypes.Count);
        var data = GetTileData(availableTypes[randomIndex]);
        return SpawnTile(position, data);
    }

    public void SetAvailableTiles(TileData[] tiles)
    {
        _availableTiles = tiles;
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private TileData GetRandomTileData()
    {
        var index = UnityEngine.Random.Range(0, _availableTiles.Length);
        return _availableTiles[index];
    }

    private TileData GetTileData(TileType type)
    {
        foreach (var data in _availableTiles)
        {
            if (data.type == type)
                return data;
        }
        return _availableTiles[0];
    }

    private List<TileType> GetAvailableTypesWithoutMatch(Vector2Int pos, Func<Vector2Int, TileType, bool> wouldMatch)
    {
        var result = new List<TileType>();

        foreach (var data in _availableTiles)
        {
            if (!wouldMatch(pos, data.type))
            {
                result.Add(data.type);
            }
        }

        return result;
    }
}
```

---

### 7. BoardController (Оркестратор)

**Файл:** `Scripts/Core/BoardController.cs`

**Ответственность:** Связывает все компоненты Board, управляет инициализацией.

```csharp
public class BoardController : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnBoardReady;
    public event Action<CellComponent, TileComponent> OnTileChanged;

    // === ЗАВИСИМОСТИ ===
    [Header("Components")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private TileSpawner _spawner;

    // === СВОЙСТВА ===
    public GridComponent Grid => _grid;
    public int Width => _grid.Width;
    public int Height => _grid.Height;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void Initialize(LevelData levelData = null)
    {
        // 1. Инициализация сетки
        var layout = levelData?.GetCellLayout();
        _grid.Initialize(layout);

        // 2. Настройка доступных тайлов
        if (levelData != null)
        {
            _spawner.SetAvailableTiles(levelData.availableTiles);
        }

        // 3. Подписка на события ячеек
        SubscribeToCells();

        // 4. Заполнение поля тайлами
        FillBoard();

        OnBoardReady?.Invoke();
    }

    public TileComponent GetTile(int x, int y)
    {
        return _grid.GetCell(x, y)?.CurrentTile;
    }

    public TileComponent GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

    public void SwapTiles(Vector2Int posA, Vector2Int posB)
    {
        var cellA = _grid.GetCell(posA);
        var cellB = _grid.GetCell(posB);

        if (cellA == null || cellB == null) return;

        var tileA = cellA.RemoveTile();
        var tileB = cellB.RemoveTile();

        cellA.SetTile(tileB);
        cellB.SetTile(tileA);
    }

    public bool AreNeighbors(Vector2Int posA, Vector2Int posB)
    {
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y) == 1;
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private void SubscribeToCells()
    {
        foreach (var cell in _grid.GetAllCells())
        {
            cell.OnTileChanged += HandleTileChanged;
        }
    }

    private void HandleTileChanged(CellComponent cell, TileComponent tile)
    {
        OnTileChanged?.Invoke(cell, tile);
    }

    private void FillBoard()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var cell = _grid.GetCell(x, y);

                if (!cell.CanHoldTile || !cell.IsEmpty)
                    continue;

                var tile = _spawner.SpawnTileWithoutMatch(
                    cell.GridPosition,
                    WouldCreateMatch
                );

                cell.SetTile(tile);
            }
        }
    }

    private bool WouldCreateMatch(Vector2Int position, TileType type)
    {
        // Проверка горизонтали (2 слева)
        if (CheckLine(position, Vector2Int.left, type, 2))
            return true;

        // Проверка вертикали (2 снизу)
        if (CheckLine(position, Vector2Int.down, type, 2))
            return true;

        return false;
    }

    private bool CheckLine(Vector2Int start, Vector2Int direction, TileType type, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var checkPos = start + direction * i;
            var tile = GetTile(checkPos);

            if (tile == null || tile.Type != type)
                return false;
        }
        return true;
    }

    private void OnDisable()
    {
        foreach (var cell in _grid.GetAllCells())
        {
            cell.OnTileChanged -= HandleTileChanged;
        }
    }
}
```

---

## Интерфейсы

**Файл:** `Scripts/Interfaces/ITile.cs`

```csharp
public interface ITile
{
    TileType Type { get; }
    Vector2Int GridPosition { get; }
    bool IsMatchable { get; }

    event Action<ITile> OnDestroyed;
}
```

**Файл:** `Scripts/Interfaces/ICell.cs`

```csharp
public interface ICell
{
    Vector2Int GridPosition { get; }
    CellType CellType { get; }
    bool CanHoldTile { get; }
    bool IsEmpty { get; }
}
```

---

## Структура файлов

```
Assets/Scripts/
├── Data/
│   ├── TileType.cs
│   ├── TileData.cs          (ScriptableObject)
│   ├── CellType.cs
│   └── LevelData.cs         (ScriptableObject, позже)
├── Interfaces/
│   ├── ITile.cs
│   └── ICell.cs
├── Components/
│   └── Board/
│       ├── TileComponent.cs
│       ├── CellComponent.cs
│       ├── GridComponent.cs
│       └── TileSpawner.cs
└── Core/
    └── BoardController.cs
```

---

## Порядок реализации

### Шаг 1: Data Layer (30 мин)
1. `TileType.cs` — enum типов
2. `CellType.cs` — enum ячеек
3. `TileData.cs` — ScriptableObject для визуала

### Шаг 2: Tile (45 мин)
1. `TileComponent.cs` — компонент фишки
2. Создать префаб `Tile.prefab`:
   - SpriteRenderer
   - TileComponent
3. Тест: спавн тайла вручную

### Шаг 3: Cell (30 мин)
1. `CellComponent.cs` — компонент ячейки
2. Создать префаб `Cell.prefab`:
   - SpriteRenderer (фон)
   - CellComponent
3. Тест: создание ячейки вручную

### Шаг 4: Grid (45 мин)
1. `GridComponent.cs` — сетка
2. Создать объект `Grid` на сцене:
   - GridComponent
   - Ссылка на Cell префаб
3. Тест: генерация пустой сетки

### Шаг 5: Spawner (30 мин)
1. `TileSpawner.cs` — создание тайлов
2. Создать несколько `TileData` ассетов (Red, Blue, Green...)
3. Тест: спавн случайных тайлов

### Шаг 6: BoardController (45 мин)
1. `BoardController.cs` — оркестратор
2. Создать объект `Board` на сцене:
   - BoardController
   - GridComponent
   - TileSpawner
3. Тест: полная инициализация поля

### Шаг 7: Валидация (30 мин)
1. Проверить отсутствие начальных матчей
2. Проверить корректность координат
3. Проверить события

---

## Префабы

### Tile.prefab
```
Tile (GameObject)
├── SpriteRenderer
│   └── Sorting Layer: "Tiles"
│   └── Order: 1
└── TileComponent
    └── _spriteRenderer → SpriteRenderer
```

### Cell.prefab
```
Cell (GameObject)
├── SpriteRenderer
│   └── Sprite: cell_background
│   └── Sorting Layer: "Board"
│   └── Order: 0
└── CellComponent
    └── _backgroundRenderer → SpriteRenderer
```

---

## Тестовый чеклист

```
□ TileData ассеты создаются и отображают спрайты
□ TileComponent инициализируется с правильным типом
□ CellComponent хранит и отдаёт тайл
□ GridComponent создаёт сетку нужного размера
□ GridToWorld/WorldToGrid работают корректно
□ GetNeighbors возвращает 4 соседа (или меньше у границ)
□ TileSpawner создаёт тайлы без начальных матчей
□ BoardController инициализирует всё поле
□ События OnTileChanged срабатывают
□ Визуально поле выглядит как сетка с цветными фишками
```

---

## Следующие шаги (после Board)

1. **Input System** — выбор и свайп тайлов
2. **Match System** — детекция совпадений
3. **Swap System** — анимация обмена

---

## Примечания

- **Пулинг тайлов** — добавить позже для оптимизации
- **LevelData** — расширить когда дойдём до системы уровней
- **Анимации** — отдельные компоненты (TileAnimator)
