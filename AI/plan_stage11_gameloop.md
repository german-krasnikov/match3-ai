# Этап 11: Game Loop - Детальный План Реализации

## Статус: В РАЗРАБОТКЕ 🔄

---

## Обзор

Game Loop завершает основной цикл Match-3. Главные задачи:
1. **Cascade** — после refill проверять новые матчи и повторять цикл
2. **Deadlock Detection** — проверять есть ли возможные ходы
3. **Board Shuffle** — перемешивать доску при deadlock

### Текущее состояние

В `SwapHandler.cs:152` есть заглушка:
```csharp
private void OnRefillsCompleted()
{
    // TODO: Stage 11 - Check for cascade matches here
    FinishSwap();
}
```

### Архитектурное решение

**Минимальные изменения** — расширяем SwapHandler вместо создания отдельного GameLoopController:

| Подход | Плюсы | Минусы |
|--------|-------|--------|
| Расширить SwapHandler | Минимум изменений, уже работает | SwapHandler растёт |
| Новый GameLoopController | Чистый SRP | Рефакторинг, больше файлов |

**Решение:** Расширить SwapHandler cascade логикой. SwapHandler уже координирует весь поток. Отдельный GameLoopController можно создать позже при необходимости.

---

## Связь с другими системами

```
┌─────────────────────────────────────────────────────────────┐
│  RefillHandler.OnRefillsCompleted                           │
│            │                                                 │
│            ▼                                                 │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  SwapHandler.OnRefillsCompleted() ◄── ЭТАП 11      │    │
│  │            │                                         │    │
│  │            ▼                                         │    │
│  │  MatchFinder.FindAllMatches()                        │    │
│  │            │                                         │    │
│  │            ├─[matches found]──► Destroy → Fall →    │    │
│  │            │                    Refill → LOOP        │    │
│  │            │                                         │    │
│  │            ▼ [no matches]                           │    │
│  │  DeadlockChecker.HasPossibleMoves()                 │    │
│  │            │                                         │    │
│  │            ├─[has moves]──► FinishSwap()            │    │
│  │            │                                         │    │
│  │            ▼ [no moves]                             │    │
│  │  BoardShuffler.Shuffle()                            │    │
│  │            │                                         │    │
│  │            ▼                                         │    │
│  │  CHECK FOR MATCHES AGAIN                            │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

---

## Архитектура

### Новые компоненты

| Компонент | Ответственность | Тип |
|-----------|-----------------|-----|
| `GameState` | enum состояний игры | enum |
| `DeadlockChecker` | Проверка возможных ходов | static class |
| `BoardShuffler` | Перемешивание доски при deadlock | MonoBehaviour |

### Изменяемые компоненты

| Компонент | Изменения |
|-----------|-----------|
| `SwapHandler` | Cascade логика, deadlock check, shuffle |

### Принцип разделения (Unity Way)

```
SwapHandler              DeadlockChecker         BoardShuffler
(координация)            (логика проверки)       (перемешивание)
      │                        │                       │
      │  1. FindAllMatches()   │                       │
      ├───────────────────────►│                       │
      │                        │                       │
      │◄───────────────────────┤ [no matches]          │
      │                        │                       │
      │  2. HasPossibleMoves() │                       │
      ├───────────────────────►│                       │
      │                        │                       │
      │◄───────────────────────┤ [no moves]            │
      │                        │                       │
      ├────────────────────────────────────────────────►│ 3. Shuffle()
      │                        │                       │
      │◄───────────────────────────────────────────────┤ OnShuffleComplete
      ▼                        ▼                       ▼
```

---

## Алгоритм Cascade

### Визуализация

```
После Refill:                После Cascade Check:
                             (новые матчи найдены!)
y=4: [Y][B][R][G][P]
y=3: [G][R][R][R][R] ←match! → Destroy → Fall → Refill → Check...
y=2: [B][Y][R][Y][G]
y=1: [P][Y][R][G][B]
y=0: [R][B][G][Y][P]

Цикл продолжается пока есть матчи!
```

### Псевдокод

```
OnRefillsCompleted():
    matches = FindAllMatches()

    if matches.Count > 0:
        DestroyMatches(matches)  # → Fall → Refill → OnRefillsCompleted (LOOP)
        return

    # Нет матчей — проверяем deadlock
    if not HasPossibleMoves():
        Shuffle()
        return

    # Всё ок — завершаем ход
    FinishSwap()
```

---

## Алгоритм DeadlockChecker

### Принцип

Проверить все возможные свапы (соседние пары). Если хотя бы один создаёт матч — есть ход.

### Визуализация

```
Для каждой ячейки проверяем swap с правым и верхним соседом:

[A]─[B]   Swap(A,B) создаёт матч?
 │
[C]       Swap(A,C) создаёт матч?

Если хоть один — есть возможный ход.
```

### Оптимизация

Не нужно проверять все свапы. Достаточно найти первый возможный:

```csharp
for (x = 0; x < width; x++)
    for (y = 0; y < height; y++)
        if (WouldCreateMatch(pos, right) || WouldCreateMatch(pos, up))
            return true;  // Early exit
return false;
```

---

## Алгоритм BoardShuffler

### Принцип

1. Собрать все элементы в список
2. Перемешать список (Fisher-Yates)
3. Распределить обратно по позициям
4. Анимировать перемещение
5. Проверить deadlock снова (рекурсивно)

### Визуализация

```
До shuffle:              После shuffle:
[R][R][G][G]            [G][R][Y][B]
[R][R][G][G]     →      [R][G][R][G]
[B][B][Y][Y]            [Y][B][G][R]
[B][B][Y][Y]            [B][Y][B][Y]

Deadlock!               Есть возможные ходы!
```

### Edge case

После shuffle может снова быть deadlock (маловероятно, но возможно). Решение: цикл shuffle пока не появятся ходы.

---

## Файлы для создания

```
Assets/Scripts/GameLoop/
├── GameState.cs           # enum состояний
├── DeadlockChecker.cs     # Проверка возможных ходов
└── BoardShuffler.cs       # Перемешивание доски

Assets/Scripts/Editor/
└── GameLoopSetup.cs       # Editor setup
```

### Изменяемые файлы

```
Assets/Scripts/Swap/SwapHandler.cs  # Cascade + Deadlock integration
```

---

## 11.1 GameState.cs

### Назначение

Enum для отслеживания состояния игры. Используется для отладки и событий.

### Код

```csharp
namespace Match3.GameLoop
{
    /// <summary>
    /// Game loop states for debugging and events.
    /// </summary>
    public enum GameState
    {
        Idle,           // Ожидание ввода
        Swapping,       // Анимация свапа
        Matching,       // Поиск матчей
        Destroying,     // Анимация уничтожения
        Falling,        // Анимация падения
        Refilling,      // Анимация заполнения
        CheckingCascade,// Проверка каскада
        Shuffling       // Перемешивание доски
    }
}
```

---

## 11.2 DeadlockChecker.cs

### Назначение

Статический класс для проверки есть ли возможные ходы на доске.

### Код

```csharp
using UnityEngine;
using Match3.Board;
using Match3.Matching;

namespace Match3.GameLoop
{
    /// <summary>
    /// Checks if any valid moves exist on the board.
    /// </summary>
    public static class DeadlockChecker
    {
        /// <summary>
        /// Returns true if at least one valid swap exists.
        /// </summary>
        public static bool HasPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            int width = board.Width;
            int height = board.Height;

            // Check horizontal swaps (with right neighbor)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x + 1, y);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        return true;
                }
            }

            // Check vertical swaps (with top neighbor)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x, y + 1);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        return true;
                }
            }

            return false;
        }

        private static bool WouldSwapCreateMatch(
            BoardComponent board,
            MatchFinder matchFinder,
            Vector2Int posA,
            Vector2Int posB)
        {
            var elementA = board.GetElement(posA);
            var elementB = board.GetElement(posB);

            if (elementA == null || elementB == null)
                return false;

            // Temporarily swap
            board.SwapElements(posA, posB);

            // Check for matches
            bool hasMatch = matchFinder.WouldCreateMatch(posA, posB);

            // Swap back
            board.SwapElements(posA, posB);

            return hasMatch;
        }

        /// <summary>
        /// Returns count of possible moves (for hints).
        /// </summary>
        public static int CountPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            int count = 0;
            int width = board.Width;
            int height = board.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x + 1, y);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        count++;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    var posA = new Vector2Int(x, y);
                    var posB = new Vector2Int(x, y + 1);

                    if (WouldSwapCreateMatch(board, matchFinder, posA, posB))
                        count++;
                }
            }

            return count;
        }
    }
}
```

### Сложность

- **Time:** O(W × H) — проверяем каждую ячейку с 2 соседями
- **Space:** O(1) — не создаём дополнительных структур
- **Early exit:** возвращаем true как только найден первый ход

---

## 11.3 BoardShuffler.cs

### Назначение

Перемешивает элементы на доске когда нет возможных ходов.

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Board;
using Match3.Grid;
using Match3.Elements;

namespace Match3.GameLoop
{
    /// <summary>
    /// Shuffles board elements when no moves are available.
    /// </summary>
    public class BoardShuffler : MonoBehaviour
    {
        public event Action OnShuffleStarted;
        public event Action OnShuffleCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;

        [Header("Animation")]
        [SerializeField] private float _shuffleDuration = 0.4f;
        [SerializeField] private Ease _shuffleEase = Ease.InOutQuad;
        [SerializeField] private float _staggerDelay = 0.02f;

        private readonly List<ElementComponent> _elementsBuffer = new();
        private readonly List<Vector2Int> _positionsBuffer = new();

        public bool IsShuffling { get; private set; }

        /// <summary>
        /// Shuffles all elements on the board with animation.
        /// </summary>
        public void Shuffle()
        {
            if (IsShuffling) return;

            IsShuffling = true;
            OnShuffleStarted?.Invoke();

            CollectElements();
            ShufflePositions();
            UpdateBoard();
            AnimateShuffle();
        }

        private void CollectElements()
        {
            _elementsBuffer.Clear();
            _positionsBuffer.Clear();

            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var element = _board.GetElement(pos);

                    if (element != null)
                    {
                        _elementsBuffer.Add(element);
                        _positionsBuffer.Add(pos);
                    }
                }
            }
        }

        private void ShufflePositions()
        {
            // Fisher-Yates shuffle
            for (int i = _positionsBuffer.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_positionsBuffer[i], _positionsBuffer[j]) =
                    (_positionsBuffer[j], _positionsBuffer[i]);
            }
        }

        private void UpdateBoard()
        {
            // Clear board
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                    _board.SetElement(new Vector2Int(x, y), null);

            // Set elements at new positions
            for (int i = 0; i < _elementsBuffer.Count; i++)
            {
                var element = _elementsBuffer[i];
                var newPos = _positionsBuffer[i];
                _board.SetElement(newPos, element);
            }
        }

        private void AnimateShuffle()
        {
            var sequence = DOTween.Sequence();

            for (int i = 0; i < _elementsBuffer.Count; i++)
            {
                var element = _elementsBuffer[i];
                var newPos = _positionsBuffer[i];
                var worldPos = _grid.GridToWorld(newPos);

                float delay = i * _staggerDelay;

                sequence.Insert(delay,
                    element.transform.DOMove(worldPos, _shuffleDuration)
                        .SetEase(_shuffleEase));
            }

            sequence.OnComplete(OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            _elementsBuffer.Clear();
            _positionsBuffer.Clear();
            IsShuffling = false;
            OnShuffleCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Shuffle")]
        private void TestShuffle()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[BoardShuffler] Only works in Play Mode");
                return;
            }

            Debug.Log("[BoardShuffler] Starting shuffle...");
            Shuffle();
        }
#endif
    }
}
```

### Параметры анимации

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_shuffleDuration` | 0.4f | Длительность перемещения |
| `_shuffleEase` | InOutQuad | Easing анимации |
| `_staggerDelay` | 0.02f | Задержка между элементами |

---

## 11.4 Изменения в SwapHandler.cs

### Текущий код (строка 150-154)

```csharp
private void OnRefillsCompleted()
{
    // TODO: Stage 11 - Check for cascade matches here
    FinishSwap();
}
```

### Новый код

```csharp
// Добавить using
using Match3.GameLoop;

// Добавить поле
[SerializeField] private BoardShuffler _boardShuffler;

// Добавить подписку в OnEnable/OnDisable
private void OnEnable()
{
    _inputDetector.OnSwapRequested += HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
    _fallHandler.OnFallsCompleted += OnFallsCompleted;
    _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
    _boardShuffler.OnShuffleCompleted += OnShuffleCompleted;  // NEW
}

private void OnDisable()
{
    _inputDetector.OnSwapRequested -= HandleSwapRequest;
    _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
    _fallHandler.OnFallsCompleted -= OnFallsCompleted;
    _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
    _boardShuffler.OnShuffleCompleted -= OnShuffleCompleted;  // NEW
}

// Изменить OnRefillsCompleted
private void OnRefillsCompleted()
{
    // Cascade: check for new matches after refill
    var matches = _matchFinder.FindAllMatches();

    if (matches.Count > 0)
    {
        // Continue cascade
        _destroyHandler.DestroyMatches(matches);
        return;
    }

    // No matches - check for deadlock
    CheckDeadlock();
}

// Добавить методы
private void CheckDeadlock()
{
    if (DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
    {
        FinishSwap();
        return;
    }

    // Deadlock! Shuffle the board
    Debug.Log("[SwapHandler] Deadlock detected! Shuffling board...");
    _boardShuffler.Shuffle();
}

private void OnShuffleCompleted()
{
    // After shuffle, check for auto-matches
    var matches = _matchFinder.FindAllMatches();

    if (matches.Count > 0)
    {
        // Matches created by shuffle - process them
        _destroyHandler.DestroyMatches(matches);
        return;
    }

    // Check deadlock again (extremely rare, but possible)
    if (!DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
    {
        Debug.LogWarning("[SwapHandler] Still deadlocked after shuffle! Shuffling again...");
        _boardShuffler.Shuffle();
        return;
    }

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
using Match3.GameLoop;

namespace Match3.Swap
{
    public class SwapHandler : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapStarted;
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
        public event Action<Vector2Int, Vector2Int> OnSwapReverted;
        public event Action OnCascadeStarted;
        public event Action<int> OnCascadeCompleted; // total destroyed count

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
        [SerializeField] private BoardShuffler _boardShuffler;

        private bool _isProcessing;
        private int _cascadeDestroyedCount;
        private int _cascadeLevel;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
            _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted += OnShuffleCompleted;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
            _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted -= OnShuffleCompleted;
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
            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;

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
                OnCascadeStarted?.Invoke();
                _destroyHandler.DestroyMatches(matches);
            }
            else
            {
                FinishSwap();
            }
        }

        private void OnDestroyCompleted(int count)
        {
            _cascadeDestroyedCount += count;
            _cascadeLevel++;
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            _refillHandler.ExecuteRefills();
        }

        private void OnRefillsCompleted()
        {
            // Cascade: check for new matches after refill
            var matches = _matchFinder.FindAllMatches();

            if (matches.Count > 0)
            {
                // Continue cascade
                _destroyHandler.DestroyMatches(matches);
                return;
            }

            // No matches - check for deadlock
            CheckDeadlock();
        }

        private void CheckDeadlock()
        {
            if (DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                FinishSwapWithCascade();
                return;
            }

            // Deadlock! Shuffle the board
            Debug.Log("[SwapHandler] Deadlock detected! Shuffling board...");
            _boardShuffler.Shuffle();
        }

        private void OnShuffleCompleted()
        {
            // After shuffle, check for auto-matches
            var matches = _matchFinder.FindAllMatches();

            if (matches.Count > 0)
            {
                // Matches created by shuffle - process them
                _destroyHandler.DestroyMatches(matches);
                return;
            }

            // Check deadlock again (extremely rare, but possible)
            if (!DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                Debug.LogWarning("[SwapHandler] Still deadlocked! Shuffling again...");
                _boardShuffler.Shuffle();
                return;
            }

            FinishSwapWithCascade();
        }

        private void FinishSwapWithCascade()
        {
            if (_cascadeLevel > 0)
            {
                OnCascadeCompleted?.Invoke(_cascadeDestroyedCount);
            }
            FinishSwap();
        }

        private void FinishSwap()
        {
            _isProcessing = false;
            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;
            _inputBlocker.Unblock();
        }

        private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
        {
            return _matchFinder.WouldCreateMatch(posA, posB);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Check Deadlock")]
        private void DebugCheckDeadlock()
        {
            bool hasMoves = DeadlockChecker.HasPossibleMoves(_board, _matchFinder);
            int count = DeadlockChecker.CountPossibleMoves(_board, _matchFinder);
            Debug.Log($"[SwapHandler] HasMoves: {hasMoves}, Count: {count}");
        }
#endif
    }
}
```

---

## 11.5 GameLoopSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.GameLoop;
using Match3.Grid;
using Match3.Board;
using Match3.Swap;
using Match3.Matching;

namespace Match3.Editor
{
    public static class GameLoopSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 11 - Game Loop")]
        public static void SetupGameLoop()
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

            var swapHandler = grid.GetComponent<SwapHandler>();
            if (swapHandler == null)
            {
                Debug.LogError("[Match3] SwapHandler not found. Run Stage 6 setup first.");
                return;
            }

            var go = grid.gameObject;

            // BoardShuffler
            var shuffler = go.GetComponent<BoardShuffler>();
            if (shuffler == null)
                shuffler = Undo.AddComponent<BoardShuffler>(go);

            SetField(shuffler, "_board", board);
            SetField(shuffler, "_grid", grid);

            // Update SwapHandler reference
            SetField(swapHandler, "_boardShuffler", shuffler);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Game Loop setup complete!");
            Debug.Log("[Match3] Cascade and Deadlock detection are now active.");
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

## Диаграмма компонентов

После Stage 11:

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
├── SwapHandler            [Stage 6] ← UPDATED (cascade, deadlock)
├── MatchFinder            [Stage 7]
├── MatchHighlighter       [Stage 7] (debug)
├── DestroyAnimator        [Stage 8]
├── DestroyHandler         [Stage 8]
├── FallAnimator           [Stage 9]
├── FallHandler            [Stage 9]
├── RefillAnimator         [Stage 10]
├── RefillHandler          [Stage 10]
└── BoardShuffler          [Stage 11] ← NEW
```

---

## Поток данных (полный с cascade)

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
┌─────────────────────────────────────────────────────┐
│                   CASCADE LOOP                       │
│                                                      │
│  DestroyHandler.DestroyMatches()                    │
│        │                                             │
│        ▼                                             │
│  DestroyAnimator.AnimateDestroy()                   │
│        │                                             │
│        ▼                                             │
│  FallHandler.ExecuteFalls()                         │
│        │                                             │
│        ▼                                             │
│  FallAnimator.AnimateFalls()                        │
│        │                                             │
│        ▼                                             │
│  RefillHandler.ExecuteRefills()                     │
│        │                                             │
│        ▼                                             │
│  RefillAnimator.AnimateRefills()                    │
│        │                                             │
│        ▼                                             │
│  OnRefillsCompleted                                  │
│        │                                             │
│        ▼                                             │
│  MatchFinder.FindAllMatches()                        │
│        │                                             │
│        ├─[matches found]──► LOOP BACK TO DESTROY ───┤
│        │                                             │
│        ▼ [no matches]                               │
└────────┼─────────────────────────────────────────────┘
         │
         ▼
DeadlockChecker.HasPossibleMoves()
         │
         ├─[has moves]──► FinishSwap()
         │
         ▼ [no moves]
BoardShuffler.Shuffle()
         │
         ▼
OnShuffleCompleted
         │
         ├─[has matches]──► ENTER CASCADE LOOP
         │
         ├─[still deadlock]──► Shuffle again
         │
         ▼
FinishSwap()
         │
         ▼
InputBlocker.Unblock()
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `GameState.cs` | — | Compile |
| 2 | `DeadlockChecker.cs` | BoardComponent, MatchFinder | ContextMenu |
| 3 | `BoardShuffler.cs` | BoardComponent, GridComponent, DOTween | ContextMenu |
| 4 | `GameLoopSetup.cs` | All above | Editor menu |
| 5 | Update `SwapHandler.cs` | All above | Full flow test |

---

## Тестирование

### Тест 1: Cascade

1. Play Mode
2. Создать ситуацию где после матча падение создаёт новый матч
3. Наблюдать автоматический каскад

**Как создать cascade:**
```
Исходное:     После матча:    После падения:
[R][R][R]     [_][_][_]       [B][G][Y]     ← новый
[B][G][Y]     [B][G][Y]       [G][G][G]     ← МАТЧ!
[G][G][G]     [G][G][G]       [P][R][B]
[P][R][B]     [P][R][B]
```

### Тест 2: DeadlockChecker

```csharp
[ContextMenu("Debug Check Deadlock")]
private void DebugCheckDeadlock()
{
    bool hasMoves = DeadlockChecker.HasPossibleMoves(_board, _matchFinder);
    int count = DeadlockChecker.CountPossibleMoves(_board, _matchFinder);
    Debug.Log($"HasMoves: {hasMoves}, Count: {count}");
}
```

### Тест 3: BoardShuffler

1. Play Mode
2. Context Menu: "Test Shuffle" на BoardShuffler
3. Наблюдать анимацию перемешивания
4. Элементы должны переместиться на новые позиции

### Тест 4: Deadlock → Shuffle

Для тестирования нужно создать искусственный deadlock:

```csharp
#if UNITY_EDITOR
[ContextMenu("Force Deadlock")]
private void ForceDeadlock()
{
    // Заполнить доску шахматным паттерном без возможных ходов
    // R B R B
    // B R B R
    // R B R B
    // ...
}
#endif
```

### Тест 5: Edge cases

1. **Множественный cascade**: 3+ уровня каскада
2. **Shuffle создаёт матч**: матч обрабатывается автоматически
3. **Double deadlock**: после shuffle снова deadlock (очень редко)

---

## События для отладки

SwapHandler предоставляет события для UI и отладки:

| Событие | Когда вызывается | Использование |
|---------|------------------|---------------|
| `OnSwapStarted` | Начало свапа | UI: показать swap |
| `OnSwapCompleted` | Свап успешен | UI: score popup |
| `OnSwapReverted` | Свап отменён | UI: shake/error |
| `OnCascadeStarted` | Начало каскада | UI: combo start |
| `OnCascadeCompleted(count)` | Конец каскада | UI: combo result |

---

## Известные ограничения

### 1. Нет комбо множителя

Текущая реализация считает общее количество уничтоженных элементов, но не применяет множитель за каскад. Scoring — отдельный этап.

### 2. Нет подсказок

DeadlockChecker может найти возможные ходы, но hint система не реализована.

### 3. Простой shuffle

Fisher-Yates shuffle не гарантирует отсутствие начальных матчей после перемешивания. Матчи обрабатываются автоматически.

---

## Возможные улучшения

| Улучшение | Сложность | Описание |
|-----------|-----------|----------|
| Combo multiplier | Низкая | cascade level → score multiplier |
| Hint system | Средняя | Подсветка возможного хода |
| Smart shuffle | Высокая | Shuffle без начальных матчей |
| Shuffle VFX | Низкая | Particles во время shuffle |
| Shuffle sound | Низкая | Звук перемешивания |

---

## Чеклист

### Код
- [ ] Создать папку `Assets/Scripts/GameLoop/`
- [ ] `GameState.cs` — enum
- [ ] `DeadlockChecker.cs` — статический класс
- [ ] `BoardShuffler.cs` — MonoBehaviour
- [ ] `GameLoopSetup.cs` — Editor menu
- [ ] Обновить `SwapHandler.cs` — cascade + deadlock

### Тестирование в Unity
- [ ] Меню `Match3 → Setup Scene → Stage 11 - Game Loop` работает
- [ ] Cascade срабатывает автоматически
- [ ] Каскад может быть многоуровневым
- [ ] DeadlockChecker правильно определяет deadlock
- [ ] BoardShuffler перемешивает элементы
- [ ] После shuffle проверяются матчи
- [ ] После shuffle проверяется deadlock
- [ ] Input заблокирован во время всех операций
- [ ] События cascade срабатывают

---

## FAQ

### Q: Почему не отдельный GameLoopController?

A: SwapHandler уже выполняет роль координатора. Добавление отдельного контроллера усложнит архитектуру без явной пользы. При необходимости рефакторинг можно сделать позже.

### Q: Почему DeadlockChecker статический?

A: Чистая функция без состояния. Не требует MonoBehaviour lifecycle.

### Q: Что если shuffle создаёт новый deadlock?

A: Крайне маловероятно, но обработано: shuffle повторяется до появления возможных ходов.

### Q: Как тестировать deadlock?

A: Создать искусственный шахматный паттерн через Context Menu или временно уменьшить количество типов элементов до 2.

### Q: Влияет ли cascade на производительность?

A: Незначительно. Каждый уровень каскада — это полный цикл (destroy → fall → refill), но это происходит последовательно с анимациями.
