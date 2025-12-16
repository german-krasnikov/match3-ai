# Этап 5: Input & Swap — Подробный План Реализации

## Обзор

Input & Swap — система обработки пользовательского ввода и обмена элементов местами. Отвечает за:
- Обработку mouse/touch ввода
- Определение направления свайпа
- Валидацию и выполнение обмена элементов
- Анимацию перемещения через DOTween

**Принцип Unity Way:** Три компонента с чёткими зонами ответственности:
- `InputComponent` — только ввод и события
- `SwapComponent` — только логика обмена
- `SwapAnimationComponent` — только анимация

---

## Архитектура

```
InputComponent (MonoBehaviour)      — обработка ввода, определение свайпа
SwapComponent (MonoBehaviour)       — логика обмена, валидация
SwapAnimationComponent (MonoBehaviour) — DOTween анимация движения
```

**Связи между компонентами:**
```
InputComponent
      │
      ▼ OnSwapRequested(Cell from, Cell to)
SwapComponent
      │
      ├── Валидация (соседи? не пустые?)
      │
      ▼ OnSwapStarted(Cell a, Cell b)
SwapAnimationComponent
      │
      ▼ Анимация завершена (callback)
SwapComponent
      │
      ▼ OnSwapCompleted(Cell a, Cell b, bool success)
```

**Поток данных при свайпе:**
```
[Игрок делает свайп]
         ↓
[InputComponent: MouseDown → MouseUp]
         ↓
[Определяет направление: Up/Down/Left/Right]
         ↓
[Находит from и to ячейки]
         ↓
[Event: OnSwapRequested]
         ↓
[SwapComponent: Валидация]
         ↓ (если валидно)
[Обмен данных в Cell и Element]
         ↓
[SwapAnimationComponent: DOTween анимация]
         ↓
[Callback → SwapComponent.OnAnimationComplete]
         ↓
[Event: OnSwapCompleted]
```

---

## 5.1 SwapConfig (ScriptableObject)

### Назначение
Настройки для свапа: скорость анимации, минимальная дистанция свайпа.

### Путь файла
`Assets/Scripts/Swap/SwapConfig.cs`

### Код

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SwapConfig", menuName = "Match3/SwapConfig")]
public class SwapConfig : ScriptableObject
{
    [Header("Input")]
    [SerializeField, Range(0.1f, 1f)] private float _minSwipeDistance = 0.3f;

    [Header("Animation")]
    [SerializeField, Range(0.1f, 0.5f)] private float _swapDuration = 0.2f;
    [SerializeField] private DG.Tweening.Ease _swapEase = DG.Tweening.Ease.OutQuad;

    public float MinSwipeDistance => _minSwipeDistance;
    public float SwapDuration => _swapDuration;
    public DG.Tweening.Ease SwapEase => _swapEase;
}
```

### Примечания
- `MinSwipeDistance` в world units — насколько далеко нужно провести пальцем
- `SwapDuration` — длительность анимации обмена
- `SwapEase` — тип easing для плавности

---

## 5.2 InputComponent (MonoBehaviour)

### Назначение
Обработка ввода игрока. Определяет какую ячейку выбрали и в каком направлении свайпнули. Стреляет событием `OnSwapRequested`.

### Путь файла
`Assets/Scripts/Input/InputComponent.cs`

### Публичный интерфейс

```csharp
public event Action<Cell, Cell> OnSwapRequested;
public bool IsEnabled { get; set; }
```

### Логика определения свайпа

```
1. MouseDown → запоминаем startPosition (world)
2. Конвертируем в grid координаты → startCell
3. MouseUp → получаем endPosition (world)
4. Вычисляем delta = endPosition - startPosition
5. Если |delta| < minSwipeDistance → игнорируем (клик, не свайп)
6. Определяем направление по наибольшей компоненте:
   - |delta.x| > |delta.y| → горизонталь (Left/Right)
   - иначе → вертикаль (Up/Down)
7. targetCell = startCell + direction
8. Fire OnSwapRequested(startCell, targetCell)
```

**Визуализация:**
```
     [UP]
       ↑
[LEFT] ← ● → [RIGHT]
       ↓
    [DOWN]

Зоны определяются углом вектора delta.
Упрощённо: сравниваем |x| vs |y| и знак.
```

### Код

```csharp
using System;
using UnityEngine;

public class InputComponent : MonoBehaviour
{
    public event Action<Cell, Cell> OnSwapRequested;

    [SerializeField] private GridComponent _grid;
    [SerializeField] private SwapConfig _config;
    [SerializeField] private Camera _camera;

    private Vector3 _startWorldPos;
    private Cell _startCell;
    private bool _isDragging;

    public bool IsEnabled { get; set; } = true;

    private void Update()
    {
        if (!IsEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown();
        }
        else if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            OnPointerUp();
        }
    }

    private void OnPointerDown()
    {
        _startWorldPos = GetWorldMousePosition();
        Vector2Int gridPos = _grid.WorldToGrid(_startWorldPos);

        _startCell = _grid.GetCell(gridPos);

        // Проверяем что кликнули внутри сетки и на непустую ячейку
        if (_startCell == null || _startCell.IsEmpty)
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
    }

    private void OnPointerUp()
    {
        _isDragging = false;

        Vector3 endWorldPos = GetWorldMousePosition();
        Vector3 delta = endWorldPos - _startWorldPos;

        // Слишком короткий свайп — игнорируем
        if (delta.magnitude < _config.MinSwipeDistance) return;

        // Определяем направление
        Vector2Int direction = GetSwipeDirection(delta);

        // Получаем целевую ячейку
        Cell targetCell = _grid.GetNeighbor(_startCell, direction);

        if (targetCell == null) return;

        OnSwapRequested?.Invoke(_startCell, targetCell);
    }

    private Vector2Int GetSwipeDirection(Vector3 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            // Горизонтальный свайп
            return delta.x > 0 ? GridDirections.Right : GridDirections.Left;
        }
        else
        {
            // Вертикальный свайп
            return delta.y > 0 ? GridDirections.Up : GridDirections.Down;
        }
    }

    private Vector3 GetWorldMousePosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -_camera.transform.position.z;
        return _camera.ScreenToWorldPoint(mousePos);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_isDragging || _startCell == null) return;

        // Рисуем стартовую позицию
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_startWorldPos, 0.2f);
    }
#endif
}
```

### Примечания
- `_camera` нужно передать через Inspector (или найти через `Camera.main` в Awake)
- Используем `GridDirections` для констант направлений (уже есть в проекте)
- `IsEnabled` — флаг для блокировки ввода во время анимаций
- Проверяем `_startCell.IsEmpty` чтобы нельзя было свайпнуть пустоту

---

## 5.3 SwapComponent (MonoBehaviour)

### Назначение
Логика обмена элементов. Валидация, обновление данных в Cell/Element, координация с анимацией.

### Путь файла
`Assets/Scripts/Swap/SwapComponent.cs`

### Публичный интерфейс

```csharp
public event Action<Cell, Cell> OnSwapStarted;
public event Action<Cell, Cell, bool> OnSwapCompleted; // bool = was valid swap

public void RequestSwap(Cell from, Cell to);
public void SwapBack(Cell a, Cell b); // Откат при отсутствии матча
```

### Валидация

```
1. Обе ячейки существуют (не null)
2. Обе ячейки не пустые
3. Ячейки соседние (|dx| + |dy| == 1)
```

### Логика обмена данных

```csharp
// Обмен ссылок в ячейках
var tempElement = cellA.Element;
cellA.Element = cellB.Element;
cellB.Element = tempElement;

// Обновление grid position в элементах
cellA.Element.SetGridPosition(cellA.X, cellA.Y);
cellB.Element.SetGridPosition(cellB.X, cellB.Y);
```

### Код

```csharp
using System;
using UnityEngine;

public class SwapComponent : MonoBehaviour
{
    public event Action<Cell, Cell> OnSwapStarted;
    public event Action<Cell, Cell, bool> OnSwapCompleted;

    [SerializeField] private SwapAnimationComponent _animation;

    private Cell _pendingCellA;
    private Cell _pendingCellB;
    private bool _isSwapping;

    public bool IsSwapping => _isSwapping;

    public void RequestSwap(Cell from, Cell to)
    {
        if (_isSwapping) return;

        if (!IsValidSwap(from, to))
        {
            OnSwapCompleted?.Invoke(from, to, false);
            return;
        }

        _isSwapping = true;
        _pendingCellA = from;
        _pendingCellB = to;

        OnSwapStarted?.Invoke(from, to);

        // Выполняем обмен данных
        SwapCellData(from, to);

        // Запускаем анимацию
        _animation.AnimateSwap(
            from.Element,
            to.Element,
            OnSwapAnimationComplete
        );
    }

    public void SwapBack(Cell a, Cell b)
    {
        if (_isSwapping) return;

        _isSwapping = true;
        _pendingCellA = a;
        _pendingCellB = b;

        // Обмен данных обратно
        SwapCellData(a, b);

        // Анимация возврата
        _animation.AnimateSwap(
            a.Element,
            b.Element,
            OnSwapBackAnimationComplete
        );
    }

    private bool IsValidSwap(Cell a, Cell b)
    {
        // Обе ячейки должны существовать
        if (a == null || b == null) return false;

        // Обе ячейки не должны быть пустыми
        if (a.IsEmpty || b.IsEmpty) return false;

        // Ячейки должны быть соседними
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);

        // Manhattan distance должна быть 1
        if (dx + dy != 1) return false;

        return true;
    }

    private void SwapCellData(Cell a, Cell b)
    {
        // Обмен элементов между ячейками
        var elementA = a.Element;
        var elementB = b.Element;

        a.Element = elementB;
        b.Element = elementA;

        // Обновляем grid position в элементах
        if (a.Element != null) a.Element.SetGridPosition(a.X, a.Y);
        if (b.Element != null) b.Element.SetGridPosition(b.X, b.Y);
    }

    private void OnSwapAnimationComplete()
    {
        _isSwapping = false;
        OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
    }

    private void OnSwapBackAnimationComplete()
    {
        _isSwapping = false;
        // SwapBack не генерирует OnSwapCompleted — это внутренняя операция
    }
}
```

### Примечания
- `_isSwapping` блокирует повторные свапы во время анимации
- `SwapBack` отдельный метод — используется GameLoop когда нет матча
- Данные обновляются ДО анимации — визуал догоняет логику
- Событие `OnSwapCompleted` с флагом `success` — для GameLoop

---

## 5.4 SwapAnimationComponent (MonoBehaviour)

### Назначение
Анимация перемещения элементов через DOTween. Знает только о визуале, ничего о логике.

### Путь файла
`Assets/Scripts/Swap/SwapAnimationComponent.cs`

### Публичный интерфейс

```csharp
public void AnimateSwap(ElementComponent a, ElementComponent b, Action onComplete);
```

### Код

```csharp
using System;
using UnityEngine;
using DG.Tweening;

public class SwapAnimationComponent : MonoBehaviour
{
    [SerializeField] private SwapConfig _config;
    [SerializeField] private GridComponent _grid;

    private Sequence _currentSequence;

    public void AnimateSwap(ElementComponent elementA, ElementComponent elementB, Action onComplete)
    {
        // Убиваем предыдущую анимацию если есть
        _currentSequence?.Kill();

        // Целевые позиции (элементы уже обменялись данными, так что берём их новые grid position)
        Vector3 targetPosA = _grid.GridToWorld(elementA.X, elementA.Y);
        Vector3 targetPosB = _grid.GridToWorld(elementB.X, elementB.Y);

        // Создаём sequence для синхронного выполнения
        _currentSequence = DOTween.Sequence();

        _currentSequence.Join(
            elementA.transform.DOMove(targetPosA, _config.SwapDuration)
                .SetEase(_config.SwapEase)
        );

        _currentSequence.Join(
            elementB.transform.DOMove(targetPosB, _config.SwapDuration)
                .SetEase(_config.SwapEase)
        );

        _currentSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
```

### Примечания
- `DOTween.Sequence` с `Join` — оба элемента двигаются одновременно
- Целевые позиции берём из `_grid.GridToWorld` по новым координатам элементов
- `Kill()` предыдущей анимации — защита от багов при быстрых свайпах
- `OnDestroy` — cleanup для DOTween

---

## 5.5 Интеграция компонентов

### Связывание Input → Swap

В GameLoop или отдельном контроллере:

```csharp
public class SwapController : MonoBehaviour
{
    [SerializeField] private InputComponent _input;
    [SerializeField] private SwapComponent _swap;

    private void OnEnable()
    {
        _input.OnSwapRequested += HandleSwapRequested;
        _swap.OnSwapStarted += HandleSwapStarted;
        _swap.OnSwapCompleted += HandleSwapCompleted;
    }

    private void OnDisable()
    {
        _input.OnSwapRequested -= HandleSwapRequested;
        _swap.OnSwapStarted -= HandleSwapStarted;
        _swap.OnSwapCompleted -= HandleSwapCompleted;
    }

    private void HandleSwapRequested(Cell from, Cell to)
    {
        _swap.RequestSwap(from, to);
    }

    private void HandleSwapStarted(Cell a, Cell b)
    {
        _input.IsEnabled = false; // Блокируем ввод
    }

    private void HandleSwapCompleted(Cell a, Cell b, bool success)
    {
        if (success)
        {
            // Здесь будет проверка на матч (этап 6+)
            Debug.Log($"Swap completed: {a} <-> {b}");
        }

        _input.IsEnabled = true; // Разблокируем ввод
    }
}
```

### Альтернатива: Прямая связь без контроллера

Можно связать Input и Swap напрямую в SwapComponent:

```csharp
// В SwapComponent добавить:
[SerializeField] private InputComponent _input;

private void OnEnable()
{
    _input.OnSwapRequested += RequestSwap;
}

private void OnDisable()
{
    _input.OnSwapRequested -= RequestSwap;
}
```

Я рекомендую **отдельный контроллер** — проще добавлять логику GameLoop.

---

## Настройка сцены

### Шаг 1: Создать файлы

```
Assets/Scripts/
├── Input/
│   └── InputComponent.cs
└── Swap/
    ├── SwapConfig.cs
    ├── SwapComponent.cs
    └── SwapAnimationComponent.cs
```

### Шаг 2: Создать SwapConfig

```
1. Project window → Create → Match3 → SwapConfig
2. Настроить:
   - Min Swipe Distance: 0.3
   - Swap Duration: 0.2
   - Swap Ease: OutQuad
3. Сохранить в Assets/Configs/SwapConfig.asset
```

### Шаг 3: Добавить компоненты на сцену

```
Board (GameObject)
├── GridComponent          (уже есть)
├── SpawnComponent         (уже есть)
├── BoardInitializer       (уже есть)
├── MatchDetectorComponent (уже есть)
├── InputComponent         [ДОБАВИТЬ]
├── SwapComponent          [ДОБАВИТЬ]
└── SwapAnimationComponent [ДОБАВИТЬ]
```

### Шаг 4: Связать в Inspector

**InputComponent:**
- Grid: Board (GridComponent)
- Config: SwapConfig.asset
- Camera: Main Camera

**SwapComponent:**
- Animation: Board (SwapAnimationComponent)

**SwapAnimationComponent:**
- Config: SwapConfig.asset
- Grid: Board (GridComponent)

---

## Порядок выполнения (рекомендуемый)

```
1. [ ] Создать SwapConfig.cs
2. [ ] Создать SwapConfig.asset в Configs/
3. [ ] Создать SwapAnimationComponent.cs
4. [ ] Создать SwapComponent.cs
5. [ ] Создать InputComponent.cs
6. [ ] Добавить компоненты на Board
7. [ ] Связать зависимости в Inspector
8. [ ] Тест: свайп двигает элементы
```

---

## Тестирование

### Тест 1: Базовый свайп

```
1. Play Mode
2. Свайпнуть по любому элементу вправо
3. Ожидание: элемент меняется местами с соседом справа
4. Консоль: "Swap completed: Cell(x,y) <-> Cell(x+1,y)"
```

### Тест 2: Невалидный свайп (край поля)

```
1. Свайпнуть элемент в правом краю вправо
2. Ожидание: ничего не происходит (targetCell = null)
```

### Тест 3: Короткий свайп

```
1. Кликнуть и чуть-чуть сдвинуть мышь (меньше minSwipeDistance)
2. Ожидание: ничего не происходит
```

### Тест 4: Блокировка во время анимации

```
1. Свайпнуть элемент
2. Пока идёт анимация — попробовать свайпнуть другой
3. Ожидание: второй свайп игнорируется
```

### Debug через Context Menu

```csharp
// Добавить в SwapComponent для тестирования
#if UNITY_EDITOR
[SerializeField] private GridComponent _grid;

[ContextMenu("Test Swap (0,0) <-> (1,0)")]
private void TestSwap()
{
    var a = _grid.GetCell(0, 0);
    var b = _grid.GetCell(1, 0);
    RequestSwap(a, b);
}
#endif
```

---

## Диаграмма состояний (для GameLoop — этап 8)

```
                    ┌─────────────┐
                    │    IDLE     │
                    └──────┬──────┘
                           │ OnSwapRequested
                           ▼
                    ┌─────────────┐
                    │  SWAPPING   │
                    └──────┬──────┘
                           │ OnSwapCompleted
                           ▼
                    ┌─────────────┐
                    │  CHECKING   │ ← MatchDetector.FindMatches()
                    └──────┬──────┘
                           │
              ┌────────────┴────────────┐
              │ no match                │ match found
              ▼                         ▼
       ┌─────────────┐           ┌─────────────┐
       │ SWAP BACK   │           │  DESTROYING │ (этап 6)
       └──────┬──────┘           └─────────────┘
              │
              ▼
       ┌─────────────┐
       │    IDLE     │
       └─────────────┘
```

---

## Возможные проблемы и решения

### Проблема: Элементы не двигаются

**Причина:** Неправильная связь в Inspector
**Решение:** Проверить что SwapAnimationComponent получает GridComponent и SwapConfig

### Проблема: Свайп работает неточно

**Причина:** Неправильный z для камеры
**Решение:** В `GetWorldMousePosition()` проверить `mousePos.z = -_camera.transform.position.z`

### Проблема: Можно свайпнуть во время анимации

**Причина:** `IsEnabled` не блокируется
**Решение:** Убедиться что `HandleSwapStarted` устанавливает `_input.IsEnabled = false`

### Проблема: Элементы "прыгают" в конце анимации

**Причина:** Рассинхрон данных и визуала
**Решение:** Данные должны обновляться ДО анимации, а не после

### Проблема: DOTween ошибки

**Причина:** DOTween не инициализирован
**Решение:** `DOTween.Init()` в любом Awake или добавить DOTween Setup в сцену

---

## Оптимизации (на будущее)

### 1. Touch поддержка

```csharp
// В InputComponent.Update()
if (Input.touchCount > 0)
{
    var touch = Input.GetTouch(0);
    if (touch.phase == TouchPhase.Began) OnPointerDown();
    else if (touch.phase == TouchPhase.Ended) OnPointerUp();
}
```

### 2. Visual feedback при drag

Подсветка выбранного элемента и направления:

```csharp
// В InputComponent
private void Update()
{
    if (_isDragging)
    {
        Vector3 current = GetWorldMousePosition();
        Vector3 delta = current - _startWorldPos;
        Vector2Int dir = GetSwipeDirection(delta);
        // Подсветить ячейку в направлении dir
    }
}
```

### 3. Haptic feedback (мобильные)

```csharp
#if UNITY_IOS || UNITY_ANDROID
Handheld.Vibrate();
#endif
```

---

## Чеклист готовности

- [ ] SwapConfig.cs создан
- [ ] SwapConfig.asset создан и настроен
- [ ] InputComponent.cs создан
- [ ] SwapComponent.cs создан
- [ ] SwapAnimationComponent.cs создан
- [ ] Компоненты добавлены на Board
- [ ] Зависимости связаны в Inspector
- [ ] Тест: свайп меняет элементы местами
- [ ] Тест: короткий свайп игнорируется
- [ ] Тест: свайп за край игнорируется
- [ ] Тест: ввод блокируется во время анимации
- [ ] Код компилируется без ошибок

---

## Следующий этап

После завершения Input & Swap → **Этап 6: Destruction**
- DestroyComponent (уничтожение матчей)
- DestroyAnimationComponent (анимация исчезновения)
- Интеграция с MatchDetector
