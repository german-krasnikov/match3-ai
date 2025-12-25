# Этап 1: Grid System — Детальный план реализации

## Статус: ✅ ЗАВЕРШЕНО

---

## Обзор

Базовая система сетки для Match-3. Отвечает за:
- Хранение параметров сетки (ScriptableObject)
- Конвертацию координат grid ↔ world
- Debug-визуализацию в Editor

---

## Созданные файлы

```
Assets/
├── Scripts/
│   ├── Grid/
│   │   ├── GridData.cs        # ScriptableObject с параметрами
│   │   ├── Cell.cs            # Struct ячейки
│   │   └── GridComponent.cs   # MonoBehaviour + Gizmos
│   └── Editor/
│       └── GridSceneSetup.cs  # Автонастройка сцены
└── Data/
    └── Grid/
        └── DefaultGridData.asset  # (создаётся автоматически)
```

---

## Быстрый старт

**Menu → Match3 → Setup Scene → Stage 1 - Grid System**

Автоматически:
- Создаёт GridData ассет (8x8, cellSize=1, spacing=0.1)
- Создаёт GameObject "Grid" с GridComponent
- Центрирует Main Camera на сетку
- Выделяет Grid и фокусирует Scene View

---

## 1. GridData.cs — ScriptableObject

### Назначение
Конфигурация сетки. Один ассет можно переиспользовать для разных уровней.

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
        public float Step => _cellSize + _spacing;

        private void OnValidate()
        {
            _width = Mathf.Max(1, _width);
            _height = Mathf.Max(1, _height);
            _cellSize = Mathf.Max(0.1f, _cellSize);
            _spacing = Mathf.Max(0f, _spacing);
        }
    }
}
```

---

## 2. Cell.cs — Struct

### Назначение
Минимальная структура данных ячейки. `isBlocked` добавим позже.

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
| `Vector3 GridToWorld(Vector2Int pos)` | Grid → World (центр ячейки) |
| `Vector2Int WorldToGrid(Vector3 pos)` | World → Grid |
| `bool IsValidPosition(Vector2Int pos)` | Проверка границ |
| `Cell GetCell(Vector2Int pos)` | Получить ячейку |

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
        public event Action OnGridReady;

        [Header("Configuration")]
        [SerializeField] private GridData _gridData;

        private Cell[,] _cells;

        public int Width => _gridData.Width;
        public int Height => _gridData.Height;
        public GridData Data => _gridData;

        private void Awake()
        {
            InitializeGrid();
            OnGridReady?.Invoke();
        }

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

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float step = _gridData.Step;
            float halfCell = _gridData.CellSize * 0.5f;

            float x = transform.position.x + gridPos.x * step + halfCell;
            float y = transform.position.y + gridPos.y * step + halfCell;

            return new Vector3(x, y, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float step = _gridData.Step;
            Vector3 localPos = worldPos - transform.position;

            int x = Mathf.FloorToInt(localPos.x / step);
            int y = Mathf.FloorToInt(localPos.y / step);

            return new Vector2Int(x, y);
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width &&
                   pos.y >= 0 && pos.y < Height;
        }

        public Cell GetCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), $"Position {pos} is outside grid bounds");

            return _cells[pos.x, pos.y];
        }

        private void OnDrawGizmos()
        {
            if (_gridData == null) return;
            DrawGridGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (_gridData == null) return;
            DrawGridGizmos(true);
        }

        private void DrawGridGizmos(bool selected)
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

## 4. GridSceneSetup.cs — Editor Script

### Назначение
Автоматическая настройка сцены для тестирования Grid System.

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Grid;

namespace Match3.Editor
{
    public static class GridSceneSetup
    {
        private const string GridDataPath = "Assets/Data/Grid/DefaultGridData.asset";
        private const string GridObjectName = "Grid";

        [MenuItem("Match3/Setup Scene/Stage 1 - Grid System")]
        public static void SetupGridScene()
        {
            var gridData = GetOrCreateGridData();
            var gridComponent = GetOrCreateGridObject(gridData);
            SetupCamera(gridData);

            Selection.activeGameObject = gridComponent.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[Match3] Grid System setup complete!");
        }

        private static GridData GetOrCreateGridData()
        {
            var gridData = AssetDatabase.LoadAssetAtPath<GridData>(GridDataPath);

            if (gridData == null)
            {
                gridData = ScriptableObject.CreateInstance<GridData>();

                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                    AssetDatabase.CreateFolder("Assets", "Data");
                if (!AssetDatabase.IsValidFolder("Assets/Data/Grid"))
                    AssetDatabase.CreateFolder("Assets/Data", "Grid");

                AssetDatabase.CreateAsset(gridData, GridDataPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[Match3] Created GridData at {GridDataPath}");
            }

            return gridData;
        }

        private static GridComponent GetOrCreateGridObject(GridData gridData)
        {
            var existingGrid = Object.FindFirstObjectByType<GridComponent>();

            if (existingGrid != null)
            {
                var so = new SerializedObject(existingGrid);
                so.FindProperty("_gridData").objectReferenceValue = gridData;
                so.ApplyModifiedProperties();

                Debug.Log("[Match3] Updated existing Grid object");
                return existingGrid;
            }

            var gridObject = new GameObject(GridObjectName);
            gridObject.transform.position = Vector3.zero;

            var gridComponent = gridObject.AddComponent<GridComponent>();

            var serializedObject = new SerializedObject(gridComponent);
            serializedObject.FindProperty("_gridData").objectReferenceValue = gridData;
            serializedObject.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(gridObject, "Create Grid");

            Debug.Log("[Match3] Created Grid object");
            return gridComponent;
        }

        private static void SetupCamera(GridData gridData)
        {
            var camera = Camera.main;
            if (camera == null) return;

            float step = gridData.Step;

            float centerX = gridData.Width * step * 0.5f - gridData.Spacing * 0.5f;
            float centerY = gridData.Height * step * 0.5f - gridData.Spacing * 0.5f;

            camera.transform.position = new Vector3(centerX, centerY, -10f);

            if (camera.orthographic)
            {
                float gridHeight = gridData.Height * step;
                camera.orthographicSize = gridHeight * 0.6f;
            }

            Debug.Log("[Match3] Camera positioned to grid center");
        }
    }
}
#endif
```

---

## Диаграмма координат

```
         Y ↑
           │
     (0,7) │ ┌───┐ ┌───┐ ... ┌───┐ (7,7)
           │ └───┘ └───┘     └───┘
           │   ...
     (0,0) │ ┌───┐ ┌───┐ ... ┌───┐ (7,0)
           │ └───┘ └───┘     └───┘
         ──┼─────────────────────────→ X
           │
     transform.position = (0,0)

step = cellSize + spacing = 1.0 + 0.1 = 1.1
Центр ячейки (0,0) = (0.5, 0.5)
Центр ячейки (1,0) = (1.6, 0.5)
```

---

## Gizmos

- **Не выделен:** серая полупрозрачная сетка
- **Выделен:** cyan ячейки + жёлтая внешняя рамка

---

## Использование в следующих этапах

```csharp
// Позиционирование элемента
element.transform.position = _grid.GridToWorld(gridPos);

// Клик → ячейка
Vector2Int cell = _grid.WorldToGrid(clickWorldPos);
if (_grid.IsValidPosition(cell))
{
    // обработка
}

// Подписка на готовность
_grid.OnGridReady += OnGridInitialized;
```

---

## Следующий этап

**Этап 2: Elements** — ElementType, ElementData, ElementComponent, ElementDatabase
