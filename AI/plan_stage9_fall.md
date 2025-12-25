# Этап 9: Fall System - Детальный План Реализации

## Статус: ЗАВЕРШЁН ✅

---

## Обзор

Fall System отвечает за падение элементов после уничтожения матчей. Когда DestroyHandler удаляет элементы и создаёт "дыры" на доске, FallSystem заполняет их, сдвигая элементы сверху вниз.

### Связь с другими системами

```
DestroyHandler.OnDestroyCompleted(count)
         │
         ▼
┌─────────────────────────────────────┐
│  FallHandler.ExecuteFalls()       ◄─┼── ЭТАП 9
│         │                           │
│         ▼                           │
│  FallCalculator.CalculateFalls()    │
│         │                           │
│         ▼                           │
│  BoardComponent.SetElement()        │
│         │                           │
│         ▼                           │
│  FallAnimator.AnimateFalls()        │
│         │                           │
│         ▼                           │
│  OnFallsCompleted                   │
└─────────────────────────────────────┘
         │
         ▼
   [Этап 10: RefillHandler]
```

### Зависимости

| Зависимость | Использование |
|-------------|---------------|
| `BoardComponent` | `GetEmptyRowsInColumn(col)`, `GetElement()`, `SetElement()` |
| `GridComponent` | `GridToWorld(pos)` — позиции для анимации |
| `DestroyHandler` | `OnDestroyCompleted` — триггер для запуска |

---

## Архитектура

### Компоненты

| Компонент | Ответственность | События |
|-----------|-----------------|---------|
| `FallCalculator` | Расчёт какие элементы куда падают | — |
| `FallHandler` | Координация процесса падения | `OnFallsStarted`, `OnFallsCompleted` |
| `FallAnimator` | DOTween анимации падения с bounce | — |

### Принцип разделения (Unity Way)

```
FallHandler              FallCalculator           FallAnimator
(координация)            (логика/данные)          (визуал)
      │                        │                       │
      │  1. CalculateFalls()   │                       │
      ├───────────────────────►│                       │
      │                        │                       │
      │◄───────────────────────┤ List<FallData>        │
      │                        │                       │
      │  2. Update Board       │                       │
      │                        │                       │
      ├────────────────────────────────────────────────►│ 3. AnimateFalls()
      │                        │                       │
      │◄───────────────────────────────────────────────┤ 4. OnComplete
      │                        │                       │
      │  5. Fire OnFallsCompleted                      │
      ▼                        ▼                       ▼
```

---

## Алгоритм падения

### Визуализация

```
До падения:                После падения:

y=4: G _ _ P R            y=4: _ _ _ _ _    (пусто - заполнит Refill)
y=3: B _ R Y G            y=3: G _ _ P R
y=2: _ _ _ G B            y=2: B _ R Y G
y=1: P Y R B G            y=1: P Y R G B
y=0: R B G Y P            y=0: R B G Y P
     0 1 2 3 4                 0 1 2 3 4

Столбец 0:                Столбец 2:
G падает на 2 клетки      R падает на 1 клетку
B падает на 1 клетку
```

### Алгоритм по столбцам

```
Для каждого столбца (x = 0 до Width-1):
  1. Найти все пустые ячейки в столбце (снизу вверх)
  2. Для каждой пустой ячейки (y_empty):
     - Найти ближайший элемент сверху (y_element > y_empty)
     - Если найден:
       - Создать FallData(element, from: y_element, to: y_empty)
       - Пометить y_element как пустую
  3. Собрать все FallData
```

### Пример работы алгоритма

```
Столбец 0: [R, P, _, B, G]  (индексы 0-4, _ = пустая на y=2)

Шаг 1: Пустые = [2]
Шаг 2: Ищем элемент выше y=2 → B на y=3
       FallData(B, from=3, to=2)
       Столбец теперь: [R, P, B, _, G]
       Пустые обновлены = [3]

Шаг 3: Ищем элемент выше y=3 → G на y=4
       FallData(G, from=4, to=3)
       Столбец теперь: [R, P, B, G, _]

Результат: [FallData(B, 3→2), FallData(G, 4→3)]
```

---

## Файлы для создания

```
Assets/Scripts/Fall/
├── FallData.cs            # Struct данных о падении
├── FallCalculator.cs      # Расчёт падений
├── FallHandler.cs         # Координация
└── FallAnimator.cs        # Анимации

Assets/Scripts/Editor/
└── FallSystemSetup.cs     # Editor setup
```

---

## 9.1 FallData.cs

### Назначение

Readonly struct с информацией о падении одного элемента.

### Код

```csharp
using UnityEngine;
using Match3.Elements;

namespace Match3.Fall
{
    /// <summary>
    /// Data about a single element fall movement.
    /// </summary>
    public readonly struct FallData
    {
        public ElementComponent Element { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public int Distance { get; }

        public FallData(ElementComponent element, Vector2Int from, Vector2Int to)
        {
            Element = element;
            From = from;
            To = to;
            Distance = from.y - to.y;
        }

        public override string ToString()
            => $"Fall: {Element?.Type} from {From} to {To} (dist={Distance})";
    }
}
```

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Element` | `ElementComponent` | Падающий элемент |
| `From` | `Vector2Int` | Исходная позиция |
| `To` | `Vector2Int` | Целевая позиция |
| `Distance` | `int` | Расстояние падения (для расчёта времени) |

---

## 9.2 FallCalculator.cs

### Назначение

Статический класс для расчёта падений. Не MonoBehaviour — чистая логика.

### Код

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Elements;

namespace Match3.Fall
{
    /// <summary>
    /// Calculates which elements need to fall and where.
    /// </summary>
    public static class FallCalculator
    {
        private static readonly List<FallData> _fallsBuffer = new(64);

        /// <summary>
        /// Calculates all falls for the current board state.
        /// Elements fall down to fill empty spaces.
        /// </summary>
        public static List<FallData> CalculateFalls(BoardComponent board)
        {
            _fallsBuffer.Clear();

            for (int x = 0; x < board.Width; x++)
            {
                CalculateColumnFalls(board, x);
            }

            return new List<FallData>(_fallsBuffer);
        }

        private static void CalculateColumnFalls(BoardComponent board, int column)
        {
            // Scan from bottom to top
            int writeIndex = 0; // Where the next element should land

            for (int y = 0; y < board.Height; y++)
            {
                var pos = new Vector2Int(column, y);
                var element = board.GetElement(pos);

                if (element != null)
                {
                    if (y != writeIndex)
                    {
                        // Element needs to fall
                        var from = pos;
                        var to = new Vector2Int(column, writeIndex);
                        _fallsBuffer.Add(new FallData(element, from, to));
                    }
                    writeIndex++;
                }
            }
        }
    }
}
```

### Алгоритм

Используем подход "gravity simulation":
1. `writeIndex` — следующая доступная позиция для элемента (снизу вверх)
2. Проходим столбец снизу вверх
3. Если нашли элемент:
   - Если он не на своём месте (y != writeIndex) → создаём FallData
   - Увеличиваем writeIndex
4. Пустые ячейки пропускаем (writeIndex остаётся на месте)

### Визуализация алгоритма

```
Столбец: [R, _, P, _, G]   (R на y=0, P на y=2, G на y=4)
          0  1  2  3  4

writeIndex = 0
y=0: R есть, y==writeIndex → ничего не делаем, writeIndex=1
y=1: пусто → пропуск
y=2: P есть, y(2) != writeIndex(1) → FallData(P, 2→1), writeIndex=2
y=3: пусто → пропуск
y=4: G есть, y(4) != writeIndex(2) → FallData(G, 4→2), writeIndex=3

Результат: [FallData(P, 2→1), FallData(G, 4→2)]

После падения: [R, P, G, _, _]
```

---

## 9.3 FallHandler.cs

### Назначение

Координирует процесс падения: вызывает расчёт, обновляет Board, запускает анимации.

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;

namespace Match3.Fall
{
    /// <summary>
    /// Handles the fall process: calculates, updates board, animates.
    /// </summary>
    public class FallHandler : MonoBehaviour
    {
        public event Action OnFallsStarted;
        public event Action OnFallsCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private FallAnimator _animator;

        private List<FallData> _currentFalls;

        public bool IsFalling { get; private set; }

        /// <summary>
        /// Calculates and executes all falls for current board state.
        /// </summary>
        public void ExecuteFalls()
        {
            if (IsFalling) return;

            _currentFalls = FallCalculator.CalculateFalls(_board);

            if (_currentFalls.Count == 0)
            {
                OnFallsCompleted?.Invoke();
                return;
            }

            IsFalling = true;
            OnFallsStarted?.Invoke();

            UpdateBoardState();
            AnimateFalls();
        }

        private void UpdateBoardState()
        {
            // First pass: clear old positions
            foreach (var fall in _currentFalls)
            {
                _board.SetElement(fall.From, null);
            }

            // Second pass: set new positions
            foreach (var fall in _currentFalls)
            {
                _board.SetElement(fall.To, fall.Element);
            }
        }

        private void AnimateFalls()
        {
            var worldPositions = new List<Vector3>(_currentFalls.Count);
            foreach (var fall in _currentFalls)
            {
                worldPositions.Add(_grid.GridToWorld(fall.To));
            }

            _animator.AnimateFalls(_currentFalls, worldPositions, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            IsFalling = false;
            _currentFalls = null;
            OnFallsCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Execute Falls")]
        private void TestExecuteFalls()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[FallHandler] Only works in Play Mode");
                return;
            }

            var falls = FallCalculator.CalculateFalls(_board);
            Debug.Log($"[FallHandler] Calculated {falls.Count} falls:");
            foreach (var fall in falls)
            {
                Debug.Log($"  {fall}");
            }

            ExecuteFalls();
        }
#endif
    }
}
```

### Важные моменты

| Момент | Решение |
|--------|---------|
| Порядок обновления | Сначала очищаем From, потом ставим To (избегаем перезаписи) |
| IsFalling flag | Защита от повторного вызова |
| Board vs Animation | Board обновляется сразу, анимация догоняет визуально |

---

## 9.4 FallAnimator.cs

### Назначение

Анимирует падение элементов с помощью DOTween. Bouncy/Casual стиль — элементы "приземляются" с отскоком.

### Анимация

```
Начало:           Падение:          Приземление:

  ●
  │
  │                  ●                 ●  (bounce)
  │                  │                 │
  ▼                  ▼               ──┴──
```

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Fall
{
    /// <summary>
    /// Animates element falls with bouncy landing effect.
    /// </summary>
    public class FallAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _fallSpeed = 12f; // Units per second
        [SerializeField] private float _minFallDuration = 0.1f;
        [SerializeField] private float _maxFallDuration = 0.5f;
        [SerializeField] private float _staggerDelay = 0.02f;

        [Header("Effects")]
        [SerializeField] private Ease _fallEase = Ease.InQuad;
        [SerializeField] private float _bounceStrength = 0.15f;
        [SerializeField] private float _bounceDuration = 0.15f;
        [SerializeField] private int _bounceVibrato = 1;

        private Sequence _currentSequence;

        public void AnimateFalls(List<FallData> falls, List<Vector3> targetPositions, Action onComplete)
        {
            KillCurrentAnimation();

            if (falls == null || falls.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            // Group by column for stagger effect
            var columnDelays = new Dictionary<int, float>();
            float currentDelay = 0f;

            for (int i = 0; i < falls.Count; i++)
            {
                var fall = falls[i];
                var targetPos = targetPositions[i];

                if (fall.Element == null) continue;

                // Calculate delay based on column
                int column = fall.From.x;
                if (!columnDelays.TryGetValue(column, out float delay))
                {
                    delay = currentDelay;
                    columnDelays[column] = delay;
                    currentDelay += _staggerDelay;
                }

                var elementSequence = CreateElementFallSequence(fall, targetPos);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementFallSequence(FallData fall, Vector3 targetPos)
        {
            var transform = fall.Element.transform;

            // Calculate duration based on distance
            float distance = fall.Distance;
            float duration = distance / _fallSpeed;
            duration = Mathf.Clamp(duration, _minFallDuration, _maxFallDuration);

            var seq = DOTween.Sequence();

            // Fall movement
            seq.Append(transform.DOMove(targetPos, duration).SetEase(_fallEase));

            // Bounce on landing (squash & stretch)
            seq.Append(transform.DOPunchScale(
                new Vector3(_bounceStrength, -_bounceStrength, 0),
                _bounceDuration,
                _bounceVibrato,
                0.5f
            ));

            return seq;
        }

        public void KillCurrentAnimation()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
```

### Параметры анимации (Inspector)

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_fallSpeed` | 12f | Скорость падения (units/sec) |
| `_minFallDuration` | 0.1f | Минимальная длительность |
| `_maxFallDuration` | 0.5f | Максимальная длительность |
| `_staggerDelay` | 0.02f | Задержка между столбцами |
| `_fallEase` | InQuad | Easing падения (ускорение) |
| `_bounceStrength` | 0.15f | Сила сжатия при приземлении |
| `_bounceDuration` | 0.15f | Длительность bounce |
| `_bounceVibrato` | 1 | Количество отскоков |

### Расчёт длительности

```
duration = distance / _fallSpeed
         = 3 cells / 12 (units/sec)
         = 0.25 sec (при cellSize = 1.0)

С clamp: min(0.1, max(0.25, 0.5)) = 0.25 sec
```

### Timeline

```
Column 0:  [──fall──][bounce]
Column 1:       [──fall──][bounce]
Column 2:            [──fall──][bounce]
           ├─0.02─┼─0.02─┼─0.02─►  (stagger)
```

---

## 9.5 FallSystemSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Fall;
using Match3.Grid;
using Match3.Board;
using Match3.Destroy;

namespace Match3.Editor
{
    public static class FallSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 9 - Fall System")]
        public static void SetupFallSystem()
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

            var destroyHandler = grid.GetComponent<DestroyHandler>();
            if (destroyHandler == null)
            {
                Debug.LogError("[Match3] DestroyHandler not found. Run Stage 8 setup first.");
                return;
            }

            var go = grid.gameObject;

            // FallAnimator
            var fallAnimator = go.GetComponent<FallAnimator>();
            if (fallAnimator == null)
                fallAnimator = Undo.AddComponent<FallAnimator>(go);

            // FallHandler
            var fallHandler = go.GetComponent<FallHandler>();
            if (fallHandler == null)
                fallHandler = Undo.AddComponent<FallHandler>(go);

            SetField(fallHandler, "_board", board);
            SetField(fallHandler, "_grid", grid);
            SetField(fallHandler, "_animator", fallAnimator);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Fall System setup complete!");
            Debug.Log("[Match3] NOTE: You need to manually wire DestroyHandler.OnDestroyCompleted -> FallHandler.ExecuteFalls");
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

## 9.6 Интеграция в SwapHandler

### Изменения в SwapHandler.cs

Нужно подключить FallHandler в цепочку:

```csharp
// Добавить using
using Match3.Fall;

// Добавить поле
[SerializeField] private FallHandler _fallHandler;

// Изменить подписки
private void OnEnable()
{
    _inputDetector.OnSwapRequested += HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
    _fallHandler.OnFallsCompleted += OnFallsCompleted;  // NEW
}

private void OnDisable()
{
    _inputDetector.OnSwapRequested -= HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
    _fallHandler.OnFallsCompleted -= OnFallsCompleted;  // NEW
}

// Изменить OnDestroyCompleted
private void OnDestroyCompleted(int count)
{
    // Вместо FinishSwap() запускаем падение
    _fallHandler.ExecuteFalls();
}

// Добавить новый метод
private void OnFallsCompleted()
{
    // TODO: После этапа 10 здесь будет RefillHandler
    // Пока просто завершаем
    FinishSwap();
}
```

### Полный обновлённый SwapHandler.cs

```csharp
using System;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Input;
using Match3.Elements;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;

namespace Match3.Swap
{
    public class SwapHandler : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapStarted;
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
        public event Action<Vector2Int, Vector2Int> OnSwapReverted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private InputDetector _inputDetector;
        [SerializeField] private InputBlocker _inputBlocker;
        [SerializeField] private SwapAnimator _swapAnimator;
        [SerializeField] private MatchFinder _matchFinder;
        [SerializeField] private DestroyHandler _destroyHandler;
        [SerializeField] private FallHandler _fallHandler;

        private bool _isProcessing;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
        }

        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            HandleSwapRequest(posA, posB);
        }

        private void HandleSwapRequest(Vector2Int posA, Vector2Int posB)
        {
            if (_isProcessing) return;
            if (!CanSwap(posA, posB)) return;

            var elementA = _board.GetElement(posA);
            var elementB = _board.GetElement(posB);

            if (elementA == null || elementB == null) return;

            StartSwap(posA, posB, elementA, elementB);
        }

        private bool CanSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!_grid.IsValidPosition(posA) || !_grid.IsValidPosition(posB))
                return false;

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

            Vector3 targetPosA = _grid.GridToWorld(posB);
            Vector3 targetPosB = _grid.GridToWorld(posA);

            Vector3 originalPosA = elementA.transform.position;
            Vector3 originalPosB = elementB.transform.position;

            _swapAnimator.AnimateSwap(elementA, elementB, targetPosA, targetPosB, () =>
            {
                _board.SwapElements(posA, posB);

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
            _board.SwapElements(posA, posB);

            _swapAnimator.AnimateRevert(elementA, elementB, originalPosA, originalPosB, () =>
            {
                OnSwapReverted?.Invoke(posA, posB);
                FinishSwap();
            });
        }

        private void CompleteSwap(Vector2Int posA, Vector2Int posB)
        {
            OnSwapCompleted?.Invoke(posA, posB);

            var matches = _matchFinder.FindAllMatches();
            if (matches.Count > 0)
            {
                _destroyHandler.DestroyMatches(matches);
            }
            else
            {
                FinishSwap();
            }
        }

        private void OnDestroyCompleted(int count)
        {
            // After destroy, execute falls
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            // TODO: Stage 10 - RefillHandler will be called here
            // For now, just finish the swap
            FinishSwap();
        }

        private void FinishSwap()
        {
            _isProcessing = false;
            _inputBlocker.Unblock();
        }

        private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
        {
            return _matchFinder.WouldCreateMatch(posA, posB);
        }
    }
}
```

---

## Диаграмма компонентов

После Stage 9 на GameManager объекте:

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
├── SwapAnimator           [Stage 6]
├── SwapHandler            [Stage 6] ← +_fallHandler
├── MatchFinder            [Stage 7]
├── MatchHighlighter       [Stage 7] (debug)
├── DestroyAnimator        [Stage 8]
├── DestroyHandler         [Stage 8]
├── FallAnimator           [Stage 9] ← NEW
└── FallHandler            [Stage 9] ← NEW
```

---

## Поток данных (полный)

```
User Swap Input
      │
      ▼
SwapHandler.HandleSwapRequest()
      │
      ├─[invalid]──► return
      │
      ▼
SwapAnimator.AnimateSwap()
      │
      ▼
BoardComponent.SwapElements()
      │
      ├─[no match]──► SwapAnimator.AnimateRevert() ──► FinishSwap()
      │
      ▼
MatchFinder.FindAllMatches()
      │
      ▼
DestroyHandler.DestroyMatches()
      │
      ▼
DestroyAnimator.AnimateDestroy()
      │
      ▼
OnDestroyCompleted
      │
      ▼
FallHandler.ExecuteFalls()        ◄── ЭТАП 9
      │
      ▼
FallCalculator.CalculateFalls()
      │
      ▼
BoardComponent.SetElement()
      │
      ▼
FallAnimator.AnimateFalls()
      │
      ▼
OnFallsCompleted
      │
      ▼
FinishSwap()   (TODO: Stage 10 - Refill)
      │
      ▼
InputBlocker.Unblock()
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `FallData.cs` | — | Compile |
| 2 | `FallCalculator.cs` | BoardComponent, FallData | Unit test / Debug.Log |
| 3 | `FallAnimator.cs` | DOTween, FallData | Visual в Scene |
| 4 | `FallHandler.cs` | All above | Context menu тест |
| 5 | `FallSystemSetup.cs` | All above | Меню создаёт компоненты |
| 6 | Update `SwapHandler.cs` | FallHandler | Полный flow тест |

---

## Тестирование

### Тест 1: FallCalculator

```csharp
// Временный тест в FallHandler:
[ContextMenu("Debug Calculate Falls")]
private void DebugCalculateFalls()
{
    var falls = FallCalculator.CalculateFalls(_board);
    Debug.Log($"[FallHandler] Calculated {falls.Count} falls:");
    foreach (var fall in falls)
    {
        Debug.Log($"  Column {fall.From.x}: {fall.Element?.Type} from y={fall.From.y} to y={fall.To.y}");
    }
}
```

### Тест 2: Визуальная проверка

1. Play Mode
2. Сделать свап, создающий матч
3. Наблюдать:
   - Элементы уничтожаются (Stage 8)
   - Элементы сверху падают вниз
   - Bounce эффект при приземлении
4. Board state корректен после падения

### Тест 3: Edge cases

1. **Пустой столбец**: Все элементы в столбце уничтожены → ничего не падает
2. **Верхний ряд пустой**: Элементы падают максимально вниз
3. **Множественные дыры**: Все элементы правильно группируются внизу

### Тест 4: Performance

```csharp
// В FallCalculator для дебага:
var sw = System.Diagnostics.Stopwatch.StartNew();
var falls = CalculateFalls(board);
Debug.Log($"[FallCalculator] Calculated {falls.Count} falls in {sw.ElapsedMilliseconds}ms");
```

Ожидаемо: < 1ms для 8x8 доски.

---

## Визуализация процесса

```
После матча:              После падения:              После Refill (Stage 10):

y=4: G _ _ P R           y=4: _ _ _ _ _               y=4: Y B R G P
y=3: B _ R Y G           y=3: G _ _ P R               y=3: G R G P R
y=2: _ _ _ G B           y=2: B _ R Y G               y=2: B Y R Y G
y=1: P Y R B G           y=1: P Y R G B               y=1: P Y R G B
y=0: R B G Y P           y=0: R B G Y P               y=0: R B G Y P
     0 1 2 3 4                0 1 2 3 4                    0 1 2 3 4

     ▲                        ▲                            ▲
     │                        │                            │
   Destroy                   Fall                        Refill
   (Stage 8)                (Stage 9)                   (Stage 10)
```

---

## Известные ограничения

### 1. Нет Refill

После падения верхние ячейки остаются пустыми. Это Refill System (Этап 10).

### 2. Нет Cascade

После Refill могут образоваться новые матчи. Cascade Loop — часть GameLoop (Этап 11).

### 3. Нет оптимизации множественных падений

Если уничтожено много элементов, все падения анимируются. Для очень больших досок может потребоваться batch optimization.

---

## Возможные улучшения

| Улучшение | Сложность | Описание |
|-----------|-----------|----------|
| Anticipation | Низкая | Элементы чуть "подпрыгивают" перед падением |
| Trail effect | Низкая | Визуальный след за падающим элементом |
| Sound | Низкая | Звук приземления (один или для каждого) |
| Variable speed | Средняя | Ускорение падения с высотой |
| Squash on impact | Средняя | Более выраженное сжатие при приземлении |

---

## Чеклист

### Код
- [x] Создать папку `Assets/Scripts/Fall/`
- [x] `FallData.cs` — readonly struct
- [x] `FallCalculator.cs` — статический класс расчёта
- [x] `FallAnimator.cs` — DOTween анимации
- [x] `FallHandler.cs` — координация
- [x] `FallSystemSetup.cs` — Editor menu

### Интеграция
- [x] SwapHandler получает ссылку на FallHandler
- [x] DestroyHandler.OnDestroyCompleted → FallHandler.ExecuteFalls
- [x] FallHandler.OnFallsCompleted → SwapHandler (временно до Stage 10)

### Тестирование в Unity
- [ ] Меню `Match3 → Setup Scene → Stage 9 - Fall System` работает
- [ ] Элементы падают после уничтожения матча
- [ ] Анимация падения плавная (InQuad)
- [ ] Bounce эффект при приземлении
- [ ] Board state корректен после падения
- [ ] Stagger delay создаёт каскадный эффект
- [ ] Пустые ячейки остаются сверху (для Refill)
- [ ] Input заблокирован во время падения

---

## FAQ

### Q: Почему Board обновляется до анимации?

A: Board = логическое состояние. Анимация = визуальное представление. Board должен быть актуален для следующих расчётов (Refill, Match detection). Анимация "догоняет" логику.

### Q: Почему FallCalculator статический?

A: Чистая функция без состояния. Не нужен MonoBehaviour. Проще тестировать, нет lifecycle issues.

### Q: Почему stagger по столбцам, а не по элементам?

A: Визуально приятнее — столбцы "волнами" падают. Если stagger по элементам, будет хаотично.

### Q: Что если элемент падает на 0 клеток?

A: FallCalculator не создаёт FallData для элементов на своих местах (y == writeIndex). Только реальные падения попадают в список.
