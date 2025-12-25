# Этап 6: Swap System - Детальный План Реализации

## Статус: ЗАВЕРШЁН

## Обзор

Swap System обрабатывает обмен позициями двух соседних элементов. Включает анимацию и валидацию (привёл ли свап к матчу). Если матча нет - автоматический реверс.

### Связь с другими системами

```
InputDetector.OnSwapRequested
         │
         ▼
┌─────────────────┐
│   SwapHandler   │ ──── InputBlocker.Block()
│                 │
│ 1. Get elements │
│ 2. Animate swap │
│ 3. Update board │
│ 4. Check match  │ ──► [MatchFinder - Этап 7]
│ 5. Revert/Done  │
└────────┬────────┘
         │
         ▼
   InputBlocker.Unblock()
```

### Зависимости

- `BoardComponent` - хранит данные, метод `SwapElements()`
- `GridComponent` - `GridToWorld()` для позиционирования
- `InputDetector` - событие `OnSwapRequested`
- `InputBlocker` - блокировка ввода во время анимации
- `DOTween` - анимации

---

## Архитектура

### Компоненты

| Компонент | Ответственность |
|-----------|-----------------|
| `SwapHandler` | Координация процесса свапа, подписка на события, оркестрация |
| `SwapAnimator` | DOTween анимации обмена элементов |

### Почему нет SwapValidator?

Валидация матчей будет в **Этапе 7 (MatchFinder)**. На этом этапе SwapHandler будет иметь временную заглушку `CheckForMatch()` которая всегда возвращает `true` (матч есть). Это позволит:
1. Полностью протестировать анимации свапа
2. Не блокировать разработку
3. Легко интегрировать MatchFinder позже

---

## Файлы для создания

```
Assets/Scripts/Swap/
├── SwapHandler.cs      # Координатор свапа
└── SwapAnimator.cs     # Анимации DOTween

Assets/Scripts/Editor/
└── SwapSystemSetup.cs  # Editor setup
```

---

## 6.1 SwapAnimator.cs

### Назначение
Анимирует обмен позициями двух элементов с использованием DOTween. Стиль: Bouncy/Casual с overshoot.

### Дизайн

```
Элемент A ────────► Позиция B
                         (overshoot → settle)
Элемент B ────────► Позиция A
                         (overshoot → settle)
```

### Код

```csharp
using System;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Swap
{
    /// <summary>
    /// Animates element swapping with bouncy casual style.
    /// </summary>
    public class SwapAnimator : MonoBehaviour
    {
        // === EVENTS ===

        public event Action OnSwapAnimationComplete;

        // === SETTINGS ===

        [Header("Settings")]
        [SerializeField] private float _swapDuration = 0.25f;
        [SerializeField] private Ease _swapEase = Ease.OutBack;

        [Header("Bounce Settings")]
        [SerializeField] private float _overshoot = 1.5f;

        // === PRIVATE FIELDS ===

        private Sequence _currentSequence;

        // === PUBLIC METHODS ===

        /// <summary>
        /// Animates swap between two elements. Updates only visual positions.
        /// </summary>
        public void AnimateSwap(ElementComponent elementA, ElementComponent elementB,
            Vector3 targetPosA, Vector3 targetPosB, Action onComplete = null)
        {
            KillCurrentAnimation();

            _currentSequence = DOTween.Sequence();

            // Both elements move simultaneously
            _currentSequence.Join(
                elementA.transform.DOMove(targetPosA, _swapDuration)
                    .SetEase(_swapEase, _overshoot)
            );

            _currentSequence.Join(
                elementB.transform.DOMove(targetPosB, _swapDuration)
                    .SetEase(_swapEase, _overshoot)
            );

            _currentSequence.OnComplete(() =>
            {
                OnSwapAnimationComplete?.Invoke();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Animates revert (same animation, elements go back)
        /// </summary>
        public void AnimateRevert(ElementComponent elementA, ElementComponent elementB,
            Vector3 originalPosA, Vector3 originalPosB, Action onComplete = null)
        {
            KillCurrentAnimation();

            _currentSequence = DOTween.Sequence();

            // Slightly different ease for "failed" feel
            _currentSequence.Join(
                elementA.transform.DOMove(originalPosA, _swapDuration)
                    .SetEase(Ease.OutQuad)
            );

            _currentSequence.Join(
                elementB.transform.DOMove(originalPosB, _swapDuration)
                    .SetEase(Ease.OutQuad)
            );

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Kills any running animation
        /// </summary>
        public void KillCurrentAnimation()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        // === UNITY CALLBACKS ===

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
```

### Параметры анимации

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_swapDuration` | 0.25f | Длительность анимации |
| `_swapEase` | OutBack | Ease с перелётом (bounce) |
| `_overshoot` | 1.5f | Сила перелёта для OutBack |

### Почему OutBack?

`Ease.OutBack` создаёт эффект "перелёта" - элемент проскакивает целевую позицию и возвращается назад. Это даёт "bouncy/casual" ощущение из параметров проекта.

---

## 6.2 SwapHandler.cs

### Назначение
Координатор процесса свапа. Подписывается на `InputDetector.OnSwapRequested`, управляет последовательностью действий.

### Логика работы

```
OnSwapRequested(posA, posB)
         │
         ▼
    Block Input
         │
         ▼
   Get Elements A, B
         │
         ▼
   Animate Swap ─────► Wait for animation
         │
         ▼
   Update Board Data
         │
         ▼
   Check Match? ─────► [TODO: Этап 7]
         │
    ┌────┴────┐
    │         │
   YES        NO
    │         │
    ▼         ▼
  Done    Revert Swap
    │         │
    │    ┌────┘
    ▼    ▼
   Unblock Input
```

### Код

```csharp
using System;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Input;
using Match3.Elements;

namespace Match3.Swap
{
    /// <summary>
    /// Handles swap logic: validates, animates, and checks for matches.
    /// </summary>
    public class SwapHandler : MonoBehaviour
    {
        // === EVENTS ===

        /// <summary>
        /// Called when swap animation starts
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwapStarted;

        /// <summary>
        /// Called when swap completes successfully (match found)
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;

        /// <summary>
        /// Called when swap is reverted (no match)
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwapReverted;

        // === DEPENDENCIES ===

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private InputDetector _inputDetector;
        [SerializeField] private InputBlocker _inputBlocker;
        [SerializeField] private SwapAnimator _swapAnimator;

        // === PRIVATE FIELDS ===

        private bool _isProcessing;

        // === UNITY CALLBACKS ===

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
        }

        // === PUBLIC METHODS ===

        /// <summary>
        /// Can be called directly for testing or AI moves
        /// </summary>
        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            HandleSwapRequest(posA, posB);
        }

        // === PRIVATE METHODS ===

        private void HandleSwapRequest(Vector2Int posA, Vector2Int posB)
        {
            if (_isProcessing) return;

            // Validate positions
            if (!CanSwap(posA, posB)) return;

            // Get elements
            var elementA = _board.GetElement(posA);
            var elementB = _board.GetElement(posB);

            if (elementA == null || elementB == null) return;

            StartSwap(posA, posB, elementA, elementB);
        }

        private bool CanSwap(Vector2Int posA, Vector2Int posB)
        {
            // Check valid positions
            if (!_grid.IsValidPosition(posA) || !_grid.IsValidPosition(posB))
                return false;

            // Check adjacency
            int dx = Mathf.Abs(posA.x - posB.x);
            int dy = Mathf.Abs(posA.y - posB.y);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private void StartSwap(Vector2Int posA, Vector2Int posB,
            ElementComponent elementA, ElementComponent elementB)
        {
            _isProcessing = true;
            _inputBlocker.Block();

            OnSwapStarted?.Invoke(posA, posB);

            // Calculate target world positions
            Vector3 targetPosA = _grid.GridToWorld(posB);
            Vector3 targetPosB = _grid.GridToWorld(posA);

            // Store original positions for potential revert
            Vector3 originalPosA = elementA.transform.position;
            Vector3 originalPosB = elementB.transform.position;

            // Animate swap
            _swapAnimator.AnimateSwap(elementA, elementB, targetPosA, targetPosB, () =>
            {
                // Update board data
                _board.SwapElements(posA, posB);

                // Check for match
                bool hasMatch = CheckForMatch(posA, posB);

                if (hasMatch)
                {
                    CompleteSwap(posA, posB);
                }
                else
                {
                    RevertSwap(posA, posB, elementA, elementB, originalPosA, originalPosB);
                }
            });
        }

        private void RevertSwap(Vector2Int posA, Vector2Int posB,
            ElementComponent elementA, ElementComponent elementB,
            Vector3 originalPosA, Vector3 originalPosB)
        {
            // Revert board data first
            _board.SwapElements(posA, posB);

            // Animate revert
            _swapAnimator.AnimateRevert(elementA, elementB, originalPosA, originalPosB, () =>
            {
                OnSwapReverted?.Invoke(posA, posB);
                FinishSwap();
            });
        }

        private void CompleteSwap(Vector2Int posA, Vector2Int posB)
        {
            OnSwapCompleted?.Invoke(posA, posB);
            FinishSwap();
        }

        private void FinishSwap()
        {
            _isProcessing = false;
            _inputBlocker.Unblock();
        }

        /// <summary>
        /// Stub for match checking. Will be replaced by MatchFinder in Stage 7.
        /// Currently always returns true (assumes match exists).
        /// </summary>
        private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
        {
            // TODO: Replace with actual MatchFinder in Stage 7
            // For now, always return true to test swap animation
            return true;
        }
    }
}
```

### События

| Событие | Когда | Использование |
|---------|-------|---------------|
| `OnSwapStarted` | Начало свапа | Звуки, UI |
| `OnSwapCompleted` | Свап успешен (есть матч) | Этап 7: запуск поиска матчей |
| `OnSwapReverted` | Свап отменён (нет матча) | Звук "неудачи" |

---

## 6.3 SwapSystemSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Swap;
using Match3.Grid;
using Match3.Board;
using Match3.Input;

namespace Match3.Editor
{
    public static class SwapSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 6 - Swap System")]
        public static void SetupSwapSystem()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            var board = grid.GetComponent<BoardComponent>();
            if (board == null)
            {
                Debug.LogError("[Match3] BoardComponent not found. Run Stage 4 setup first.");
                return;
            }

            var inputDetector = grid.GetComponent<InputDetector>();
            var inputBlocker = grid.GetComponent<InputBlocker>();

            if (inputDetector == null || inputBlocker == null)
            {
                Debug.LogError("[Match3] Input system not found. Run Stage 5 setup first.");
                return;
            }

            var gameObject = grid.gameObject;

            // SwapAnimator
            var swapAnimator = gameObject.GetComponent<SwapAnimator>();
            if (swapAnimator == null)
                swapAnimator = Undo.AddComponent<SwapAnimator>(gameObject);

            // SwapHandler
            var swapHandler = gameObject.GetComponent<SwapHandler>();
            if (swapHandler == null)
                swapHandler = Undo.AddComponent<SwapHandler>(gameObject);

            SetField(swapHandler, "_board", board);
            SetField(swapHandler, "_grid", grid);
            SetField(swapHandler, "_inputDetector", inputDetector);
            SetField(swapHandler, "_inputBlocker", inputBlocker);
            SetField(swapHandler, "_swapAnimator", swapAnimator);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[Match3] Swap System setup complete!");
        }

        private static void SetField<T>(Component component, string fieldName, T value) where T : Object
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `SwapAnimator.cs` | DOTween | Создать, проверить компиляцию |
| 2 | `SwapHandler.cs` | Board, Grid, Input, SwapAnimator | Создать, проверить компиляцию |
| 3 | `SwapSystemSetup.cs` | Все выше | Меню создаёт компоненты |
| 4 | Play Mode Test | - | Свап работает с анимацией |

---

## Тестирование

### Ручной тест

1. Запустить Play Mode
2. Кликнуть по элементу → выделился
3. Свайпнуть → элементы должны поменяться местами с bouncy анимацией
4. В консоли: `OnSwapStarted`, `OnSwapCompleted`
5. Элементы остаются на новых позициях (т.к. `CheckForMatch()` возвращает `true`)

### Тест реверта

Временно изменить `CheckForMatch()`:

```csharp
private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
{
    return false; // Всегда реверт
}
```

Теперь при свапе элементы должны вернуться на исходные позиции.

### Debug компонент (временный)

```csharp
using UnityEngine;
using Match3.Swap;

namespace Match3.Debug
{
    public class SwapDebugger : MonoBehaviour
    {
        [SerializeField] private SwapHandler _swapHandler;

        private void OnEnable()
        {
            _swapHandler.OnSwapStarted += (a, b) =>
                UnityEngine.Debug.Log($"Swap started: {a} <-> {b}");
            _swapHandler.OnSwapCompleted += (a, b) =>
                UnityEngine.Debug.Log($"Swap completed: {a} <-> {b}");
            _swapHandler.OnSwapReverted += (a, b) =>
                UnityEngine.Debug.Log($"Swap reverted: {a} <-> {b}");
        }
    }
}
```

---

## Интеграция с Этапом 7 (Match Detection)

После реализации MatchFinder в Этапе 7, нужно:

1. Добавить зависимость `MatchFinder` в `SwapHandler`
2. Заменить заглушку `CheckForMatch()`:

```csharp
// SwapHandler.cs - после Этапа 7
[SerializeField] private MatchFinder _matchFinder;

private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
{
    // Проверяем матчи в обеих позициях
    var matchesA = _matchFinder.FindMatchesAt(posA);
    var matchesB = _matchFinder.FindMatchesAt(posB);

    return matchesA.Count > 0 || matchesB.Count > 0;
}
```

---

## Возможные улучшения (будущее)

1. **Swap звуки** - whoosh при свапе, thud при реверте
2. **Shake при реверте** - небольшое дрожание элементов
3. **Trail эффект** - визуальный след при движении
4. **Подсказки** - подсветка возможных свапов при idle

---

## Чеклист

- [x] Создать папку `Assets/Scripts/Swap/`
- [x] `SwapAnimator.cs` создан
- [x] `SwapHandler.cs` создан
- [x] `SwapSystemSetup.cs` создан
- [ ] Меню Setup работает
- [ ] Свап анимируется (bouncy)
- [ ] BoardComponent обновляется
- [ ] InputBlocker блокирует во время анимации
- [ ] События публикуются корректно
- [ ] Тест реверта работает (при false в CheckForMatch)

---

## Диаграмма компонентов на GameObject

После Stage 6 на GameManager объекте:

```
GameManager (GameObject)
├── GridComponent          [Stage 1]
├── BoardComponent         [Stage 4]
├── ElementPool            [Stage 3]
├── ElementFactory         [Stage 3]
├── InitialBoardSpawner    [Stage 3]
├── InputBlocker           [Stage 5]
├── InputDetector          [Stage 5]
├── SelectionHighlighter   [Stage 5]
├── SwapAnimator           [Stage 6] ← NEW
└── SwapHandler            [Stage 6] ← NEW
```

---

## Известные ограничения

### CheckForMatch() - заглушка

На этом этапе `CheckForMatch()` всегда возвращает `true`. Это означает:
- Все свапы считаются успешными
- Реверт не происходит
- Для тестирования реверта нужно вручную изменить return

Это будет исправлено в Этапе 7 (Match Detection).
