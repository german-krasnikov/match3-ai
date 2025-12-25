# Этап 10: Refill System - Детальный План Реализации

## Статус: В РАБОТЕ 🔄

---

## Обзор

Refill System заполняет пустые ячейки новыми элементами после падения. Когда FallHandler завершает работу, верхние ряды могут содержать "дыры" — RefillSystem создаёт новые элементы выше сетки и анимирует их падение.

### Связь с другими системами

```
FallHandler.OnFallsCompleted
         │
         ▼
┌─────────────────────────────────────┐
│  RefillHandler.ExecuteRefills()   ◄─┼── ЭТАП 10
│         │                           │
│         ▼                           │
│  RefillCalculator.CalculateRefills()│
│         │                           │
│         ▼                           │
│  ElementFactory.CreateRandom()      │
│         │                           │
│         ▼                           │
│  BoardComponent.SetElement()        │
│         │                           │
│         ▼                           │
│  RefillAnimator.AnimateRefills()    │
│         │                           │
│         ▼                           │
│  OnRefillsCompleted                 │
└─────────────────────────────────────┘
         │
         ▼
   [Этап 11: GameLoopController - Cascade Check]
```

### Зависимости

| Зависимость | Использование |
|-------------|---------------|
| `BoardComponent` | `GetEmptyPositions()` — пустые ячейки |
| `GridComponent` | `GridToWorld(pos)` — мировые координаты |
| `ElementFactory` | `CreateRandom()` — создание элементов |
| `FallHandler` | `OnFallsCompleted` — триггер для запуска |

---

## Архитектура

### Компоненты

| Компонент | Ответственность | События |
|-----------|-----------------|---------|
| `RefillCalculator` | Расчёт какие позиции нужно заполнить | — |
| `RefillHandler` | Координация создания и анимации | `OnRefillsStarted`, `OnRefillsCompleted` |
| `RefillAnimator` | DOTween анимации падения новых элементов | — |

### Принцип разделения (Unity Way)

```
RefillHandler            RefillCalculator         RefillAnimator
(координация)            (логика/данные)          (визуал)
      │                        │                       │
      │  1. CalculateRefills() │                       │
      ├───────────────────────►│                       │
      │                        │                       │
      │◄───────────────────────┤ List<RefillData>      │
      │                        │                       │
      │  2. Create Elements    │                       │
      │     via ElementFactory │                       │
      │                        │                       │
      │  3. Update Board       │                       │
      │                        │                       │
      ├────────────────────────────────────────────────►│ 4. AnimateRefills()
      │                        │                       │
      │◄───────────────────────────────────────────────┤ 5. OnComplete
      │                        │                       │
      │  6. Fire OnRefillsCompleted                    │
      ▼                        ▼                       ▼
```

---

## Алгоритм заполнения

### Визуализация

```
После падения:              Spawn позиции:           После Refill:

y=5:                        ● ● ●                    (spawn row)
y=4: _ _ _ _ _              ● ● ● ● ●               y=4: Y B R G P
y=3: G _ _ P R              ↓ ↓ ↓ ↓ ↓               y=3: G R G P R
y=2: B _ R Y G                                      y=2: B Y R Y G
y=1: P Y R G B                                      y=1: P Y R G B
y=0: R B G Y P                                      y=0: R B G Y P
     0 1 2 3 4                                           0 1 2 3 4
```

### Алгоритм

```
1. Найти все пустые позиции на доске
2. Для каждой пустой позиции:
   - Определить тип нового элемента (Random)
   - Вычислить spawn позицию (выше сетки)
   - Создать RefillData
3. Создать элементы через ElementFactory
4. Установить элементы в spawn позиции
5. Обновить Board state
6. Анимировать падение
```

### Расчёт spawn позиции

Для столбца с N пустых ячеек, spawn позиции располагаются выше сетки:

```
Столбец 1: 2 пустые ячейки (y=3, y=4)

Grid height = 5 (y: 0-4)
Empty: y=3, y=4

Spawn positions:
- Element для y=4 → spawn at y=5 (Height + 0)
- Element для y=3 → spawn at y=6 (Height + 1)

Общая формула:
spawn_y = grid.Height + (index_in_column)

Где index_in_column = порядковый номер пустой ячейки сверху вниз в столбце
```

---

## Файлы для создания

```
Assets/Scripts/Refill/
├── RefillData.cs           # Struct данных о заполнении
├── RefillCalculator.cs     # Расчёт заполнений
├── RefillHandler.cs        # Координация
└── RefillAnimator.cs       # Анимации

Assets/Scripts/Editor/
└── RefillSystemSetup.cs    # Editor setup
```

---

## 10.1 RefillData.cs

### Назначение

Readonly struct с информацией о новом элементе для заполнения.

### Код

```csharp
using UnityEngine;

namespace Match3.Refill
{
    /// <summary>
    /// Data about a single element to be spawned and dropped.
    /// </summary>
    public readonly struct RefillData
    {
        /// <summary>Target position on the grid where element should land.</summary>
        public Vector2Int TargetPosition { get; }

        /// <summary>Spawn position above the grid (in grid coordinates).</summary>
        public Vector2Int SpawnPosition { get; }

        /// <summary>World position where element spawns.</summary>
        public Vector3 SpawnWorldPosition { get; }

        /// <summary>World position where element lands.</summary>
        public Vector3 TargetWorldPosition { get; }

        /// <summary>Distance to fall (in cells).</summary>
        public int FallDistance { get; }

        public RefillData(
            Vector2Int targetPosition,
            Vector2Int spawnPosition,
            Vector3 spawnWorldPosition,
            Vector3 targetWorldPosition)
        {
            TargetPosition = targetPosition;
            SpawnPosition = spawnPosition;
            SpawnWorldPosition = spawnWorldPosition;
            TargetWorldPosition = targetWorldPosition;
            FallDistance = spawnPosition.y - targetPosition.y;
        }

        public override string ToString()
            => $"Refill: spawn {SpawnPosition} → target {TargetPosition} (dist={FallDistance})";
    }
}
```

---

## 10.2 RefillCalculator.cs

### Назначение

Статический класс для расчёта заполнений. Определяет какие позиции пусты и где спаунить новые элементы.

### Код

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;

namespace Match3.Refill
{
    /// <summary>
    /// Calculates refill data for empty positions on the board.
    /// </summary>
    public static class RefillCalculator
    {
        private static readonly List<RefillData> _refillsBuffer = new(64);
        private static readonly Dictionary<int, int> _columnCounters = new(8);

        /// <summary>
        /// Calculates all refills needed for current board state.
        /// </summary>
        public static List<RefillData> CalculateRefills(BoardComponent board, GridComponent grid)
        {
            _refillsBuffer.Clear();
            _columnCounters.Clear();

            // Scan from top to bottom (spawn order matters for stagger)
            for (int y = board.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var pos = new Vector2Int(x, y);

                    if (board.IsEmpty(pos))
                    {
                        var refillData = CreateRefillData(pos, x, grid, board.Height);
                        _refillsBuffer.Add(refillData);
                    }
                }
            }

            return new List<RefillData>(_refillsBuffer);
        }

        private static RefillData CreateRefillData(
            Vector2Int targetPos,
            int column,
            GridComponent grid,
            int gridHeight)
        {
            // Get spawn index for this column (how many already spawned)
            if (!_columnCounters.TryGetValue(column, out int spawnIndex))
                spawnIndex = 0;

            _columnCounters[column] = spawnIndex + 1;

            // Spawn position is above grid
            var spawnPos = new Vector2Int(column, gridHeight + spawnIndex);

            // Calculate world positions
            var spawnWorldPos = grid.GridToWorld(spawnPos);
            var targetWorldPos = grid.GridToWorld(targetPos);

            return new RefillData(targetPos, spawnPos, spawnWorldPos, targetWorldPos);
        }
    }
}
```

### Визуализация алгоритма

```
Board после Fall:
y=4: _ _ _    (пусто)
y=3: G _ _    (1 и 2 пусты)
y=2: B Y R
y=1: P Y R
y=0: R B G
     0 1 2

Сканируем top-to-bottom:
y=4: x=0 пусто → spawn(0,5), x=1 пусто → spawn(1,5), x=2 пусто → spawn(2,5)
y=3: x=0 занят, x=1 пусто → spawn(1,6), x=2 пусто → spawn(2,6)

RefillData:
1. target(0,4) ← spawn(0,5)
2. target(1,4) ← spawn(1,5)
3. target(2,4) ← spawn(2,5)
4. target(1,3) ← spawn(1,6)
5. target(2,3) ← spawn(2,6)
```

---

## 10.3 RefillHandler.cs

### Назначение

Координирует процесс заполнения: вызывает расчёт, создаёт элементы, обновляет Board, запускает анимации.

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Spawn;
using Match3.Elements;

namespace Match3.Refill
{
    /// <summary>
    /// Handles the refill process: calculates, creates elements, animates.
    /// </summary>
    public class RefillHandler : MonoBehaviour
    {
        public event Action OnRefillsStarted;
        public event Action OnRefillsCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactory _factory;
        [SerializeField] private RefillAnimator _animator;

        private List<RefillData> _currentRefills;
        private List<ElementComponent> _createdElements;

        public bool IsRefilling { get; private set; }

        /// <summary>
        /// Calculates and executes refills for empty positions.
        /// </summary>
        public void ExecuteRefills()
        {
            if (IsRefilling) return;

            _currentRefills = RefillCalculator.CalculateRefills(_board, _grid);

            if (_currentRefills.Count == 0)
            {
                OnRefillsCompleted?.Invoke();
                return;
            }

            IsRefilling = true;
            OnRefillsStarted?.Invoke();

            CreateElements();
            UpdateBoardState();
            AnimateRefills();
        }

        private void CreateElements()
        {
            _createdElements = new List<ElementComponent>(_currentRefills.Count);

            foreach (var refill in _currentRefills)
            {
                var element = _factory.CreateRandom(
                    refill.SpawnWorldPosition,
                    refill.TargetPosition
                );
                _createdElements.Add(element);
            }
        }

        private void UpdateBoardState()
        {
            for (int i = 0; i < _currentRefills.Count; i++)
            {
                var refill = _currentRefills[i];
                var element = _createdElements[i];
                _board.SetElement(refill.TargetPosition, element);
            }
        }

        private void AnimateRefills()
        {
            _animator.AnimateRefills(_currentRefills, _createdElements, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            IsRefilling = false;
            _currentRefills = null;
            _createdElements = null;
            OnRefillsCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Execute Refills")]
        private void TestExecuteRefills()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[RefillHandler] Only works in Play Mode");
                return;
            }

            var refills = RefillCalculator.CalculateRefills(_board, _grid);
            Debug.Log($"[RefillHandler] Calculated {refills.Count} refills:");
            foreach (var refill in refills)
            {
                Debug.Log($"  {refill}");
            }

            ExecuteRefills();
        }
#endif
    }
}
```

---

## 10.4 RefillAnimator.cs

### Назначение

Анимирует падение новых элементов. Использует тот же стиль что FallAnimator — InQuad + Bounce.

### Анимация

```
                              Spawn
                                ●
                                │
                                ▼  (fall with InQuad)
                                ●
                                │
                             ──┴── (bounce on landing)
                              Grid
```

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Refill
{
    /// <summary>
    /// Animates new elements falling from above the grid.
    /// </summary>
    public class RefillAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _fallSpeed = 12f;
        [SerializeField] private float _minFallDuration = 0.1f;
        [SerializeField] private float _maxFallDuration = 0.6f;
        [SerializeField] private float _staggerDelay = 0.03f;

        [Header("Effects")]
        [SerializeField] private Ease _fallEase = Ease.InQuad;
        [SerializeField] private float _bounceStrength = 0.15f;
        [SerializeField] private float _bounceDuration = 0.15f;

        [Header("Spawn Effect")]
        [SerializeField] private float _spawnScale = 0.5f;
        [SerializeField] private float _scaleUpDuration = 0.1f;

        private Sequence _currentSequence;

        public void AnimateRefills(
            List<RefillData> refills,
            List<ElementComponent> elements,
            Action onComplete)
        {
            KillCurrentAnimation();

            if (refills == null || refills.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            // Group by column for stagger
            var columnDelays = new Dictionary<int, float>();
            float maxDelay = 0f;

            for (int i = 0; i < refills.Count; i++)
            {
                var refill = refills[i];
                var element = elements[i];

                if (element == null) continue;

                // Calculate delay based on column
                int column = refill.TargetPosition.x;
                if (!columnDelays.TryGetValue(column, out float delay))
                {
                    delay = maxDelay;
                    columnDelays[column] = delay;
                    maxDelay += _staggerDelay;
                }
                else
                {
                    // Same column, stack delay
                    delay = columnDelays[column] + _staggerDelay;
                    columnDelays[column] = delay;
                }

                var elementSequence = CreateElementRefillSequence(refill, element);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementRefillSequence(RefillData refill, ElementComponent element)
        {
            var transform = element.transform;

            // Start with smaller scale
            transform.localScale = Vector3.one * _spawnScale;

            // Calculate duration based on distance
            float duration = refill.FallDistance / _fallSpeed;
            duration = Mathf.Clamp(duration, _minFallDuration, _maxFallDuration);

            var seq = DOTween.Sequence();

            // Scale up as it spawns
            seq.Append(transform.DOScale(Vector3.one, _scaleUpDuration).SetEase(Ease.OutBack));

            // Fall movement
            seq.Join(transform.DOMove(refill.TargetWorldPosition, duration).SetEase(_fallEase));

            // Bounce on landing
            seq.Append(transform.DOPunchScale(
                new Vector3(_bounceStrength, -_bounceStrength, 0),
                _bounceDuration,
                1,
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
| `_maxFallDuration` | 0.6f | Максимальная длительность |
| `_staggerDelay` | 0.03f | Задержка между элементами |
| `_fallEase` | InQuad | Easing падения |
| `_bounceStrength` | 0.15f | Сила bounce |
| `_bounceDuration` | 0.15f | Длительность bounce |
| `_spawnScale` | 0.5f | Начальный масштаб (эффект появления) |
| `_scaleUpDuration` | 0.1f | Время масштабирования |

### Timeline

```
Element 1 (col 0): [scale][─────fall─────][bounce]
Element 2 (col 1):      [scale][─────fall─────][bounce]
Element 3 (col 2):           [scale][─────fall─────][bounce]
Element 4 (col 1):                [scale][─────fall─────][bounce]  (second in column)
                   ├─0.03─┼─0.03─┼─0.03─┼─0.03─►  (stagger)
```

---

## 10.5 RefillSystemSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Refill;
using Match3.Grid;
using Match3.Board;
using Match3.Spawn;
using Match3.Fall;

namespace Match3.Editor
{
    public static class RefillSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 10 - Refill System")]
        public static void SetupRefillSystem()
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

            var factory = grid.GetComponent<ElementFactory>();
            if (factory == null)
            {
                Debug.LogError("[Match3] ElementFactory not found. Run Stage 3 setup first.");
                return;
            }

            var fallHandler = grid.GetComponent<FallHandler>();
            if (fallHandler == null)
            {
                Debug.LogError("[Match3] FallHandler not found. Run Stage 9 setup first.");
                return;
            }

            var go = grid.gameObject;

            // RefillAnimator
            var refillAnimator = go.GetComponent<RefillAnimator>();
            if (refillAnimator == null)
                refillAnimator = Undo.AddComponent<RefillAnimator>(go);

            // RefillHandler
            var refillHandler = go.GetComponent<RefillHandler>();
            if (refillHandler == null)
                refillHandler = Undo.AddComponent<RefillHandler>(go);

            SetField(refillHandler, "_board", board);
            SetField(refillHandler, "_grid", grid);
            SetField(refillHandler, "_factory", factory);
            SetField(refillHandler, "_animator", refillAnimator);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Refill System setup complete!");
            Debug.Log("[Match3] NOTE: SwapHandler needs to be updated to call RefillHandler.ExecuteRefills()");
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

## 10.6 Интеграция в SwapHandler

### Изменения в SwapHandler.cs

Текущий код (строка 141-144):
```csharp
private void OnFallsCompleted()
{
    // TODO: Stage 10 - RefillHandler will be called here
    FinishSwap();
}
```

Нужно заменить на:
```csharp
// Добавить using
using Match3.Refill;

// Добавить поле
[SerializeField] private RefillHandler _refillHandler;

// Изменить подписки
private void OnEnable()
{
    _inputDetector.OnSwapRequested += HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
    _fallHandler.OnFallsCompleted += OnFallsCompleted;
    _refillHandler.OnRefillsCompleted += OnRefillsCompleted;  // NEW
}

private void OnDisable()
{
    _inputDetector.OnSwapRequested -= HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
    _fallHandler.OnFallsCompleted -= OnFallsCompleted;
    _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;  // NEW
}

// Изменить OnFallsCompleted
private void OnFallsCompleted()
{
    // Stage 10: After falls, refill empty positions
    _refillHandler.ExecuteRefills();
}

// Добавить новый метод
private void OnRefillsCompleted()
{
    // TODO: Stage 11 - Check for cascade matches here
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
using Match3.Refill;

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
        [SerializeField] private RefillHandler _refillHandler;

        private bool _isProcessing;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
            _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
            _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
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
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            _refillHandler.ExecuteRefills();
        }

        private void OnRefillsCompleted()
        {
            // TODO: Stage 11 - Check for cascade matches here
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

После Stage 10 на GameManager объекте:

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
├── SwapHandler            [Stage 6] ← +_refillHandler
├── MatchFinder            [Stage 7]
├── MatchHighlighter       [Stage 7] (debug)
├── DestroyAnimator        [Stage 8]
├── DestroyHandler         [Stage 8]
├── FallAnimator           [Stage 9]
├── FallHandler            [Stage 9]
├── RefillAnimator         [Stage 10] ← NEW
└── RefillHandler          [Stage 10] ← NEW
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
FallHandler.ExecuteFalls()
      │
      ▼
FallCalculator.CalculateFalls()
      │
      ▼
FallAnimator.AnimateFalls()
      │
      ▼
OnFallsCompleted
      │
      ▼
RefillHandler.ExecuteRefills()        ◄── ЭТАП 10
      │
      ▼
RefillCalculator.CalculateRefills()
      │
      ▼
ElementFactory.CreateRandom()
      │
      ▼
BoardComponent.SetElement()
      │
      ▼
RefillAnimator.AnimateRefills()
      │
      ▼
OnRefillsCompleted
      │
      ▼
FinishSwap()   (TODO: Stage 11 - Cascade Check)
      │
      ▼
InputBlocker.Unblock()
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `RefillData.cs` | — | Compile |
| 2 | `RefillCalculator.cs` | BoardComponent, GridComponent | Debug.Log |
| 3 | `RefillAnimator.cs` | DOTween, RefillData, ElementComponent | Visual в Scene |
| 4 | `RefillHandler.cs` | All above + ElementFactory | Context menu тест |
| 5 | `RefillSystemSetup.cs` | All above | Меню создаёт компоненты |
| 6 | Update `SwapHandler.cs` | RefillHandler | Полный flow тест |

---

## Тестирование

### Тест 1: RefillCalculator

```csharp
// В RefillHandler:
[ContextMenu("Debug Calculate Refills")]
private void DebugCalculateRefills()
{
    var refills = RefillCalculator.CalculateRefills(_board, _grid);
    Debug.Log($"[RefillHandler] Calculated {refills.Count} refills:");
    foreach (var refill in refills)
    {
        Debug.Log($"  {refill}");
    }
}
```

### Тест 2: Визуальная проверка

1. Play Mode
2. Сделать свап, создающий матч
3. Наблюдать:
   - Элементы уничтожаются
   - Элементы падают вниз
   - Новые элементы появляются сверху
   - Bounce эффект при приземлении
4. Board полностью заполнен после refill

### Тест 3: Edge cases

1. **Весь столбец пустой**: 8 новых элементов должны упасть
2. **Один элемент**: Минимальный refill
3. **Много столбцов одновременно**: Stagger эффект между столбцами

### Тест 4: Анимация

1. Элементы появляются с меньшим масштабом
2. Scale up происходит одновременно с падением
3. Stagger delay создаёт волновой эффект
4. Bounce при приземлении

---

## Известные ограничения

### 1. Нет Cascade

После Refill могут образоваться новые матчи. Cascade Loop — часть GameLoop (Этап 11).

### 2. Простой Random

Новые элементы создаются полностью случайно. Возможны матчи сразу после спауна.

### 3. Нет smart spawn

Не проверяется, создаст ли новый элемент матч. Это сделано намеренно — каскады интереснее.

---

## Возможные улучшения (для будущих этапов)

| Улучшение | Сложность | Описание |
|-----------|-----------|----------|
| Anti-match spawn | Средняя | Новые элементы не создают матч сразу |
| Trail VFX | Низкая | Визуальный след за падающим элементом |
| Sound | Низкая | Звук появления / приземления |
| Particle burst | Низкая | Частицы при появлении элемента |
| Color bias | Средняя | Увеличить шанс определённых цветов |

---

## Чеклист

### Код
- [ ] Создать папку `Assets/Scripts/Refill/`
- [ ] `RefillData.cs` — readonly struct
- [ ] `RefillCalculator.cs` — статический класс расчёта
- [ ] `RefillAnimator.cs` — DOTween анимации
- [ ] `RefillHandler.cs` — координация
- [ ] `RefillSystemSetup.cs` — Editor menu

### Интеграция
- [ ] SwapHandler получает ссылку на RefillHandler
- [ ] FallHandler.OnFallsCompleted → RefillHandler.ExecuteRefills
- [ ] RefillHandler.OnRefillsCompleted → SwapHandler.OnRefillsCompleted

### Тестирование в Unity
- [ ] Меню `Match3 → Setup Scene → Stage 10 - Refill System` работает
- [ ] Новые элементы появляются после падения
- [ ] Элементы спаунятся выше сетки
- [ ] Анимация падения плавная
- [ ] Scale-up эффект при появлении
- [ ] Bounce эффект при приземлении
- [ ] Stagger delay между столбцами
- [ ] Board полностью заполнен после refill
- [ ] Input заблокирован во время refill

---

## FAQ

### Q: Почему элементы создаются через ElementFactory?

A: Переиспользование пула (ElementPool). Элементы не создаются/удаляются — берутся из пула и возвращаются в него.

### Q: Почему RefillCalculator статический?

A: Чистая функция без состояния (кроме буферов). Проще тестировать, нет lifecycle issues.

### Q: Почему scan top-to-bottom?

A: Для правильного stagger эффекта. Верхние элементы падают дольше, нужно стартовать их первыми.

### Q: Почему spawn scale начинается с 0.5?

A: Визуальный эффект "появления из ниоткуда". Элемент не просто падает — он материализуется.

### Q: Что если после refill образовался матч?

A: Это Cascade — часть GameLoop (Этап 11). Текущий этап просто заполняет доску.
