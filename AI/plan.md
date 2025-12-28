# Match-3 Game - Декомпозиция базовых механик

> **READ-ONLY после утверждения.** Каждый модуль независим благодаря STUB-заглушкам.

## Конфигурация
- **Сетка:** 8x8 фиксированная
- **Элементы:** 5 типов (Red, Green, Blue, Yellow, Purple)
- **Визуалы:** Цветные квадраты (placeholder)
- **Pooling:** Нет (добавим позже)

---

## Порядок реализации

```
1. Core (Interfaces + Enums) → 2. Grid → 3. Elements → 4. Spawn →
5. Match Detection → 6. Swap + Input → 7. Destruction → 8. Gravity → 9. Game Loop
```

---

## 1. CORE - Интерфейсы и Enums

### Файлы
```
Assets/Scripts/Core/
├── ElementType.cs
├── GameState.cs
└── Interfaces/
    ├── IGrid.cs
    ├── IGridElement.cs
    ├── IElementFactory.cs
    ├── ISpawnSystem.cs
    ├── IGravitySystem.cs
    ├── ISwapSystem.cs
    ├── IMatchDetection.cs
    └── IDestructionSystem.cs
```

### 1.1 ElementType.cs
```csharp
public enum ElementType
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 3,
    Yellow = 4,
    Purple = 5
}
```

### 1.2 GameState.cs
```csharp
public enum GameState
{
    Initializing,
    WaitingForInput,
    Swapping,
    CheckingMatches,
    Destroying,
    Falling
}
```

### 1.3 IGrid.cs
```csharp
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
```

### 1.4 IGridElement.cs
```csharp
public interface IGridElement
{
    Vector2Int GridPosition { get; set; }
    ElementType Type { get; }
    GameObject GameObject { get; }
}
```

### Подзадачи
- [ ] Создать структуру папок
- [ ] Создать ElementType enum
- [ ] Создать GameState enum
- [ ] Создать все интерфейсы

---

## 2. GRID SYSTEM - Сетка

### Файлы
```
Assets/Scripts/Grid/
└── GridComponent.cs
```

### GridComponent.cs
```csharp
public class GridComponent : MonoBehaviour, IGrid
{
    [SerializeField] private int _width = 8;
    [SerializeField] private int _height = 8;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _origin;

    private IGridElement[,] _grid;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;

    public Vector3 GridToWorld(Vector2Int gridPos);
    public Vector2Int WorldToGrid(Vector3 worldPos);
    public bool IsValidPosition(Vector2Int pos);

    public IGridElement GetElementAt(Vector2Int pos);
    public void SetElementAt(Vector2Int pos, IGridElement element);
    public void ClearCell(Vector2Int pos);

    public event Action<Vector2Int, IGridElement> OnElementPlaced;
    public event Action<Vector2Int> OnCellCleared;
}
```

### STUB для тестирования
```csharp
// STUB: IGridElement - заглушка до Element System
public class StubGridElement : IGridElement
{
    public Vector2Int GridPosition { get; set; }
    public ElementType Type => ElementType.Red;
    public GameObject GameObject => null;
}
```

### Подзадачи
- [ ] Реализовать GridComponent с 2D массивом
- [ ] GridToWorld: `_origin + new Vector3(gridPos.x * _cellSize, gridPos.y * _cellSize, 0)`
- [ ] WorldToGrid: обратная конвертация с Mathf.FloorToInt
- [ ] IsValidPosition: проверка границ
- [ ] Gizmos для debug-отрисовки сетки
- [ ] Тест: создать сетку, проверить конвертацию

---

## 3. ELEMENT SYSTEM - Элементы

### Файлы
```
Assets/Scripts/Elements/
├── ElementComponent.cs
└── ElementFactoryComponent.cs

Assets/ScriptableObjects/
└── ElementColorConfig.asset
```

### ElementComponent.cs
```csharp
public class ElementComponent : MonoBehaviour, IGridElement
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private ElementType _type;
    private Vector2Int _gridPosition;

    public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
    public ElementType Type => _type;
    public GameObject GameObject => gameObject;

    public void Initialize(ElementType type, Vector2Int gridPos);
    public void UpdateVisual(); // применить цвет из config
}
```

### ElementColorConfig.cs (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "ElementColors", menuName = "Match3/ElementColorConfig")]
public class ElementColorConfig : ScriptableObject
{
    [System.Serializable]
    public struct ElementColor
    {
        public ElementType type;
        public Color color;
    }

    [SerializeField] private ElementColor[] _colors;
    public Color GetColor(ElementType type);
}
```

### ElementFactoryComponent.cs
```csharp
public class ElementFactoryComponent : MonoBehaviour, IElementFactory
{
    [SerializeField] private ElementComponent _elementPrefab;
    [SerializeField] private ElementColorConfig _colorConfig;

    public ElementComponent Create(ElementType type, Vector3 worldPosition);
    public void Destroy(ElementComponent element);
}
```

### STUB для тестирования
```csharp
// STUB: Grid - позиция без реальной сетки
public Vector3 StubGetWorldPosition(Vector2Int gridPos)
    => new Vector3(gridPos.x, gridPos.y, 0);
```

### Подзадачи
- [ ] Создать ElementColorConfig ScriptableObject
- [ ] Настроить 5 цветов в Inspector
- [ ] Создать prefab: Sprite (квадрат) + ElementComponent
- [ ] Реализовать ElementComponent.Initialize()
- [ ] Реализовать ElementFactoryComponent
- [ ] Тест: создать по элементу каждого типа

---

## 4. SPAWN SYSTEM - Спаун

### Файлы
```
Assets/Scripts/Spawn/
└── SpawnComponent.cs
```

### SpawnComponent.cs
```csharp
public class SpawnComponent : MonoBehaviour, ISpawnSystem
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactoryComponent _factory;

    public void FillGrid();
    public ElementComponent SpawnAt(Vector2Int pos);
    public ElementComponent SpawnAtTop(int column);

    private ElementType GetRandomTypeWithoutMatch(Vector2Int pos);

    public event Action OnGridFilled;
}
```

### Алгоритм без начальных матчей
```csharp
private ElementType GetRandomTypeWithoutMatch(Vector2Int pos)
{
    var available = new List<ElementType> { Red, Green, Blue, Yellow, Purple };

    // Проверка 2 слева
    if (pos.x >= 2)
    {
        var left1 = _grid.GetElementAt(pos + Vector2Int.left);
        var left2 = _grid.GetElementAt(pos + Vector2Int.left * 2);
        if (left1?.Type == left2?.Type && left1 != null)
            available.Remove(left1.Type);
    }

    // Проверка 2 снизу
    if (pos.y >= 2)
    {
        var down1 = _grid.GetElementAt(pos + Vector2Int.down);
        var down2 = _grid.GetElementAt(pos + Vector2Int.down * 2);
        if (down1?.Type == down2?.Type && down1 != null)
            available.Remove(down1.Type);
    }

    return available[Random.Range(0, available.Count)];
}
```

### STUB для тестирования
```csharp
// STUB: Grid
private IGridElement StubGetElementAt(Vector2Int pos) => null;
private void StubSetElementAt(Vector2Int pos, IGridElement el) { }

// STUB: Factory
private ElementComponent StubCreate(ElementType type, Vector3 pos) => null;
```

### Подзадачи
- [ ] Реализовать SpawnComponent
- [ ] FillGrid(): заполнение снизу вверх, слева направо
- [ ] GetRandomTypeWithoutMatch(): исключение матчей
- [ ] SpawnAtTop(): для гравитации (спавн над сеткой)
- [ ] Тест: заполнить сетку, визуально проверить отсутствие матчей

---

## 5. MATCH DETECTION - Поиск матчей

### Файлы
```
Assets/Scripts/Match/
└── MatchDetectionComponent.cs
```

### MatchDetectionComponent.cs
```csharp
public class MatchDetectionComponent : MonoBehaviour, IMatchDetection
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private int _minMatchLength = 3;

    public List<Vector2Int> FindAllMatches();
    public List<Vector2Int> FindMatchesAt(Vector2Int pos);
    public bool HasAnyMatch();
    public bool WouldCreateMatch(Vector2Int pos, ElementType type);

    private List<Vector2Int> GetMatchLine(Vector2Int start, Vector2Int direction);

    public event Action<List<Vector2Int>> OnMatchesFound;
}
```

### Алгоритм поиска
```csharp
public List<Vector2Int> FindAllMatches()
{
    var matches = new HashSet<Vector2Int>();

    // Горизонтальные
    for (int y = 0; y < _grid.Height; y++)
        for (int x = 0; x < _grid.Width - 2; x++)
        {
            var line = GetMatchLine(new Vector2Int(x, y), Vector2Int.right);
            if (line.Count >= 3)
                foreach (var pos in line) matches.Add(pos);
        }

    // Вертикальные
    for (int x = 0; x < _grid.Width; x++)
        for (int y = 0; y < _grid.Height - 2; y++)
        {
            var line = GetMatchLine(new Vector2Int(x, y), Vector2Int.up);
            if (line.Count >= 3)
                foreach (var pos in line) matches.Add(pos);
        }

    return matches.ToList();
}

private List<Vector2Int> GetMatchLine(Vector2Int start, Vector2Int dir)
{
    var result = new List<Vector2Int> { start };
    var startEl = _grid.GetElementAt(start);
    if (startEl == null) return result;

    var current = start + dir;
    while (_grid.IsValidPosition(current))
    {
        var el = _grid.GetElementAt(current);
        if (el?.Type != startEl.Type) break;
        result.Add(current);
        current += dir;
    }
    return result;
}
```

### STUB для тестирования
```csharp
// STUB: Grid с предсказуемым паттерном
private IGridElement StubGetElementAt(Vector2Int pos)
    => new StubGridElement { Type = (ElementType)((pos.x + pos.y) % 5 + 1) };
```

### Подзадачи
- [ ] Реализовать MatchDetectionComponent
- [ ] GetMatchLine(): поиск линии в направлении
- [ ] FindAllMatches(): HashSet для уникальности
- [ ] WouldCreateMatch(): превентивная проверка для спауна
- [ ] Тест: вручную создать матчи, проверить детекцию

---

## 6. SWAP SYSTEM + INPUT - Обмен элементов

### Файлы
```
Assets/Scripts/Swap/
├── SwapComponent.cs
└── InputComponent.cs
```

### InputComponent.cs (Drag-based)
```csharp
public class InputComponent : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private Camera _camera;
    [SerializeField] private float _minDragDistance = 0.3f;

    public event Action<Vector2Int, Vector2Int> OnSwapRequested;
    public event Action<Vector2Int> OnDragStarted;
    public event Action OnDragCanceled;

    private Vector2Int? _dragStartCell;
    private Vector3 _dragStartWorldPos;
    private bool _isDragging;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleDragStart();
        else if (Input.GetMouseButtonUp(0) && _isDragging)
            HandleDragEnd();
    }

    private void HandleDragEnd()
    {
        Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 delta = worldPos - _dragStartWorldPos;

        if (delta.magnitude >= _minDragDistance)
        {
            Vector2Int dir = GetSwipeDirection(delta);
            Vector2Int target = _dragStartCell.Value + dir;
            if (_grid.IsValidPosition(target))
                OnSwapRequested?.Invoke(_dragStartCell.Value, target);
        }
        CancelDrag();
    }

    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        return Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? (delta.x > 0 ? Vector2Int.right : Vector2Int.left)
            : (delta.y > 0 ? Vector2Int.up : Vector2Int.down);
    }
}
```

### SwapComponent.cs
```csharp
public class SwapComponent : MonoBehaviour, ISwapSystem
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private float _swapDuration = 0.2f;

    public bool CanSwap(Vector2Int pos1, Vector2Int pos2);
    public async Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2);
    public async Task SwapBack(Vector2Int pos1, Vector2Int pos2);

    private bool AreNeighbors(Vector2Int pos1, Vector2Int pos2);
    private async Task AnimateSwap(ElementComponent el1, ElementComponent el2);

    public event Action<Vector2Int, Vector2Int> OnSwapStarted;
    public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
}

private bool AreNeighbors(Vector2Int p1, Vector2Int p2)
{
    int dx = Mathf.Abs(p1.x - p2.x);
    int dy = Mathf.Abs(p1.y - p2.y);
    return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
}
```

### DOTween анимация
```csharp
private async Task AnimateSwap(ElementComponent el1, ElementComponent el2)
{
    var pos1 = el1.transform.position;
    var pos2 = el2.transform.position;

    var t1 = el1.transform.DOMove(pos2, _swapDuration);
    var t2 = el2.transform.DOMove(pos1, _swapDuration);

    await Task.WhenAll(t1.AsyncWaitForCompletion(), t2.AsyncWaitForCompletion());
}
```

### STUB для тестирования
```csharp
// STUB: Grid
private Vector2Int StubWorldToGrid(Vector3 w) => new Vector2Int((int)w.x, (int)w.y);
```

### Подзадачи
- [x] Реализовать InputComponent (drag/swipe)
- [x] Реализовать AreNeighbors() валидацию
- [x] Реализовать AnimateSwap() с DOTween
- [x] Реализовать SwapBack() для неудачных свапов
- [ ] Тест: drag на соседа, проверить анимацию

---

## 7. DESTRUCTION SYSTEM - Уничтожение

### Файлы
```
Assets/Scripts/Destruction/
└── DestructionComponent.cs
```

### DestructionComponent.cs
```csharp
public class DestructionComponent : MonoBehaviour, IDestructionSystem
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private float _destroyDuration = 0.2f;

    public async Task DestroyElements(List<Vector2Int> positions);
    public async Task DestroyElement(Vector2Int pos);

    private async Task AnimateDestruction(ElementComponent element);

    public event Action<List<Vector2Int>> OnDestructionStarted;
    public event Action<List<Vector2Int>> OnDestructionCompleted;
}
```

### DOTween анимация
```csharp
private async Task AnimateDestruction(ElementComponent element)
{
    var sequence = DOTween.Sequence();
    sequence.Join(element.transform.DOScale(0f, _destroyDuration).SetEase(Ease.InBack));
    sequence.Join(element.GetComponent<SpriteRenderer>().DOFade(0f, _destroyDuration));

    await sequence.AsyncWaitForCompletion();

    Destroy(element.gameObject);
}
```

### Подзадачи
- [ ] Реализовать DestructionComponent
- [ ] AnimateDestruction(): scale + fade с DOTween
- [ ] DestroyElements(): параллельное удаление
- [ ] Очищать ячейки в Grid после уничтожения
- [ ] Тест: пометить элементы, проверить анимацию

---

## 8. GRAVITY SYSTEM - Падение

### Файлы
```
Assets/Scripts/Gravity/
└── GravityComponent.cs
```

### GravityComponent.cs
```csharp
public class GravityComponent : MonoBehaviour, IGravitySystem
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawn;
    [SerializeField] private float _fallDuration = 0.3f;

    public async Task ApplyGravity();
    public bool HasEmptyCells();

    private async Task FallColumn(int column);
    private async Task AnimateFall(ElementComponent el, Vector2Int from, Vector2Int to);

    public event Action OnGravityStarted;
    public event Action OnGravityCompleted;
}
```

### Алгоритм гравитации
```csharp
private async Task FallColumn(int column)
{
    for (int y = 0; y < _grid.Height; y++)
    {
        var pos = new Vector2Int(column, y);
        if (_grid.GetElementAt(pos) != null) continue;

        // Найти элемент выше
        for (int above = y + 1; above < _grid.Height; above++)
        {
            var abovePos = new Vector2Int(column, above);
            var element = _grid.GetElementAt(abovePos);
            if (element != null)
            {
                _grid.ClearCell(abovePos);
                _grid.SetElementAt(pos, element);
                await AnimateFall((ElementComponent)element, abovePos, pos);
                break;
            }
        }

        // Если выше ничего - спавним
        if (_grid.GetElementAt(pos) == null)
        {
            var newEl = _spawn.SpawnAtTop(column);
            await AnimateFall(newEl, new Vector2Int(column, _grid.Height), pos);
        }
    }
}
```

### Подзадачи
- [ ] Реализовать GravityComponent
- [ ] FallColumn(): обработка одной колонки
- [ ] AnimateFall() с DOTween
- [ ] ApplyGravity(): параллельно все колонки
- [ ] Связать со SpawnComponent для новых элементов
- [ ] Тест: удалить элемент, проверить падение

---

## 9. GAME LOOP - Координация

### Файлы
```
Assets/Scripts/GameLoop/
└── GameLoopComponent.cs
```

### GameLoopComponent.cs
```csharp
public class GameLoopComponent : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawn;
    [SerializeField] private SwapComponent _swap;
    [SerializeField] private MatchDetectionComponent _matchDetection;
    [SerializeField] private DestructionComponent _destruction;
    [SerializeField] private GravityComponent _gravity;
    [SerializeField] private InputComponent _input;

    private GameState _currentState;

    public event Action<GameState> OnStateChanged;
}
```

### State Machine
```csharp
private async Task ProcessSwap(Vector2Int pos1, Vector2Int pos2)
{
    SetState(GameState.Swapping);
    await _swap.TrySwap(pos1, pos2);

    SetState(GameState.CheckingMatches);
    var matches = _matchDetection.FindAllMatches();

    if (matches.Count == 0)
    {
        SetState(GameState.Swapping);
        await _swap.SwapBack(pos1, pos2);
        SetState(GameState.WaitingForInput);
        return;
    }

    // Каскадный цикл
    while (matches.Count > 0)
    {
        SetState(GameState.Destroying);
        await _destruction.DestroyElements(matches);

        SetState(GameState.Falling);
        await _gravity.ApplyGravity();

        SetState(GameState.CheckingMatches);
        matches = _matchDetection.FindAllMatches();
    }

    SetState(GameState.WaitingForInput);
}
```

### Подзадачи
- [ ] Реализовать GameLoopComponent
- [ ] Initialize(): FillGrid + подписка на Input
- [ ] ProcessSwap(): полный цикл
- [ ] Защита от input во время анимаций
- [ ] Тест: полный цикл свап → матч → удаление → гравитация → каскад

---

## Структура проекта

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── ElementType.cs
│   │   ├── GameState.cs
│   │   └── Interfaces/
│   │       ├── IGrid.cs
│   │       ├── IGridElement.cs
│   │       ├── IElementFactory.cs
│   │       ├── ISpawnSystem.cs
│   │       ├── IGravitySystem.cs
│   │       ├── ISwapSystem.cs
│   │       ├── IMatchDetection.cs
│   │       └── IDestructionSystem.cs
│   ├── Grid/
│   │   └── GridComponent.cs
│   ├── Elements/
│   │   ├── ElementComponent.cs
│   │   └── ElementFactoryComponent.cs
│   ├── Spawn/
│   │   └── SpawnComponent.cs
│   ├── Gravity/
│   │   └── GravityComponent.cs
│   ├── Swap/
│   │   ├── SwapComponent.cs
│   │   └── InputComponent.cs
│   ├── Match/
│   │   └── MatchDetectionComponent.cs
│   ├── Destruction/
│   │   └── DestructionComponent.cs
│   └── GameLoop/
│       └── GameLoopComponent.cs
├── ScriptableObjects/
│   └── ElementColorConfig.asset
├── Prefabs/
│   └── Element.prefab
└── Scenes/
    └── SampleScene.unity
```

---

## Критические файлы

| Файл | Назначение |
|------|------------|
| `Scripts/Core/Interfaces/IGrid.cs` | Центральный интерфейс, от которого зависят все модули |
| `Scripts/Grid/GridComponent.cs` | Хранилище данных, фундамент игры |
| `Scripts/Elements/ElementComponent.cs` | Визуальное представление с IGridElement |
| `Scripts/Match/MatchDetectionComponent.cs` | Ключевая логика Match-3 |
| `Scripts/GameLoop/GameLoopComponent.cs` | State Machine, координация |
