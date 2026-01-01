# Step 1: Grid System — Implementation Specification

## Overview

Создание системы сетки: конфигурация, данные, визуализация. Сетка 8x8 с origin в левом нижнем углу.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    GridConfig (SO)                       │
│              width, height, cellSize, origin             │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                  GridData (C# class)                     │
│     Size, GridToWorld(), WorldToGrid(), IsValid()        │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                GridView (MonoBehaviour)                  │
│          [SerializeField] GridConfig, cellPrefab         │
│            Creates visual grid, exposes GridData         │
└─────────────────────────────────────────────────────────┘
```

---

## File Structure

```
Assets/
  Scripts/
    Grid/
      GridConfig.cs      # ScriptableObject
      GridData.cs        # Plain C# class
      GridView.cs        # MonoBehaviour
  ScriptableObjects/
    GridConfig.asset     # Create via Assets menu
  Prefabs/
    Cell.prefab          # Optional: visual cell sprite (can be null)
```

---

## Component 1: GridConfig

**File:** `Assets/Scripts/Grid/GridConfig.cs`

**Responsibility:** Хранит настройки сетки. ScriptableObject для переиспользования между сценами.

### Code

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Dimensions")]
    [SerializeField] private int _width = 8;
    [SerializeField] private int _height = 8;

    [Header("Cell Settings")]
    [SerializeField] private float _cellSize = 1f;

    [Header("Position")]
    [Tooltip("World position of bottom-left cell (0,0)")]
    [SerializeField] private Vector3 _origin = Vector3.zero;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public Vector3 Origin => _origin;
    public Vector2Int Size => new Vector2Int(_width, _height);
}
```

### Inspector Setup
- Width: 8
- Height: 8
- Cell Size: 1
- Origin: (0, 0, 0)

---

## Component 2: GridData

**File:** `Assets/Scripts/Grid/GridData.cs`

**Responsibility:** Логика координат. Конвертация grid <-> world. Валидация позиций.

**Note:** Plain C# class (не MonoBehaviour). Создаётся и хранится в GridView.

### Code

```csharp
using UnityEngine;

public class GridData
{
    private readonly int _width;
    private readonly int _height;
    private readonly float _cellSize;
    private readonly Vector3 _origin;

    public Vector2Int Size => new Vector2Int(_width, _height);
    public int Width => _width;
    public int Height => _height;

    public GridData(GridConfig config)
    {
        _width = config.Width;
        _height = config.Height;
        _cellSize = config.CellSize;
        _origin = config.Origin;
    }

    public GridData(int width, int height, float cellSize, Vector3 origin)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _origin = origin;
    }

    /// <summary>
    /// Converts grid position to world position (center of cell).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = _origin.x + gridPos.x * _cellSize + _cellSize * 0.5f;
        float y = _origin.y + gridPos.y * _cellSize + _cellSize * 0.5f;
        return new Vector3(x, y, _origin.z);
    }

    /// <summary>
    /// Converts world position to grid position.
    /// Returns nearest cell, may be outside grid bounds.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - _origin.x) / _cellSize);
        int y = Mathf.FloorToInt((worldPos.y - _origin.y) / _cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Checks if grid position is within bounds.
    /// </summary>
    public bool IsValidPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _width &&
               gridPos.y >= 0 && gridPos.y < _height;
    }

    /// <summary>
    /// Gets world position above the grid for spawning.
    /// </summary>
    public Vector3 GetSpawnPosition(int column, int rowsAbove = 1)
    {
        return GridToWorld(new Vector2Int(column, _height - 1 + rowsAbove));
    }
}
```

### Public API (for other systems)

| Member | Type | Description |
|--------|------|-------------|
| `Size` | `Vector2Int` | Grid dimensions (width, height) |
| `Width` | `int` | Grid width |
| `Height` | `int` | Grid height |
| `GridToWorld(Vector2Int)` | `Vector3` | Converts grid coords to world position (cell center) |
| `WorldToGrid(Vector3)` | `Vector2Int` | Converts world position to grid coords |
| `IsValidPosition(Vector2Int)` | `bool` | Returns true if position is within grid bounds |
| `GetSpawnPosition(int, int)` | `Vector3` | Gets spawn position above grid for column |

---

## Component 3: GridView

**File:** `Assets/Scripts/Grid/GridView.cs`

**Responsibility:** Визуализация сетки. Создаёт GridData. Опционально рисует ячейки.

### Code

```csharp
using UnityEngine;

public class GridView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GridConfig _config;

    [Header("Visuals (Optional)")]
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Transform _cellsParent;

    private GridData _gridData;
    private GameObject[,] _cellVisuals;

    public GridData Data => _gridData;
    public GridConfig Config => _config;

    private void Awake()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        _gridData = new GridData(_config);

        if (_cellPrefab != null)
        {
            CreateCellVisuals();
        }
    }

    private void CreateCellVisuals()
    {
        Transform parent = _cellsParent != null ? _cellsParent : transform;
        _cellVisuals = new GameObject[_config.Width, _config.Height];

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = _gridData.GridToWorld(gridPos);

                GameObject cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, parent);
                cell.name = $"Cell_{x}_{y}";
                _cellVisuals[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// Returns visual cell at position (if exists).
    /// </summary>
    public GameObject GetCellVisual(Vector2Int pos)
    {
        if (_cellVisuals == null || !_gridData.IsValidPosition(pos))
            return null;
        return _cellVisuals[pos.x, pos.y];
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _gizmoColor = new Color(1f, 1f, 1f, 0.3f);

    private void OnDrawGizmos()
    {
        if (!_showGizmos || _config == null) return;

        Gizmos.color = _gizmoColor;

        // Draw grid cells
        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector3 center = GetGizmoCenter(x, y);
                Vector3 size = Vector3.one * _config.CellSize * 0.95f;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    private Vector3 GetGizmoCenter(int x, int y)
    {
        float posX = _config.Origin.x + x * _config.CellSize + _config.CellSize * 0.5f;
        float posY = _config.Origin.y + y * _config.CellSize + _config.CellSize * 0.5f;
        return new Vector3(posX, posY, _config.Origin.z);
    }
#endif
}
```

### Public API

| Member | Type | Description |
|--------|------|-------------|
| `Data` | `GridData` | Access to grid data for coordinate conversion |
| `Config` | `GridConfig` | Access to grid configuration |
| `GetCellVisual(Vector2Int)` | `GameObject` | Returns cell visual at position (nullable) |

### Dependencies

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `_config` | `GridConfig` | Yes | ScriptableObject with grid settings |
| `_cellPrefab` | `GameObject` | No | Optional prefab for cell visuals |
| `_cellsParent` | `Transform` | No | Parent for cell objects (defaults to self) |

---

## Scene Setup

### Hierarchy

```
Scene
└── Grid
    ├── GridView (component)
    └── Cells (empty, parent for cell visuals)
```

### Steps

1. Create empty GameObject "Grid" at position (0, 0, 0)
2. Add `GridView` component
3. Create `GridConfig` asset via **Assets > Create > Match3 > Grid Config**
4. Assign `GridConfig` to GridView
5. (Optional) Create cell prefab with SpriteRenderer
6. (Optional) Assign cell prefab and cells parent

---

## Coordinate System

```
        +Y
         │
    ┌────┼────┬────┬────┬────┬────┬────┬────┬────┐
    │7,7 │    │    │    │    │    │    │    │7,7 │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │    │    │    │    │    │    │    │    │    │
    ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
    │0,0 │1,0 │2,0 │3,0 │4,0 │5,0 │6,0 │7,0 │    │ ──────► +X
    └────┴────┴────┴────┴────┴────┴────┴────┴────┘
   Origin (0,0)
```

- **Origin:** Bottom-left corner at world (0, 0, 0)
- **GridToWorld:** Returns center of cell
- **Cell (0,0):** World position (0.5, 0.5, 0) with cellSize=1
- **Cell (7,7):** World position (7.5, 7.5, 0) with cellSize=1

---

## Integration with Other Systems

### BoardView (Step 3) will use:
```csharp
[SerializeField] private GridView _gridView;

void Start()
{
    GridData grid = _gridView.Data;
    Vector3 worldPos = grid.GridToWorld(new Vector2Int(3, 4));
}
```

### SpawnSystem (Step 3) will use:
```csharp
Vector3 spawnPos = gridData.GetSpawnPosition(column, rowsAbove: 2);
```

### SwipeDetector (Step 5) will use:
```csharp
Vector2Int gridPos = gridData.WorldToGrid(touchWorldPos);
if (gridData.IsValidPosition(gridPos)) { ... }
```

---

## Testing Checklist

- [ ] GridConfig creates via Assets menu
- [ ] GridView initializes GridData in Awake
- [ ] GridToWorld returns cell center
- [ ] WorldToGrid returns correct cell
- [ ] IsValidPosition returns false for (-1, 0), (8, 0), (0, -1), (0, 8)
- [ ] IsValidPosition returns true for (0, 0), (7, 7)
- [ ] Gizmos draw grid in Scene view
- [ ] Cell visuals spawn at correct positions (if prefab assigned)

---

## Notes

- GridData is a pure C# class for testability
- GridView owns and exposes GridData via property
- Cell visuals are optional (useful for debugging)
- All other systems access GridData through GridView.Data
