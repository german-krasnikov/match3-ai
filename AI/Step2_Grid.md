# Step 2: Grid System - План реализации

## Цель
Создать систему сетки: конфиг, компонент сетки и ячейки. Реализовать интерфейсы IGrid и IBoardState из Step 1.

---

## Зависимости от Step 1

```csharp
// Используем из Core:
- GridPosition      // координаты
- IGrid            // интерфейс сетки
- IBoardState      // интерфейс состояния доски
- IPiece           // интерфейс элемента (для хранения в ячейках)
```

---

## Структура файлов

```
Assets/
├── Scripts/
│   └── Grid/
│       ├── GridConfig.cs       # ScriptableObject конфига
│       ├── GridComponent.cs    # Компонент сетки (IGrid + IBoardState)
│       └── CellComponent.cs    # Компонент ячейки
│
├── Configs/
│   └── GridConfig.asset        # Инстанс конфига (создать в Unity)
│
└── Prefabs/
    └── Cell.prefab             # Префаб ячейки (создать в Unity)
```

---

## 1. GridConfig.cs

**Назначение:** ScriptableObject с настройками сетки.

```csharp
using UnityEngine;

namespace Match3.Grid
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Size")]
        [Range(5, 12)] public int Width = 8;
        [Range(5, 12)] public int Height = 8;

        [Header("Cell Settings")]
        public float CellSize = 1f;
        public float CellSpacing = 0.1f;

        [Header("Prefabs")]
        public GameObject CellPrefab;

        /// <summary>
        /// Полный размер ячейки с учётом spacing
        /// </summary>
        public float TotalCellSize => CellSize + CellSpacing;

        /// <summary>
        /// Смещение для центрирования сетки
        /// </summary>
        public Vector3 GridOffset => new(
            -(Width - 1) * TotalCellSize / 2f,
            -(Height - 1) * TotalCellSize / 2f,
            0f
        );
    }
}
```

**Ключевые решения:**
- `TotalCellSize` — вычисляемое свойство для удобства
- `GridOffset` — автоматическое центрирование сетки
- `CellPrefab` — ссылка на префаб ячейки

---

## 2. CellComponent.cs

**Назначение:** Компонент отдельной ячейки. Хранит позицию и ссылку на элемент.

```csharp
using Match3.Core;
using UnityEngine;

namespace Match3.Grid
{
    public class CellComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private GridPosition _position;
        private IPiece _piece;

        public GridPosition Position => _position;
        public IPiece Piece => _piece;
        public bool IsEmpty => _piece == null;

        public void Initialize(GridPosition position)
        {
            _position = position;
            gameObject.name = $"Cell_{position.X}_{position.Y}";
        }

        public void SetPiece(IPiece piece)
        {
            _piece = piece;
            if (piece != null)
            {
                piece.Position = _position;
            }
        }

        public IPiece RemovePiece()
        {
            var piece = _piece;
            _piece = null;
            return piece;
        }

        public void Clear()
        {
            _piece = null;
        }
    }
}
```

**Ключевые решения:**
- Минимальная ответственность: только хранение позиции и ссылки на piece
- `SetPiece` автоматически обновляет позицию у piece
- `RemovePiece` возвращает piece и очищает ячейку

---

## 3. GridComponent.cs

**Назначение:** Главный компонент сетки. Реализует IGrid и IBoardState.

```csharp
using System;
using System.Collections.Generic;
using Match3.Core;
using UnityEngine;

namespace Match3.Grid
{
    public class GridComponent : MonoBehaviour, IGrid, IBoardState
    {
        [Header("Config")]
        [SerializeField] private GridConfig _config;

        private CellComponent[,] _cells;

        // === IGrid ===
        public int Width => _config.Width;
        public int Height => _config.Height;

        // === IBoardState ===
        public event Action OnBoardChanged;

        public IEnumerable<GridPosition> AllPositions
        {
            get
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        yield return new GridPosition(x, y);
            }
        }

        // === Events ===
        public event Action OnGridInitialized;

        private void Awake()
        {
            CreateGrid();
        }

        private void CreateGrid()
        {
            _cells = new CellComponent[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var position = new GridPosition(x, y);
                    var worldPos = GridToWorld(position);

                    var cellGO = Instantiate(_config.CellPrefab, worldPos, Quaternion.identity, transform);
                    var cell = cellGO.GetComponent<CellComponent>();
                    cell.Initialize(position);

                    _cells[x, y] = cell;
                }
            }

            OnGridInitialized?.Invoke();
        }

        // === IGrid Implementation ===

        public Vector3 GridToWorld(GridPosition position)
        {
            return transform.position + _config.GridOffset + new Vector3(
                position.X * _config.TotalCellSize,
                position.Y * _config.TotalCellSize,
                0f
            );
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            var local = worldPosition - transform.position - _config.GridOffset;
            int x = Mathf.RoundToInt(local.x / _config.TotalCellSize);
            int y = Mathf.RoundToInt(local.y / _config.TotalCellSize);
            return new GridPosition(x, y);
        }

        public bool IsValidPosition(GridPosition position)
        {
            return position.X >= 0 && position.X < Width &&
                   position.Y >= 0 && position.Y < Height;
        }

        // === IBoardState Implementation ===

        public IPiece GetPieceAt(GridPosition position)
        {
            if (!IsValidPosition(position)) return null;
            return _cells[position.X, position.Y].Piece;
        }

        public void SetPieceAt(GridPosition position, IPiece piece)
        {
            if (!IsValidPosition(position)) return;
            _cells[position.X, position.Y].SetPiece(piece);
            OnBoardChanged?.Invoke();
        }

        public void ClearCell(GridPosition position)
        {
            if (!IsValidPosition(position)) return;
            _cells[position.X, position.Y].Clear();
            OnBoardChanged?.Invoke();
        }

        public bool IsEmpty(GridPosition position)
        {
            if (!IsValidPosition(position)) return false;
            return _cells[position.X, position.Y].IsEmpty;
        }

        // === Utility ===

        public CellComponent GetCell(GridPosition position)
        {
            if (!IsValidPosition(position)) return null;
            return _cells[position.X, position.Y];
        }
    }
}
```

**Ключевые решения:**
- Реализует оба интерфейса: `IGrid` и `IBoardState`
- `AllPositions` — генератор для итерации по всем позициям
- `CreateGrid()` в Awake — автоматическое создание сетки
- `GetCell()` — дополнительный метод для прямого доступа к ячейке

---

## Чеклист реализации

### Код
- [ ] Создать папку `Assets/Scripts/Grid/`
- [ ] Реализовать `GridConfig.cs`
- [ ] Реализовать `CellComponent.cs`
- [ ] Реализовать `GridComponent.cs`

### Unity Editor
- [ ] Создать папку `Assets/Configs/`
- [ ] Создать `GridConfig.asset` (ПКМ → Create → Match3 → Grid Config)
- [ ] Создать папку `Assets/Prefabs/`
- [ ] Создать `Cell.prefab`:
  - Создать пустой GameObject "Cell"
  - Добавить SpriteRenderer (квадрат/спрайт ячейки)
  - Добавить CellComponent
  - Сохранить как префаб
- [ ] Привязать Cell.prefab к GridConfig.CellPrefab
- [ ] Создать пустой GameObject "Grid" на сцене
- [ ] Добавить GridComponent
- [ ] Привязать GridConfig к GridComponent

### Тестирование
- [ ] Запустить Play Mode
- [ ] Убедиться что сетка 8x8 создаётся
- [ ] Проверить центрирование

---

## Визуальная схема

```
GridComponent (GameObject "Grid")
    │
    ├── реализует IGrid (конвертация координат)
    ├── реализует IBoardState (CRUD для pieces)
    │
    └── содержит CellComponent[8,8]
            │
            ├── Cell_0_0
            ├── Cell_1_0
            ├── ...
            └── Cell_7_7
```

---

## Связь с другими модулями

**Предоставляет:**
- `IGrid` — для Pieces, Spawner (конвертация координат)
- `IBoardState` — для Gravity, Swap, Match (состояние доски)
- `OnGridInitialized` — для GameController (старт игры)

**Получает:**
- `IPiece` — хранит в ячейках (из Step 3)

---

## Время выполнения

~20-25 минут (код + настройка в Unity Editor)
