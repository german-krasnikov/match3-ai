# Phase 6: Input & Swap System — Реализация

## Статус: 📋 ПЛАН

## Обзор

Система обработки ввода и свапа элементов. Swipe-based управление (Candy Crush стиль).

```
Assets/Scripts/
├── Input/
│   ├── IInputHandler.cs           # Интерфейс ввода
│   ├── SwipeInputHandler.cs       # Touch + Mouse swipe detection
│   └── InputConfig.cs             # SO настройки ввода
└── Swap/
    ├── SwapValidator.cs           # Валидация свапа (Pure C#)
    ├── SwapAnimator.cs            # Анимация свапа (MonoBehaviour)
    └── SwapController.cs          # Оркестратор (MonoBehaviour)
```

**Решения:**
- Swipe-based ввод (tap + drag в направлении)
- Touch + Mouse поддержка (unified через Screen coordinates)
- Без визуальной индикации выбора
- Откат с "shake" анимацией при невалидном свапе

**Зависимости:** GridData, GridConfig, GridPositionConverter, MatchController, ElementView

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                   SwipeInputHandler                          │
│                    (MonoBehaviour)                           │
│  • Touch/Mouse detection                                     │
│  • Swipe direction calculation                               │
│  • События: OnSwipeDetected(GridPosition from, direction)   │
└─────────────────────┬───────────────────────────────────────┘
                      │ fires event
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    SwapController                            │
│                    (MonoBehaviour)                           │
│  • Оркестратор свапа                                        │
│  • Проверка валидности через SwapValidator                  │
│  • Запуск анимации через SwapAnimator                       │
│  • События: OnSwapComplete, OnSwapFailed                    │
└────────────┬────────────────────────┬───────────────────────┘
             │                        │
             ▼                        ▼
┌─────────────────────┐  ┌─────────────────────────────────────┐
│   SwapValidator     │  │         SwapAnimator                 │
│    (Pure C#)        │  │        (MonoBehaviour)               │
│  • AreNeighbors     │  │  • AnimateSwap (DOTween)            │
│  • WouldCreateMatch │  │  • AnimateInvalidSwap (shake)       │
└─────────────────────┘  └─────────────────────────────────────┘
```

**Unity Way принципы:**
- `SwipeInputHandler` — только обработка ввода
- `SwapValidator` — Pure C#, легко тестировать
- `SwapAnimator` — только анимации
- `SwapController` — оркестрация, события для GameLoop
- Event-driven коммуникация

---

## Реализуемые файлы

### 6.1 InputConfig.cs (ScriptableObject)

**Файл:** `Assets/Scripts/Input/InputConfig.cs`

Настройки чувствительности свайпа.

```csharp
using UnityEngine;

namespace Match3.Input
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Match3/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [Header("Swipe Detection")]
        [Tooltip("Минимальная дистанция свайпа в пикселях")]
        [SerializeField] private float _minSwipeDistance = 30f;

        [Tooltip("Максимальное время свайпа в секундах")]
        [SerializeField] private float _maxSwipeTime = 0.5f;

        public float MinSwipeDistance => _minSwipeDistance;
        public float MaxSwipeTime => _maxSwipeTime;
    }
}
```

**Параметры:**
| Поле | Default | Описание |
|------|---------|----------|
| `MinSwipeDistance` | 30px | Минимум для детекции свайпа |
| `MaxSwipeTime` | 0.5s | Максимум времени для свайпа |

---

### 6.2 IInputHandler.cs (Interface)

**Файл:** `Assets/Scripts/Input/IInputHandler.cs`

Контракт для систем ввода.

```csharp
using System;
using Match3.Core;

namespace Match3.Input
{
    public interface IInputHandler
    {
        /// <summary>
        /// Свайп детектирован: позиция элемента + направление.
        /// </summary>
        event Action<GridPosition, GridPosition> OnSwipeDetected;

        /// <summary>
        /// Включить/выключить обработку ввода.
        /// </summary>
        void SetEnabled(bool enabled);

        bool IsEnabled { get; }
    }
}
```

**Зачем интерфейс:**
- Можно заменить на AI input для тестов
- Можно добавить ReplayInputHandler для записи/воспроизведения
- DIP — SwapController зависит от абстракции

---

### 6.3 SwipeInputHandler.cs (MonoBehaviour)

**Файл:** `Assets/Scripts/Input/SwipeInputHandler.cs`

Обработка touch и mouse ввода. Unified подход через screen coordinates.

```csharp
using System;
using Match3.Core;
using Match3.Grid;
using UnityEngine;

namespace Match3.Input
{
    public class SwipeInputHandler : MonoBehaviour, IInputHandler
    {
        public event Action<GridPosition, GridPosition> OnSwipeDetected;

        [SerializeField] private InputConfig _config;
        [SerializeField] private GridView _gridView;
        [SerializeField] private Camera _camera;

        private bool _isEnabled = true;
        private bool _isSwiping;
        private Vector2 _swipeStart;
        private float _swipeStartTime;
        private GridPosition _startGridPos;

        public bool IsEnabled => _isEnabled;

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled) ResetSwipe();
        }

        private void Update()
        {
            if (!_isEnabled) return;

            #if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
            #else
            HandleTouchInput();
            #endif
        }

        private void HandleMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                TryStartSwipe(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isSwiping)
            {
                TryCompleteSwipe(UnityEngine.Input.mousePosition);
            }
        }

        private void HandleTouchInput()
        {
            if (UnityEngine.Input.touchCount == 0) return;

            var touch = UnityEngine.Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartSwipe(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isSwiping) TryCompleteSwipe(touch.position);
                    break;
            }
        }

        private void TryStartSwipe(Vector2 screenPos)
        {
            var worldPos = _camera.ScreenToWorldPoint(screenPos);
            var gridPos = _gridView.PositionConverter.WorldToGrid(worldPos);

            if (!IsValidGridPosition(gridPos)) return;

            _isSwiping = true;
            _swipeStart = screenPos;
            _swipeStartTime = Time.time;
            _startGridPos = gridPos;
        }

        private void TryCompleteSwipe(Vector2 screenPos)
        {
            var elapsed = Time.time - _swipeStartTime;
            var delta = screenPos - _swipeStart;
            var distance = delta.magnitude;

            ResetSwipe();

            // Проверка валидности свайпа
            if (elapsed > _config.MaxSwipeTime) return;
            if (distance < _config.MinSwipeDistance) return;

            var direction = GetSwipeDirection(delta);
            if (direction == GridPosition.Zero) return;

            OnSwipeDetected?.Invoke(_startGridPos, direction);
        }

        private GridPosition GetSwipeDirection(Vector2 delta)
        {
            // Определяем доминантную ось
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? GridPosition.Right : GridPosition.Left;
            }
            else
            {
                return delta.y > 0 ? GridPosition.Up : GridPosition.Down;
            }
        }

        private bool IsValidGridPosition(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < _gridView.Config.Width &&
                   pos.Y >= 0 && pos.Y < _gridView.Config.Height;
        }

        private void ResetSwipe()
        {
            _isSwiping = false;
        }
    }
}
```

### Алгоритм Swipe Detection

```
1. Touch/Click Begin:
   ├── Конвертировать screen → world → grid
   ├── Проверить что позиция на сетке
   └── Запомнить: start position, time, grid position

2. Touch/Click End:
   ├── Проверить elapsed time < MaxSwipeTime
   ├── Проверить distance > MinSwipeDistance
   ├── Определить направление по delta (доминантная ось)
   └── Fire OnSwipeDetected(startGridPos, direction)
```

**Визуализация:**

```
Screen Space:           Grid Space:

  Start ───────► End    (2,3) + Right = (3,3)
  │     delta
  │                    OnSwipeDetected(
  ▼                        from: (2,3),
  distance > 30px          direction: Right
                       )
```

---

### 6.4 SwapValidator.cs (Pure C#)

**Файл:** `Assets/Scripts/Swap/SwapValidator.cs`

Валидация свапа. Без MonoBehaviour — легко тестировать.

```csharp
using Match3.Core;
using Match3.Grid;
using Match3.Match;

namespace Match3.Swap
{
    public class SwapValidator
    {
        private readonly GridData _grid;
        private readonly IMatchFinder _matchFinder;

        public SwapValidator(GridData grid, IMatchFinder matchFinder)
        {
            _grid = grid;
            _matchFinder = matchFinder;
        }

        /// <summary>
        /// Проверяет что позиции — соседи (по горизонтали или вертикали).
        /// </summary>
        public bool AreNeighbors(GridPosition a, GridPosition b)
        {
            var delta = a - b;
            return (Mathf.Abs(delta.X) == 1 && delta.Y == 0) ||
                   (delta.X == 0 && Mathf.Abs(delta.Y) == 1);
        }

        /// <summary>
        /// Проверяет что свап создаст матч (без изменения состояния).
        /// </summary>
        public bool WouldCreateMatch(GridPosition a, GridPosition b)
        {
            // Временный свап
            _grid.SwapElements(a, b);

            // Проверка матчей в обеих позициях
            var matches = _matchFinder.FindMatchesAt(_grid, new[] { a, b });
            var hasMatch = matches.Count > 0;

            // Откат
            _grid.SwapElements(a, b);

            return hasMatch;
        }

        /// <summary>
        /// Полная валидация свапа.
        /// </summary>
        public bool IsValidSwap(GridPosition a, GridPosition b)
        {
            // Проверка границ
            if (!_grid.IsValidPosition(a) || !_grid.IsValidPosition(b))
                return false;

            // Проверка наличия элементов
            if (_grid.GetElement(a) == null || _grid.GetElement(b) == null)
                return false;

            // Проверка соседства
            if (!AreNeighbors(a, b))
                return false;

            // Проверка матча
            return WouldCreateMatch(a, b);
        }
    }
}
```

### Логика валидации

```
IsValidSwap(A, B):
│
├── IsValidPosition(A)? ─── No ──► return false
├── IsValidPosition(B)? ─── No ──► return false
├── GetElement(A) != null? ─ No ──► return false
├── GetElement(B) != null? ─ No ──► return false
├── AreNeighbors(A, B)? ─── No ──► return false
│
└── WouldCreateMatch(A, B)?
    ├── Swap A ↔ B (временно)
    ├── FindMatchesAt([A, B])
    ├── Swap B ↔ A (откат)
    └── return matches.Count > 0
```

---

### 6.5 SwapAnimator.cs (MonoBehaviour)

**Файл:** `Assets/Scripts/Swap/SwapAnimator.cs`

Анимации свапа через DOTween.

```csharp
using System;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Swap
{
    public class SwapAnimator : MonoBehaviour
    {
        [SerializeField] private GridConfig _config;
        [SerializeField] private GridView _gridView;

        /// <summary>
        /// Анимация валидного свапа (элементы меняются местами).
        /// </summary>
        public void AnimateSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            var posA = _gridView.PositionConverter.GridToWorld(elementA.Position);
            var posB = _gridView.PositionConverter.GridToWorld(elementB.Position);
            var duration = _config.SwapDuration;

            int completed = 0;
            void OnOneComplete()
            {
                completed++;
                if (completed == 2) onComplete?.Invoke();
            }

            elementA.MoveTo(posB, duration, OnOneComplete);
            elementB.MoveTo(posA, duration, OnOneComplete);
        }

        /// <summary>
        /// Анимация невалидного свапа (туда-обратно с shake).
        /// </summary>
        public void AnimateInvalidSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            var posA = _gridView.PositionConverter.GridToWorld(elementA.Position);
            var posB = _gridView.PositionConverter.GridToWorld(elementB.Position);
            var duration = _config.SwapDuration * 0.5f; // Быстрее для invalid

            var sequence = DOTween.Sequence();

            // Движение навстречу (половина пути)
            var midA = Vector3.Lerp(posA, posB, 0.3f);
            var midB = Vector3.Lerp(posB, posA, 0.3f);

            sequence.Append(elementA.Transform.DOMove(midA, duration).SetEase(Ease.OutQuad));
            sequence.Join(elementB.Transform.DOMove(midB, duration).SetEase(Ease.OutQuad));

            // Откат с небольшим shake
            sequence.Append(elementA.Transform.DOMove(posA, duration).SetEase(Ease.OutBack));
            sequence.Join(elementB.Transform.DOMove(posB, duration).SetEase(Ease.OutBack));

            sequence.OnComplete(() => onComplete?.Invoke());
        }
    }
}
```

### Анимации

**Валидный свап:**
```
A ────────────────► B position
B ────────────────► A position
Duration: SwapDuration (0.2s)
Ease: OutQuad
```

**Невалидный свап (shake):**
```
A ───► mid ──┐
             │ OutBack (bounce)
B ───► mid ──┘
             │
A ◄────────── original A
B ◄────────── original B
Duration: SwapDuration * 0.5 × 2
```

---

### 6.6 SwapController.cs (MonoBehaviour)

**Файл:** `Assets/Scripts/Swap/SwapController.cs`

Оркестратор свапа. Связывает Input → Validation → Animation → Grid Update.

```csharp
using System;
using Match3.Core;
using Match3.Elements;
using Match3.Grid;
using Match3.Input;
using Match3.Match;
using UnityEngine;

namespace Match3.Swap
{
    public class SwapController : MonoBehaviour
    {
        public event Action<GridPosition, GridPosition> OnSwapComplete;
        public event Action OnSwapFailed;

        [SerializeField] private SwipeInputHandler _inputHandler;
        [SerializeField] private SwapAnimator _animator;
        [SerializeField] private MatchController _matchController;
        [SerializeField] private GridView _gridView;

        private GridData _grid;
        private SwapValidator _validator;
        private bool _isSwapping;

        public bool IsSwapping => _isSwapping;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _validator = new SwapValidator(grid, new LineMatchFinder());

            _inputHandler.OnSwipeDetected += OnSwipeDetected;
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.OnSwipeDetected -= OnSwipeDetected;
        }

        private void OnSwipeDetected(GridPosition from, GridPosition direction)
        {
            if (_isSwapping) return;

            var to = from + direction;
            TrySwap(from, to);
        }

        public void TrySwap(GridPosition a, GridPosition b)
        {
            if (_isSwapping) return;

            var elementA = _grid.GetElement(a);
            var elementB = _grid.GetElement(b);

            if (elementA == null || elementB == null) return;

            _isSwapping = true;
            _inputHandler.SetEnabled(false);

            if (_validator.IsValidSwap(a, b))
            {
                ExecuteValidSwap(a, b, elementA, elementB);
            }
            else
            {
                ExecuteInvalidSwap(elementA, elementB);
            }
        }

        private void ExecuteValidSwap(GridPosition a, GridPosition b, IElement elementA, IElement elementB)
        {
            // Обновить данные
            _grid.SwapElements(a, b);
            elementA.Position = b;
            elementB.Position = a;

            // Анимировать
            _animator.AnimateSwap(elementA, elementB, () =>
            {
                _isSwapping = false;
                OnSwapComplete?.Invoke(a, b);
                // Input остаётся выключенным — GameLoop включит после обработки матчей
            });
        }

        private void ExecuteInvalidSwap(IElement elementA, IElement elementB)
        {
            _animator.AnimateInvalidSwap(elementA, elementB, () =>
            {
                _isSwapping = false;
                _inputHandler.SetEnabled(true);
                OnSwapFailed?.Invoke();
            });
        }

        /// <summary>
        /// Вызывается GameLoop после завершения каскада.
        /// </summary>
        public void EnableInput()
        {
            _inputHandler.SetEnabled(true);
        }
    }
}
```

### Поток выполнения

```
OnSwipeDetected(from, direction)
│
├── Проверка: _isSwapping? ─── Yes ──► return
│
├── to = from + direction
│
├── GetElement(from), GetElement(to)
│   └── null? ─── Yes ──► return
│
├── _isSwapping = true
├── _inputHandler.SetEnabled(false)
│
└── IsValidSwap(from, to)?
    │
    ├── Yes (Valid):
    │   ├── grid.SwapElements(a, b)
    │   ├── Update element.Position
    │   ├── AnimateSwap()
    │   └── OnComplete:
    │       ├── _isSwapping = false
    │       └── OnSwapComplete?.Invoke()
    │           // Input остаётся OFF для GameLoop
    │
    └── No (Invalid):
        ├── AnimateInvalidSwap()
        └── OnComplete:
            ├── _isSwapping = false
            ├── _inputHandler.SetEnabled(true)
            └── OnSwapFailed?.Invoke()
```

---

## Интеграция

### Использует из Phase 1-5:

| Компонент | Назначение |
|-----------|-----------|
| `GridPosition` | Координаты, направления, арифметика |
| `GridData` | GetElement, SwapElements, IsValidPosition |
| `GridConfig` | SwapDuration |
| `GridView` | PositionConverter, Config |
| `GridPositionConverter` | WorldToGrid, GridToWorld |
| `IElement` | Position, Transform, MoveTo |
| `MatchController` | (через MatchFinder) для валидации |
| `LineMatchFinder` | FindMatchesAt для WouldCreateMatch |

### Предоставляет для Phase 7+ (GameLoop):

| Событие/Метод | Использует |
|---------------|-----------|
| `OnSwapComplete` | GameStateMachine → переход в Matching state |
| `OnSwapFailed` | GameStateMachine → остаться в Idle |
| `EnableInput()` | GameStateMachine → после завершения каскада |
| `IsSwapping` | Блокировка других систем |

### Пример интеграции в GameLoop:

```csharp
public class GameStateMachine : MonoBehaviour
{
    [SerializeField] private SwapController _swapController;
    [SerializeField] private MatchController _matchController;

    private void OnEnable()
    {
        _swapController.OnSwapComplete += OnSwapComplete;
        _swapController.OnSwapFailed += OnSwapFailed;
    }

    private void OnSwapComplete(GridPosition a, GridPosition b)
    {
        SetState(GameState.Matching);
        _matchController.CheckAt(a, b);  // Проверить матчи
    }

    private void OnSwapFailed()
    {
        // Остаёмся в Idle, input уже включен
    }

    private void OnCascadeComplete()
    {
        _swapController.EnableInput();
        SetState(GameState.Idle);
    }
}
```

---

## Scene Setup

### Иерархия объектов:

```
Scene
├── Main Camera               [Camera] ← для raycast
├── Grid                      [GridView]
│   └── Cells
├── ElementPool               [ElementPool]
├── Elements                  (parent)
├── ElementFactory            [ElementFactory]
├── SpawnController           [SpawnController]
├── MatchController           [MatchController]
├── SwipeInputHandler         [SwipeInputHandler]     ← NEW
├── SwapAnimator              [SwapAnimator]          ← NEW
├── SwapController            [SwapController]        ← NEW
└── GameBootstrap             [GameBootstrap]
```

### Настройка компонентов:

**SwipeInputHandler:**
```
_config      → InputConfig.asset
_gridView    → Grid
_camera      → Main Camera
```

**SwapAnimator:**
```
_config      → GridConfig.asset
_gridView    → Grid
```

**SwapController:**
```
_inputHandler    → SwipeInputHandler
_animator        → SwapAnimator
_matchController → MatchController
_gridView        → Grid
```

### Обновление GameBootstrap:

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GridView _gridView;
    [SerializeField] private SpawnController _spawnController;
    [SerializeField] private MatchController _matchController;
    [SerializeField] private SwapController _swapController;  // NEW

    private GridData _gridData;

    private void Start()
    {
        var config = _gridView.Config;
        _gridData = new GridData(config.Width, config.Height);

        _gridView.CreateVisualGrid();

        _spawnController.Initialize(_gridData);
        _matchController.Initialize(_gridData);
        _swapController.Initialize(_gridData);  // NEW

        _spawnController.OnFillComplete += OnGridFilled;
        _swapController.OnSwapComplete += OnSwapComplete;  // NEW

        _spawnController.FillGrid();
    }

    private void OnGridFilled()
    {
        Debug.Log("[Match3] Grid ready. Swipe to play!");
    }

    private void OnSwapComplete(GridPosition a, GridPosition b)
    {
        Debug.Log($"[Match3] Swap complete: {a} ↔ {b}");
        // TODO: Check matches, destroy, gravity...
        _swapController.EnableInput();  // Временно, пока нет GameLoop
    }
}
```

---

## Edge Cases

| Ситуация | Решение |
|----------|---------|
| Свайп за пределы сетки | `IsValidGridPosition` отклоняет |
| Свайп во время анимации | `_isSwapping` блокирует |
| Свайп на пустую ячейку | `GetElement == null` отклоняет |
| Диагональный свайп | `AreNeighbors` отклоняет (только ортогональные) |
| Очень быстрый свайп | `MaxSwipeTime` проверка |
| Слишком короткий свайп | `MinSwipeDistance` проверка |
| Multi-touch | Обрабатывается только первый touch |

---

## Тестирование

### Unit Tests (SwapValidator):

```csharp
[Test]
public void AreNeighbors_Horizontal_ReturnsTrue()
{
    var validator = CreateValidator();
    Assert.IsTrue(validator.AreNeighbors(new GridPosition(2, 3), new GridPosition(3, 3)));
}

[Test]
public void AreNeighbors_Diagonal_ReturnsFalse()
{
    var validator = CreateValidator();
    Assert.IsFalse(validator.AreNeighbors(new GridPosition(2, 3), new GridPosition(3, 4)));
}

[Test]
public void WouldCreateMatch_WithMatch_ReturnsTrue()
{
    var grid = CreateGridWithPotentialMatch();
    var validator = new SwapValidator(grid, new LineMatchFinder());

    Assert.IsTrue(validator.WouldCreateMatch(new GridPosition(2, 0), new GridPosition(3, 0)));
}
```

### Manual Testing в Unity:

1. Запустить сцену
2. Свайп на элементе → должен определить направление
3. Валидный свап → элементы меняются местами
4. Невалидный свап → элементы "отскакивают" назад
5. Свайп за пределы сетки → ничего не происходит
6. Свайп во время анимации → игнорируется

---

## Checklist

- [ ] Создать папку `Assets/Scripts/Input/`
- [ ] Создать папку `Assets/Scripts/Swap/`
- [ ] Создать `Assets/Data/InputConfig.asset`
- [ ] Реализовать `InputConfig.cs`
- [ ] Реализовать `IInputHandler.cs`
- [ ] Реализовать `SwipeInputHandler.cs`
- [ ] Реализовать `SwapValidator.cs`
- [ ] Реализовать `SwapAnimator.cs`
- [ ] Реализовать `SwapController.cs`
- [ ] Обновить `GameBootstrap.cs`
- [ ] Обновить `Match3SceneSetup.cs`
- [ ] Тест: свайп детектируется в консоли
- [ ] Тест: валидный свап меняет элементы
- [ ] Тест: невалидный свап делает shake и откат

---

## Следующие шаги

**Phase 7: Destruction System**
- `DestructionController` — уничтожение матчей
- Анимации исчезновения
- Возврат элементов в пул

**Phase 8: Gravity System**
- `GravityController` — падение элементов
- Спаун новых сверху
- Каскадная проверка матчей
