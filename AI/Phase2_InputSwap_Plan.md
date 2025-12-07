# Phase 2: Input & Swap — План реализации

## Обзор

Реализация системы ввода и обмена тайлов для Match3 игры.

**Зависимости:** Phase 1 (Grid, Cell, Tile) — готов
**Технологии:** Old Input System, DOTween
**Платформы:** Desktop (mouse) + Mobile (touch)

---

## Архитектура компонентов

```
┌─────────────────────────────────────────────────────────────┐
│                      BoardController                         │
│         (существующий, будет расширен событиями)            │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ события
┌─────────────────┐    ┌──────┴──────┐    ┌─────────────────┐
│ InputController │───▶│SwapController│───▶│  SwapAnimator   │
│  (ввод/свайп)   │    │   (логика)   │    │   (анимация)    │
└─────────────────┘    └──────────────┘    └─────────────────┘
         │                    │
         ▼                    ▼
┌─────────────────┐    ┌──────────────┐
│SelectionVisual  │    │ SwapValidator │
│ (подсветка)     │    │ (валидация)   │
└─────────────────┘    └──────────────┘
```

---

## 1. Интерфейсы

### 1.1 ISwappable
```csharp
// Scripts/Interfaces/ISwappable.cs
public interface ISwappable
{
    Vector2Int GridPosition { get; }
    bool CanSwap { get; }
    void SetGridPosition(Vector2Int position);
}
```

**Зачем:** TileComponent реализует этот интерфейс. Позволяет SwapController работать с любым swappable объектом.

### 1.2 ISelectable
```csharp
// Scripts/Interfaces/ISelectable.cs
public interface ISelectable
{
    event Action<ISelectable> OnSelected;
    event Action<ISelectable> OnDeselected;
    void Select();
    void Deselect();
    bool IsSelected { get; }
}
```

**Зачем:** Абстракция выбора. TileComponent реализует для визуального фидбека.

---

## 2. InputController

**Файл:** `Scripts/Input/InputController.cs`
**Ответственность:** Обработка touch/mouse, определение свайпа, преобразование screen→world→grid координат.

### 2.1 Состояния ввода
```csharp
public enum InputState
{
    Idle,           // Ожидание
    TileSelected,   // Тайл выбран, ждём направление
    Dragging,       // Перетаскивание (свайп)
    Blocked         // Заблокирован (анимация/обработка)
}
```

### 2.2 События
```csharp
public event Action<Vector2Int> OnTilePressed;      // Нажали на тайл
public event Action<Vector2Int> OnTileReleased;     // Отпустили
public event Action<Vector2Int, SwipeDirection> OnSwipe;  // Свайп определён
public event Action OnInputCancelled;               // Отмена (вышли за поле)
```

### 2.3 SwipeDirection
```csharp
public enum SwipeDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}
```

### 2.4 Ключевая логика

```csharp
public class InputController : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<Vector2Int> OnTilePressed;
    public event Action<Vector2Int, SwipeDirection> OnSwipe;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _swipeThreshold = 0.5f;  // Минимальная длина свайпа в юнитах
    [SerializeField] private LayerMask _tileLayer;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private Camera _camera;
    [SerializeField] private GridComponent _grid;

    // === СОСТОЯНИЕ ===
    private InputState _state = InputState.Idle;
    private Vector2 _pressStartPosition;
    private Vector2Int _selectedGridPosition;

    // === CONDITION SYSTEM ===
    private readonly AndCondition _inputCondition = new();

    public void AddCondition(Func<bool> condition)
        => _inputCondition.AddCondition(condition);

    public void SetBlocked(bool blocked)
        => _state = blocked ? InputState.Blocked : InputState.Idle;

    private void Update()
    {
        if (_state == InputState.Blocked) return;
        if (!_inputCondition.IsTrue()) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // Unified: mouse и touch через одинаковую логику
        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && _state == InputState.TileSelected)
        {
            OnPointerDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnPointerUp();
        }
    }

    private void OnPointerDown(Vector2 screenPosition)
    {
        Vector2 worldPos = _camera.ScreenToWorldPoint(screenPosition);

        if (!TryGetGridPosition(worldPos, out Vector2Int gridPos))
            return;

        _pressStartPosition = worldPos;
        _selectedGridPosition = gridPos;
        _state = InputState.TileSelected;

        OnTilePressed?.Invoke(gridPos);
    }

    private void OnPointerDrag(Vector2 screenPosition)
    {
        Vector2 worldPos = _camera.ScreenToWorldPoint(screenPosition);
        Vector2 delta = worldPos - _pressStartPosition;

        if (delta.magnitude < _swipeThreshold)
            return;

        SwipeDirection direction = GetSwipeDirection(delta);

        if (direction != SwipeDirection.None)
        {
            _state = InputState.Idle;
            OnSwipe?.Invoke(_selectedGridPosition, direction);
        }
    }

    private void OnPointerUp()
    {
        _state = InputState.Idle;
    }

    private SwipeDirection GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }

    private bool TryGetGridPosition(Vector2 worldPos, out Vector2Int gridPos)
    {
        // Использует GridComponent для конвертации world → grid
        return _grid.TryWorldToGrid(worldPos, out gridPos);
    }
}
```

### 2.5 Требования к GridComponent

Добавить методы в существующий `GridComponent`:
```csharp
public bool TryWorldToGrid(Vector2 worldPos, out Vector2Int gridPos);
public Vector2 GridToWorld(Vector2Int gridPos);
public bool IsValidPosition(Vector2Int pos);
```

---

## 3. SelectionVisualComponent

**Файл:** `Scripts/Components/Visual/SelectionVisualComponent.cs`
**Ответственность:** Визуальная подсветка выбранного тайла.

### 3.1 Реализация

```csharp
public class SelectionVisualComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color _selectionColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private float _pulseScale = 1.1f;
    [SerializeField] private float _pulseDuration = 0.3f;

    [Header("Dependencies")]
    [SerializeField] private SpriteRenderer _highlightRenderer;

    private Tweener _pulseTween;

    public void Show(Vector2 position)
    {
        transform.position = position;
        _highlightRenderer.enabled = true;
        _highlightRenderer.color = _selectionColor;

        // Пульсация
        _pulseTween?.Kill();
        _pulseTween = transform
            .DOScale(_pulseScale, _pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Hide()
    {
        _pulseTween?.Kill();
        transform.localScale = Vector3.one;
        _highlightRenderer.enabled = false;
    }
}
```

---

## 4. SwapValidator

**Файл:** `Scripts/Core/SwapValidator.cs`
**Ответственность:** Проверка валидности обмена (соседние ячейки, movable тайлы, результат даёт матч).

### 4.1 Реализация

```csharp
public class SwapValidator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private MatchDetector _matchDetector;  // из Phase 3, пока stub

    /// <summary>
    /// Проверяет можно ли поменять тайлы местами
    /// </summary>
    public bool IsValidSwap(Vector2Int posA, Vector2Int posB)
    {
        // 1. Позиции в пределах поля?
        if (!_grid.IsValidPosition(posA) || !_grid.IsValidPosition(posB))
            return false;

        // 2. Соседние ячейки?
        if (!AreNeighbors(posA, posB))
            return false;

        // 3. Обе ячейки содержат movable тайлы?
        var cellA = _grid.GetCell(posA);
        var cellB = _grid.GetCell(posB);

        if (cellA?.CurrentTile == null || cellB?.CurrentTile == null)
            return false;

        if (!cellA.CurrentTile.CanSwap || !cellB.CurrentTile.CanSwap)
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет даст ли обмен матч (для отката)
    /// </summary>
    public bool WillCreateMatch(Vector2Int posA, Vector2Int posB)
    {
        // Временно меняем местами для проверки
        // MatchDetector из Phase 3, пока возвращаем true
        // TODO: реализовать когда будет MatchDetector
        return true;
    }

    private bool AreNeighbors(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        // Соседние = разница ровно 1 по одной оси и 0 по другой
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
}
```

---

## 5. SwapAnimator

**Файл:** `Scripts/Components/Animation/SwapAnimator.cs`
**Ответственность:** Анимация обмена тайлов (DOTween).

### 5.1 Реализация

```csharp
public class SwapAnimator : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnSwapAnimationComplete;
    public event Action OnRevertAnimationComplete;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _swapDuration = 0.2f;
    [SerializeField] private Ease _swapEase = Ease.OutQuad;
    [SerializeField] private float _revertDuration = 0.15f;
    [SerializeField] private Ease _revertEase = Ease.InQuad;

    /// <summary>
    /// Анимация обмена двух тайлов
    /// </summary>
    public void AnimateSwap(Transform tileA, Transform tileB, bool isRevert = false)
    {
        float duration = isRevert ? _revertDuration : _swapDuration;
        Ease ease = isRevert ? _revertEase : _swapEase;

        Vector3 posA = tileA.position;
        Vector3 posB = tileB.position;

        var sequence = DOTween.Sequence();

        sequence.Join(tileA.DOMove(posB, duration).SetEase(ease));
        sequence.Join(tileB.DOMove(posA, duration).SetEase(ease));

        sequence.OnComplete(() =>
        {
            if (isRevert)
                OnRevertAnimationComplete?.Invoke();
            else
                OnSwapAnimationComplete?.Invoke();
        });
    }
}
```

---

## 6. SwapController

**Файл:** `Scripts/Core/SwapController.cs`
**Ответственность:** Оркестрация процесса обмена (валидация → анимация → проверка матча → откат).

### 6.1 События
```csharp
public event Action OnSwapStarted;
public event Action<Vector2Int, Vector2Int> OnSwapCompleted;  // Успешный обмен
public event Action OnSwapFailed;   // Откат (нет матча)
public event Action OnSwapInvalid;  // Невалидный свап (не соседи и т.д.)
```

### 6.2 Состояния
```csharp
private enum SwapState
{
    Idle,
    Animating,
    WaitingForMatchCheck,
    Reverting
}
```

### 6.3 Реализация

```csharp
public class SwapController : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnSwapStarted;
    public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
    public event Action OnSwapFailed;
    public event Action OnSwapInvalid;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SwapValidator _validator;
    [SerializeField] private SwapAnimator _animator;

    // === СОСТОЯНИЕ ===
    private SwapState _state = SwapState.Idle;
    private Vector2Int _swapPosA;
    private Vector2Int _swapPosB;
    private TileComponent _tileA;
    private TileComponent _tileB;

    public bool IsProcessing => _state != SwapState.Idle;

    private void OnEnable()
    {
        _animator.OnSwapAnimationComplete += OnSwapAnimationComplete;
        _animator.OnRevertAnimationComplete += OnRevertAnimationComplete;
    }

    private void OnDisable()
    {
        _animator.OnSwapAnimationComplete -= OnSwapAnimationComplete;
        _animator.OnRevertAnimationComplete -= OnRevertAnimationComplete;
    }

    /// <summary>
    /// Попытка обмена тайла с соседом в указанном направлении
    /// </summary>
    public void TrySwap(Vector2Int fromPos, SwipeDirection direction)
    {
        if (_state != SwapState.Idle)
            return;

        Vector2Int toPos = GetTargetPosition(fromPos, direction);
        TrySwap(fromPos, toPos);
    }

    /// <summary>
    /// Попытка обмена двух тайлов по позициям
    /// </summary>
    public void TrySwap(Vector2Int posA, Vector2Int posB)
    {
        if (_state != SwapState.Idle)
            return;

        // Валидация
        if (!_validator.IsValidSwap(posA, posB))
        {
            OnSwapInvalid?.Invoke();
            return;
        }

        // Запоминаем позиции и тайлы
        _swapPosA = posA;
        _swapPosB = posB;
        _tileA = _grid.GetCell(posA).CurrentTile;
        _tileB = _grid.GetCell(posB).CurrentTile;

        // Начинаем обмен
        _state = SwapState.Animating;
        OnSwapStarted?.Invoke();

        _animator.AnimateSwap(_tileA.transform, _tileB.transform);
    }

    private void OnSwapAnimationComplete()
    {
        // Обновляем данные в Grid
        SwapTilesInGrid(_swapPosA, _swapPosB);

        // Проверяем создался ли матч
        if (_validator.WillCreateMatch(_swapPosA, _swapPosB))
        {
            // Успех!
            _state = SwapState.Idle;
            OnSwapCompleted?.Invoke(_swapPosA, _swapPosB);
        }
        else
        {
            // Нет матча — откатываем
            _state = SwapState.Reverting;
            _animator.AnimateSwap(_tileA.transform, _tileB.transform, isRevert: true);
        }
    }

    private void OnRevertAnimationComplete()
    {
        // Возвращаем данные в Grid
        SwapTilesInGrid(_swapPosA, _swapPosB);

        _state = SwapState.Idle;
        OnSwapFailed?.Invoke();
    }

    private void SwapTilesInGrid(Vector2Int posA, Vector2Int posB)
    {
        var cellA = _grid.GetCell(posA);
        var cellB = _grid.GetCell(posB);

        // Обмен ссылок
        (cellA.CurrentTile, cellB.CurrentTile) = (cellB.CurrentTile, cellA.CurrentTile);

        // Обновляем позиции в самих тайлах
        cellA.CurrentTile?.SetGridPosition(posA);
        cellB.CurrentTile?.SetGridPosition(posB);
    }

    private Vector2Int GetTargetPosition(Vector2Int from, SwipeDirection direction)
    {
        return direction switch
        {
            SwipeDirection.Up => from + Vector2Int.up,
            SwipeDirection.Down => from + Vector2Int.down,
            SwipeDirection.Left => from + Vector2Int.left,
            SwipeDirection.Right => from + Vector2Int.right,
            _ => from
        };
    }
}
```

---

## 7. Интеграция: BoardInputHandler

**Файл:** `Scripts/Core/BoardInputHandler.cs`
**Ответственность:** Связывает InputController, SwapController, SelectionVisual. Оркестратор ввода для доски.

### 7.1 Реализация

```csharp
public class BoardInputHandler : MonoBehaviour
{
    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private InputController _inputController;
    [SerializeField] private SwapController _swapController;
    [SerializeField] private SelectionVisualComponent _selectionVisual;
    [SerializeField] private GridComponent _grid;

    // === AUDIO ===
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _selectSound;
    [SerializeField] private AudioClip _swapSound;
    [SerializeField] private AudioClip _invalidSwapSound;

    private void OnEnable()
    {
        // Input events
        _inputController.OnTilePressed += OnTilePressed;
        _inputController.OnSwipe += OnSwipe;

        // Swap events
        _swapController.OnSwapStarted += OnSwapStarted;
        _swapController.OnSwapCompleted += OnSwapCompleted;
        _swapController.OnSwapFailed += OnSwapFailed;
        _swapController.OnSwapInvalid += OnSwapInvalid;
    }

    private void OnDisable()
    {
        _inputController.OnTilePressed -= OnTilePressed;
        _inputController.OnSwipe -= OnSwipe;

        _swapController.OnSwapStarted -= OnSwapStarted;
        _swapController.OnSwapCompleted -= OnSwapCompleted;
        _swapController.OnSwapFailed -= OnSwapFailed;
        _swapController.OnSwapInvalid -= OnSwapInvalid;
    }

    private void Awake()
    {
        // Блокируем ввод когда идёт swap
        _inputController.AddCondition(() => !_swapController.IsProcessing);
    }

    // === INPUT HANDLERS ===

    private void OnTilePressed(Vector2Int gridPos)
    {
        Vector2 worldPos = _grid.GridToWorld(gridPos);
        _selectionVisual.Show(worldPos);
        PlaySound(_selectSound);
    }

    private void OnSwipe(Vector2Int fromPos, SwipeDirection direction)
    {
        _selectionVisual.Hide();
        _swapController.TrySwap(fromPos, direction);
    }

    // === SWAP HANDLERS ===

    private void OnSwapStarted()
    {
        PlaySound(_swapSound);
    }

    private void OnSwapCompleted(Vector2Int posA, Vector2Int posB)
    {
        // Здесь будет вызов MatchDetector (Phase 3)
        Debug.Log($"Swap completed: {posA} <-> {posB}");
    }

    private void OnSwapFailed()
    {
        // Можно добавить shake анимацию
        Debug.Log("Swap failed - no match");
    }

    private void OnSwapInvalid()
    {
        _selectionVisual.Hide();
        PlaySound(_invalidSwapSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}
```

---

## 8. Изменения в существующих компонентах

### 8.1 GridComponent (добавить методы)

```csharp
// Добавить в существующий GridComponent

[SerializeField] private float _cellSize = 1f;
[SerializeField] private Vector2 _originOffset;

public bool TryWorldToGrid(Vector2 worldPos, out Vector2Int gridPos)
{
    gridPos = default;

    Vector2 localPos = worldPos - _originOffset;
    int x = Mathf.FloorToInt(localPos.x / _cellSize);
    int y = Mathf.FloorToInt(localPos.y / _cellSize);

    gridPos = new Vector2Int(x, y);
    return IsValidPosition(gridPos);
}

public Vector2 GridToWorld(Vector2Int gridPos)
{
    return new Vector2(
        gridPos.x * _cellSize + _cellSize / 2f + _originOffset.x,
        gridPos.y * _cellSize + _cellSize / 2f + _originOffset.y
    );
}

public bool IsValidPosition(Vector2Int pos)
{
    return pos.x >= 0 && pos.x < _width &&
           pos.y >= 0 && pos.y < _height;
}
```

### 8.2 TileComponent (реализовать ISwappable)

```csharp
// Добавить в существующий TileComponent

public class TileComponent : MonoBehaviour, ISwappable
{
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private bool _canSwap = true;

    public Vector2Int GridPosition => _gridPosition;
    public bool CanSwap => _canSwap && !_isMoving;

    private bool _isMoving;

    public void SetGridPosition(Vector2Int position)
    {
        _gridPosition = position;
    }

    public void SetMoving(bool moving)
    {
        _isMoving = moving;
    }
}
```

### 8.3 CellComponent (добавить свойство)

```csharp
// Добавить в существующий CellComponent

public TileComponent CurrentTile { get; set; }
```

---

## 9. Структура файлов

```
Assets/Scripts/
├── Interfaces/
│   ├── ISwappable.cs          ← NEW
│   └── ISelectable.cs         ← NEW
├── Input/
│   └── InputController.cs     ← NEW
├── Core/
│   ├── SwapController.cs      ← NEW
│   ├── SwapValidator.cs       ← NEW
│   └── BoardInputHandler.cs   ← NEW
├── Components/
│   ├── Visual/
│   │   └── SelectionVisualComponent.cs  ← NEW
│   ├── Animation/
│   │   └── SwapAnimator.cs    ← NEW
│   └── Board/
│       ├── GridComponent.cs   ← MODIFY
│       ├── CellComponent.cs   ← MODIFY
│       └── TileComponent.cs   ← MODIFY
└── Common/
    ├── AndCondition.cs        ← если нет, создать
    └── SwipeDirection.cs      ← NEW (enum)
```

---

## 10. Порядок имплементации

### Step 1: Enums и Interfaces
1. `SwipeDirection.cs`
2. `InputState.cs` (можно в InputController)
3. `ISwappable.cs`
4. `ISelectable.cs`
5. `AndCondition.cs` (если нет)

### Step 2: Модификация существующих
1. `GridComponent` — добавить `TryWorldToGrid`, `GridToWorld`, `IsValidPosition`
2. `CellComponent` — добавить `CurrentTile` property
3. `TileComponent` — реализовать `ISwappable`

### Step 3: Input
1. `InputController.cs` — полная реализация
2. Тест: логирование нажатий и свайпов

### Step 4: Visual
1. `SelectionVisualComponent.cs`
2. Создать префаб с SpriteRenderer для подсветки

### Step 5: Swap Logic
1. `SwapValidator.cs`
2. `SwapAnimator.cs`
3. `SwapController.cs`
4. Тест: свап без проверки матча

### Step 6: Integration
1. `BoardInputHandler.cs`
2. Настройка на сцене
3. Полный тест flow

### Step 7: Polish
1. Добавить звуки
2. Настроить тайминги анимаций
3. Edge cases (быстрые свайпы, граница поля)

---

## 11. Тестовый чеклист

```
□ Нажатие на тайл показывает подсветку
□ Свайп в любую сторону определяется корректно
□ Свайп за пределы поля игнорируется
□ Обмен соседних тайлов анимируется
□ Данные в Grid обновляются после свапа
□ Откат работает (временно все свапы откатываются)
□ Во время анимации ввод заблокирован
□ Touch и Mouse работают одинаково
□ Звуки воспроизводятся
□ Подсветка скрывается после свайпа
```

---

## 12. Связь с Phase 3 (Match & Destroy)

После реализации Phase 3 нужно:

1. **SwapValidator.WillCreateMatch()** — реализовать проверку через `MatchDetector`
2. **BoardInputHandler.OnSwapCompleted()** — вызывать `MatchDetector.FindMatches()`
3. Добавить событие `OnMatchFound` для запуска цепочки уничтожения

```csharp
// В BoardInputHandler после Phase 3:
private void OnSwapCompleted(Vector2Int posA, Vector2Int posB)
{
    var matches = _matchDetector.FindMatchesAt(posA, posB);
    if (matches.Count > 0)
    {
        OnMatchFound?.Invoke(matches);
    }
}
```

---

## 13. Возможные улучшения (не в scope)

- **Hint System** — подсказка возможного хода после N секунд бездействия
- **Undo** — отмена последнего хода
- **Drag Preview** — тайл следует за пальцем при перетаскивании
- **Multi-touch** — игнорирование второго касания
