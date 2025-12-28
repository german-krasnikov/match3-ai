# Step 6: SWAP SYSTEM + INPUT - Обмен элементов

> **Модуль:** Система выбора и обмена элементов
> **Зависимости:** IGrid, IGridElement, ElementComponent (из шагов 1-3)
> **Используется в:** GameLoop (шаг 9)

---

## Обзор

Два компонента с чётким разделением ответственности:
- **InputComponent** — обрабатывает drag/swipe, определяет направление
- **SwapComponent** — выполняет обмен элементов с анимацией

```
[Drag: MouseDown → MouseUp] → InputComponent → OnSwapRequested(pos1, pos2) → SwapComponent → анимация
```

---

## Файлы

```
Assets/Scripts/
├── Core/Interfaces/
│   └── ISwapSystem.cs          # Интерфейс (уже в плане)
└── Swap/
    ├── InputComponent.cs       # Обработка ввода
    └── SwapComponent.cs        # Логика и анимация обмена
```

---

## 1. ISwapSystem.cs (интерфейс из Core)

```csharp
using System;
using System.Threading.Tasks;
using UnityEngine;

public interface ISwapSystem
{
    bool CanSwap(Vector2Int pos1, Vector2Int pos2);
    Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2);
    Task SwapBack(Vector2Int pos1, Vector2Int pos2);

    event Action<Vector2Int, Vector2Int> OnSwapStarted;
    event Action<Vector2Int, Vector2Int> OnSwapCompleted;
}
```

---

## 2. InputComponent.cs

### Ответственность
- Обработка drag/swipe жестов
- Конвертация screen → world → grid координат
- Определение направления свайпа (4 направления)
- Визуальный feedback начала drag (через событие)

### Реализация (Drag-based)

```csharp
using System;
using UnityEngine;

public class InputComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<Vector2Int, Vector2Int> OnSwapRequested;
    public event Action<Vector2Int> OnDragStarted;
    public event Action OnDragCanceled;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private Camera _camera;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _minDragDistance = 0.3f;

    // === СОСТОЯНИЕ ===
    private Vector2Int? _dragStartCell;
    private Vector3 _dragStartWorldPos;
    private bool _isDragging;
    private bool _inputEnabled = true;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled) CancelDrag();
    }

    // === UNITY CALLBACKS ===
    private void Update()
    {
        if (!_inputEnabled) return;

        if (Input.GetMouseButtonDown(0))
            HandleDragStart();
        else if (Input.GetMouseButtonUp(0) && _isDragging)
            HandleDragEnd();
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private void HandleDragStart()
    {
        Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = _grid.WorldToGrid(worldPos);

        if (!_grid.IsValidPosition(gridPos)) return;
        if (_grid.GetElementAt(gridPos) == null) return;

        _dragStartCell = gridPos;
        _dragStartWorldPos = worldPos;
        _isDragging = true;
        OnDragStarted?.Invoke(gridPos);
    }

    private void HandleDragEnd()
    {
        if (_dragStartCell == null)
        {
            CancelDrag();
            return;
        }

        Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 delta = worldPos - _dragStartWorldPos;

        if (delta.magnitude < _minDragDistance)
        {
            CancelDrag();
            return;
        }

        Vector2Int direction = GetSwipeDirection(delta);
        Vector2Int targetCell = _dragStartCell.Value + direction;

        if (_grid.IsValidPosition(targetCell) && _grid.GetElementAt(targetCell) != null)
        {
            OnSwapRequested?.Invoke(_dragStartCell.Value, targetCell);
        }

        CancelDrag();
    }

    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    private void CancelDrag()
    {
        if (_isDragging)
            OnDragCanceled?.Invoke();

        _dragStartCell = null;
        _isDragging = false;
    }
}
```

### Ключевые решения

| Решение | Обоснование |
|---------|-------------|
| Drag вместо двух кликов | Стандартный UX для Match-3 игр |
| `_minDragDistance` | Фильтрует случайные микро-движения |
| `GetSwipeDirection` | Определяет основное направление (4 стороны) |
| Проверка target cell | Нельзя свайпнуть за пределы или в пустоту |

---

## 3. SwapComponent.cs

### Ответственность
- Валидация swap (соседние ячейки)
- Обмен данных в Grid
- Анимация перемещения через DOTween
- Откат swap при неудаче

### Реализация

```csharp
using System;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class SwapComponent : MonoBehaviour, ISwapSystem
{
    // === СОБЫТИЯ ===
    public event Action<Vector2Int, Vector2Int> OnSwapStarted;
    public event Action<Vector2Int, Vector2Int> OnSwapCompleted;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _swapDuration = 0.2f;
    [SerializeField] private Ease _swapEase = Ease.OutQuad;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public bool CanSwap(Vector2Int pos1, Vector2Int pos2)
    {
        if (!_grid.IsValidPosition(pos1) || !_grid.IsValidPosition(pos2))
            return false;

        if (_grid.GetElementAt(pos1) == null || _grid.GetElementAt(pos2) == null)
            return false;

        return AreNeighbors(pos1, pos2);
    }

    public async Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2)
    {
        if (!CanSwap(pos1, pos2))
            return false;

        await ExecuteSwap(pos1, pos2);
        return true;
    }

    public async Task SwapBack(Vector2Int pos1, Vector2Int pos2)
    {
        await ExecuteSwap(pos1, pos2);
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private bool AreNeighbors(Vector2Int p1, Vector2Int p2)
    {
        int dx = Mathf.Abs(p1.x - p2.x);
        int dy = Mathf.Abs(p1.y - p2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private async Task ExecuteSwap(Vector2Int pos1, Vector2Int pos2)
    {
        OnSwapStarted?.Invoke(pos1, pos2);

        var element1 = _grid.GetElementAt(pos1);
        var element2 = _grid.GetElementAt(pos2);

        // Обмен в данных Grid
        _grid.SetElementAt(pos1, element2);
        _grid.SetElementAt(pos2, element1);

        // Обновление GridPosition в элементах
        element1.GridPosition = pos2;
        element2.GridPosition = pos1;

        // Анимация
        await AnimateSwap(element1.GameObject, element2.GameObject);

        OnSwapCompleted?.Invoke(pos1, pos2);
    }

    private async Task AnimateSwap(GameObject go1, GameObject go2)
    {
        Vector3 pos1 = go1.transform.position;
        Vector3 pos2 = go2.transform.position;

        var tween1 = go1.transform.DOMove(pos2, _swapDuration).SetEase(_swapEase);
        var tween2 = go2.transform.DOMove(pos1, _swapDuration).SetEase(_swapEase);

        // Ждём обе анимации
        await Task.WhenAll(
            tween1.AsyncWaitForCompletion(),
            tween2.AsyncWaitForCompletion()
        );
    }
}
```

### Ключевые решения

| Решение | Обоснование |
|---------|-------------|
| `async/await` | Чистый код, легко ждать завершения в GameLoop |
| Сначала данные, потом анимация | Grid сразу консистентен |
| `SwapBack` без проверки | GameLoop уже знает что swap валидный |
| `Task.WhenAll` | Параллельная анимация обоих элементов |

---

## 4. Stub-ы для изолированного тестирования

### StubGrid (для тестирования без реального Grid)

```csharp
#if UNITY_EDITOR
public class StubGrid : MonoBehaviour, IGrid
{
    private Dictionary<Vector2Int, IGridElement> _elements = new();

    public int Width => 8;
    public int Height => 8;
    public float CellSize => 1f;

    public Vector3 GridToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x, gridPos.y, 0);

    public Vector2Int WorldToGrid(Vector3 worldPos)
        => new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

    public bool IsValidPosition(Vector2Int pos)
        => pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

    public IGridElement GetElementAt(Vector2Int pos)
        => _elements.TryGetValue(pos, out var el) ? el : null;

    public void SetElementAt(Vector2Int pos, IGridElement element)
        => _elements[pos] = element;

    public void ClearCell(Vector2Int pos)
        => _elements.Remove(pos);

    public event Action<Vector2Int, IGridElement> OnElementPlaced;
    public event Action<Vector2Int> OnCellCleared;
}
#endif
```

### StubElement (для тестирования без реального Element)

```csharp
#if UNITY_EDITOR
public class StubElement : MonoBehaviour, IGridElement
{
    public Vector2Int GridPosition { get; set; }
    public ElementType Type => ElementType.Red;
    public GameObject GameObject => gameObject;
}
#endif
```

---

## 5. Интеграция с GameLoop (для справки)

GameLoop (шаг 9) будет использовать эти компоненты так:

```csharp
// В GameLoopComponent
private void OnEnable()
{
    _input.OnSwapRequested += OnSwapRequested;
}

private async void OnSwapRequested(Vector2Int pos1, Vector2Int pos2)
{
    _input.SetInputEnabled(false);  // Блокируем ввод

    bool swapped = await _swap.TrySwap(pos1, pos2);
    if (!swapped)
    {
        _input.SetInputEnabled(true);
        return;
    }

    var matches = _matchDetection.FindAllMatches();
    if (matches.Count == 0)
    {
        await _swap.SwapBack(pos1, pos2);  // Откат
    }
    else
    {
        // Продолжаем цикл destroy → gravity → check
    }

    _input.SetInputEnabled(true);
}
```

---

## 6. Визуальный feedback drag (опционально)

Простой компонент для подсветки ячейки во время drag:

```csharp
public class DragHighlightComponent : MonoBehaviour
{
    [SerializeField] private InputComponent _input;
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpriteRenderer _highlightPrefab;

    private SpriteRenderer _highlight;

    private void Awake()
    {
        _highlight = Instantiate(_highlightPrefab);
        _highlight.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _input.OnDragStarted += OnDragStarted;
        _input.OnDragCanceled += OnDragCanceled;
        _input.OnSwapRequested += OnSwapRequested;
    }

    private void OnDisable()
    {
        _input.OnDragStarted -= OnDragStarted;
        _input.OnDragCanceled -= OnDragCanceled;
        _input.OnSwapRequested -= OnSwapRequested;
    }

    private void OnDragStarted(Vector2Int pos)
    {
        _highlight.transform.position = _grid.GridToWorld(pos);
        _highlight.gameObject.SetActive(true);
    }

    private void OnDragCanceled() => _highlight.gameObject.SetActive(false);
    private void OnSwapRequested(Vector2Int _, Vector2Int __) => _highlight.gameObject.SetActive(false);
}
```

---

## 7. Подзадачи реализации

### Фаза 1: Базовая структура
- [ ] Создать папку `Assets/Scripts/Swap/`
- [ ] Убедиться что `ISwapSystem.cs` существует в Core/Interfaces
- [ ] Создать пустые классы InputComponent и SwapComponent

### Фаза 2: InputComponent
- [x] Реализовать обработку drag в Update (MouseDown/MouseUp)
- [x] Добавить конвертацию screen → world → grid
- [x] Реализовать GetSwipeDirection (4 направления)
- [x] Добавить события OnSwapRequested, OnDragStarted, OnDragCanceled
- [x] Добавить SetInputEnabled для блокировки

### Фаза 3: SwapComponent
- [ ] Реализовать AreNeighbors
- [ ] Реализовать CanSwap с проверками
- [ ] Реализовать ExecuteSwap с обменом данных
- [ ] Добавить DOTween анимацию
- [ ] Реализовать async/await паттерн
- [ ] Добавить события OnSwapStarted, OnSwapCompleted

### Фаза 4: Тестирование
- [ ] Создать тестовую сцену со Stub-ами
- [ ] Проверить выбор элементов кликом
- [ ] Проверить swap соседних элементов
- [ ] Проверить отказ swap не-соседних
- [ ] Проверить SwapBack анимацию

---

## 8. Критические точки

### Порядок операций в ExecuteSwap
```
1. Событие OnSwapStarted
2. Обмен в _grid (SetElementAt)
3. Обновление GridPosition в элементах
4. Анимация (await)
5. Событие OnSwapCompleted
```

**Важно:** Данные обновляются ДО анимации. Grid всегда консистентен.

### Обработка edge cases

| Случай | Поведение |
|--------|-----------|
| Drag начат вне сетки | Игнорируется |
| Drag начат на пустой ячейке | Игнорируется |
| Drag слишком короткий | `OnDragCanceled`, нет swap |
| Drag в сторону пустой/невалидной ячейки | `OnDragCanceled`, нет swap |
| Drag во время анимации | Заблокирован через `_inputEnabled` |

---

## 9. DOTween заметки

```csharp
// Убедиться что DOTween инициализирован
DOTween.Init();

// AsyncWaitForCompletion возвращает Task
await tween.AsyncWaitForCompletion();

// Можно добавить juice эффекты
go.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
```

---

## 10. Диаграмма потока данных

```
┌──────────────────┐
│  Mouse Down      │
│  (start drag)    │
└────────┬─────────┘
         ▼
┌──────────────────┐
│ InputComponent   │
│ - Save start pos │
│ - OnDragStarted  │
└────────┬─────────┘
         │
┌────────▼─────────┐
│  Mouse Up        │
│  (end drag)      │
└────────┬─────────┘
         ▼
┌──────────────────┐
│ InputComponent   │
│ - Calc delta     │
│ - GetSwipeDir    │
│ - Validate       │
└────────┬─────────┘
         │ OnSwapRequested(pos1, pos2)
         ▼
┌──────────────────┐
│  SwapComponent   │
│ - AreNeighbors   │
│ - Grid.SetAt     │
│ - DOTween anim   │
└────────┬─────────┘
         │ OnSwapCompleted
         ▼
┌──────────────────┐
│    GameLoop      │
│ (проверка мат.)  │
└──────────────────┘
```

---

## Готово к реализации ✓

После создания Grid (шаг 2) и Elements (шаг 3) этот модуль можно полностью реализовать и протестировать изолированно.
