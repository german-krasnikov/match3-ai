# Phase 6: Input & Swap System — Реализация

## Статус: ✅ ГОТОВО

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

**Зависимости:** GridData, GridConfig, GridPositionConverter, MatchController, ElementView

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                   SwipeInputHandler                          │
│                    (MonoBehaviour)                           │
│  • Touch/Mouse detection                                     │
│  • Swipe direction calculation (доминантная ось)            │
│  • События: OnSwipeDetected(GridPosition from, direction)   │
└─────────────────────┬───────────────────────────────────────┘
                      │ fires event
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    SwapController                            │
│                    (MonoBehaviour)                           │
│  • Проверка соседства (AreNeighbors) до анимации           │
│  • Проверка матча (WouldCreateMatch)                        │
│  • Запуск анимации через SwapAnimator                       │
│  • События: OnSwapComplete, OnSwapFailed                    │
└────────────┬────────────────────────┬───────────────────────┘
             │                        │
             ▼                        ▼
┌─────────────────────┐  ┌─────────────────────────────────────┐
│   SwapValidator     │  │         SwapAnimator                 │
│    (Pure C#)        │  │        (MonoBehaviour)               │
│  • AreNeighbors     │  │  • AnimateSwap (DOTween Sequence)   │
│  • WouldCreateMatch │  │  • AnimateInvalidSwap (туда-обратно)│
└─────────────────────┘  └─────────────────────────────────────┘
```

---

## Реализованные файлы

### 6.1 InputConfig.cs

**Файл:** `Assets/Scripts/Input/InputConfig.cs`

```csharp
using UnityEngine;

namespace Match3.Input
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Match3/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [SerializeField] private float _minSwipeDistance = 30f;
        [SerializeField] private float _maxSwipeTime = 0.5f;

        public float MinSwipeDistance => _minSwipeDistance;
        public float MaxSwipeTime => _maxSwipeTime;
    }
}
```

---

### 6.2 IInputHandler.cs

**Файл:** `Assets/Scripts/Input/IInputHandler.cs`

```csharp
using System;
using Match3.Core;

namespace Match3.Input
{
    public interface IInputHandler
    {
        event Action<GridPosition, GridPosition> OnSwipeDetected;
        void SetEnabled(bool enabled);
        bool IsEnabled { get; }
    }
}
```

---

### 6.3 SwipeInputHandler.cs

**Файл:** `Assets/Scripts/Input/SwipeInputHandler.cs`

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
            if (!enabled) _isSwiping = false;
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
                TryStartSwipe(UnityEngine.Input.mousePosition);
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isSwiping)
                TryCompleteSwipe(UnityEngine.Input.mousePosition);
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

            _isSwiping = false;

            if (elapsed > _config.MaxSwipeTime) return;
            if (distance < _config.MinSwipeDistance) return;

            var direction = GetSwipeDirection(delta);
            OnSwipeDetected?.Invoke(_startGridPos, direction);
        }

        private GridPosition GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? GridPosition.Right : GridPosition.Left;
            return delta.y > 0 ? GridPosition.Up : GridPosition.Down;
        }

        private bool IsValidGridPosition(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < _gridView.Config.Width &&
                   pos.Y >= 0 && pos.Y < _gridView.Config.Height;
        }
    }
}
```

---

### 6.4 SwapValidator.cs

**Файл:** `Assets/Scripts/Swap/SwapValidator.cs`

```csharp
using Match3.Core;
using Match3.Grid;
using Match3.Match;
using UnityEngine;

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

        public bool AreNeighbors(GridPosition a, GridPosition b)
        {
            var delta = a - b;
            return (Mathf.Abs(delta.X) == 1 && delta.Y == 0) ||
                   (delta.X == 0 && Mathf.Abs(delta.Y) == 1);
        }

        public bool WouldCreateMatch(GridPosition a, GridPosition b)
        {
            _grid.SwapElements(a, b);
            var matches = _matchFinder.FindMatchesAt(_grid, new[] { a, b });
            _grid.SwapElements(a, b);
            return matches.Count > 0;
        }
    }
}
```

---

### 6.5 SwapAnimator.cs

**Файл:** `Assets/Scripts/Swap/SwapAnimator.cs`

```csharp
using System;
using DG.Tweening;
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

        private Sequence _currentSequence;

        public void AnimateSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            KillCurrent();

            var posA = _gridView.PositionConverter.GridToWorld(elementA.Position);
            var posB = _gridView.PositionConverter.GridToWorld(elementB.Position);
            var duration = _config.SwapDuration;

            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(elementA.Transform.DOMove(posB, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(posA, duration).SetEase(Ease.OutQuad));
            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        public void AnimateInvalidSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            KillCurrent();

            var posA = elementA.Transform.position;
            var posB = elementB.Transform.position;
            var duration = _config.SwapDuration;

            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(elementA.Transform.DOMove(posB, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(posA, duration).SetEase(Ease.OutQuad));
            _currentSequence.Append(elementA.Transform.DOMove(posA, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(posB, duration).SetEase(Ease.OutQuad));
            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void KillCurrent()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDisable() => KillCurrent();
    }
}
```

---

### 6.6 SwapController.cs

**Файл:** `Assets/Scripts/Swap/SwapController.cs`

```csharp
using System;
using Match3.Core;
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
            TrySwap(from, from + direction);
        }

        public void TrySwap(GridPosition a, GridPosition b)
        {
            if (_isSwapping) return;

            if (!_validator.AreNeighbors(a, b)) return;

            var elementA = _grid.GetElement(a);
            var elementB = _grid.GetElement(b);
            if (elementA == null || elementB == null) return;

            _isSwapping = true;
            _inputHandler.SetEnabled(false);

            bool isValid = _validator.WouldCreateMatch(a, b);

            if (isValid)
                ExecuteValidSwap(a, b, elementA, elementB);
            else
                ExecuteInvalidSwap(elementA, elementB);
        }

        private void ExecuteValidSwap(GridPosition a, GridPosition b, Elements.IElement elementA, Elements.IElement elementB)
        {
            _grid.SwapElements(a, b);
            elementA.Position = b;
            elementB.Position = a;

            _animator.AnimateSwap(elementA, elementB, () =>
            {
                _isSwapping = false;
                OnSwapComplete?.Invoke(a, b);
            });
        }

        private void ExecuteInvalidSwap(Elements.IElement elementA, Elements.IElement elementB)
        {
            _animator.AnimateInvalidSwap(elementA, elementB, () =>
            {
                _isSwapping = false;
                _inputHandler.SetEnabled(true);
                OnSwapFailed?.Invoke();
            });
        }

        public void EnableInput() => _inputHandler.SetEnabled(true);
    }
}
```

---

## Поток выполнения

```
OnSwipeDetected(from, direction)
│
├── _isSwapping? ─── Yes ──► return
│
├── to = from + direction
│
├── AreNeighbors(from, to)? ─── No ──► return (диагональ игнорируется)
│
├── GetElement(from), GetElement(to)
│   └── null? ─── Yes ──► return
│
├── _isSwapping = true
├── _inputHandler.SetEnabled(false)
│
└── WouldCreateMatch(from, to)?
    │
    ├── Yes (Valid):
    │   ├── grid.SwapElements(a, b)
    │   ├── Update element.Position
    │   ├── AnimateSwap()
    │   └── OnComplete → OnSwapComplete
    │
    └── No (Invalid):
        ├── AnimateInvalidSwap() (туда-обратно)
        └── OnComplete → EnableInput, OnSwapFailed
```

---

## Scene Setup

### Unity Menu:

```
Match3 → Add Swap System
```

Автоматически создаёт SwipeInputHandler, SwapAnimator, SwapController и связывает с GameBootstrap.

### Иерархия:

```
Scene
├── Main Camera               [Camera]
├── Grid                      [GridView]
├── ElementPool               [ElementPool]
├── Elements                  (parent)
├── ElementFactory            [ElementFactory]
├── SpawnController           [SpawnController]
├── MatchController           [MatchController]
├── SwipeInputHandler         [SwipeInputHandler]
├── SwapAnimator              [SwapAnimator]
├── SwapController            [SwapController]
└── GameBootstrap             [GameBootstrap]
```

---

## Checklist

- [x] InputConfig.cs
- [x] IInputHandler.cs
- [x] SwipeInputHandler.cs
- [x] SwapValidator.cs
- [x] SwapAnimator.cs
- [x] SwapController.cs
- [x] GameBootstrap.cs обновлён
- [x] Match3SceneSetup.cs — Add Swap System
- [x] Диагональные свапы игнорируются
- [x] Анимация через DOTween.Sequence

---

## Следующие шаги

**Phase 7: Destruction System**
- Уничтожение элементов после матча
- Анимации исчезновения
- Возврат элементов в пул

**Phase 8: Gravity System**
- Падение элементов
- Спаун новых сверху
- Каскадная проверка матчей
