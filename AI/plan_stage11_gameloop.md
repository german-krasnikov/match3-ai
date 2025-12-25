# Этап 11: Game Loop - Детальный План Реализации

## Статус: ЗАВЕРШЁН ✅

---

## Обзор

Game Loop завершает основной цикл Match-3. Главные задачи:
1. **Cascade** — после refill проверять новые матчи и повторять цикл
2. **Deadlock Detection** — проверять есть ли возможные ходы
3. **Board Shuffle** — перемешивать доску при deadlock
4. **State Machine** — отслеживание состояния игры

### Архитектурное решение

**Выбран правильный подход** — отдельный GameLoopController для соблюдения SRP:

| Компонент | Ответственность |
|-----------|-----------------|
| `SwapHandler` | Только swap: валидация, анимация, revert |
| `GameLoopController` | Координация: cascade, deadlock, state machine |

**Преимущества:**
- Чистое разделение ответственности
- SwapHandler остаётся простым (~100 строк)
- GameLoopController легко расширять (scoring, combos)

---

## Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────────────┐
│                      GameLoopController                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Подписки:                                                 │   │
│  │   SwapHandler.OnSwapStarted/Completed/Reverted           │   │
│  │   DestroyHandler.OnDestroyCompleted                       │   │
│  │   FallHandler.OnFallsCompleted                            │   │
│  │   RefillHandler.OnRefillsCompleted                        │   │
│  │   BoardShuffler.OnShuffleCompleted                        │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ State Machine:                                            │   │
│  │   Idle → Swapping → Matching → Destroying →              │   │
│  │   Falling → Refilling → CheckingCascade → Idle           │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ События:                                                  │   │
│  │   OnStateChanged(GameState)                               │   │
│  │   OnCascadeStarted                                        │   │
│  │   OnCascadeCompleted(totalDestroyed, cascadeLevel)       │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Поток данных

```
User Swap Input
      │
      ▼
SwapHandler.HandleSwapRequest()
      │
      ├─[invalid]──► return
      │
      ▼
SwapHandler.OnSwapStarted ────────────────────┐
      │                                        │
      ▼                                        ▼
SwapAnimator.AnimateSwap()          GameLoopController.OnSwapStarted()
      │                                   │
      ▼                                   ▼
BoardComponent.SwapElements()        InputBlocker.Block()
      │                              SetState(Swapping)
      │
      ├─[no match]──► AnimateRevert() ──► OnSwapReverted ──► FinishTurn()
      │
      ▼
SwapHandler.OnSwapCompleted ──────────────────┐
                                               │
                                               ▼
                               GameLoopController.OnSwapCompleted()
                                               │
                                               ▼
                               ┌───────────────────────────────────┐
                               │         CASCADE LOOP              │
                               │                                   │
                               │  ProcessMatches()                 │
                               │        │                          │
                               │        ▼                          │
                               │  MatchFinder.FindAllMatches()     │
                               │        │                          │
                               │        ├─[matches]──► Destroy ───┐│
                               │        │                         ││
                               │        ▼ [no matches]            ││
                               │  CheckDeadlock()                 ││
                               │        │                         ││
                               │        ├─[has moves]──► Finish   ││
                               │        │                         ││
                               │        ▼ [deadlock]              ││
                               │  BoardShuffler.Shuffle()         ││
                               │        │                         ││
                               │        ▼                         ││
                               │  OnShuffleCompleted()            ││
                               │        │                         ││
                               │        └─────────────────────────┘│
                               │                                   │
                               │  Destroy → Fall → Refill → LOOP  │
                               └───────────────────────────────────┘
                                               │
                                               ▼
                                         FinishTurn()
                                               │
                                               ▼
                                      InputBlocker.Unblock()
                                      SetState(Idle)
```

---

## Файлы

```
Assets/Scripts/GameLoop/
├── GameState.cs            # enum состояний
├── GameLoopController.cs   # координатор цикла
├── DeadlockChecker.cs      # проверка возможных ходов
└── BoardShuffler.cs        # перемешивание доски

Assets/Scripts/Editor/
└── GameLoopSetup.cs        # Editor menu

Assets/Scripts/Swap/
└── SwapHandler.cs          # упрощён, только swap логика
```

---

## 11.1 GameState.cs

### Назначение

Enum для отслеживания состояния игры.

### Код

```csharp
namespace Match3.GameLoop
{
    public enum GameState
    {
        Idle,            // Ожидание ввода
        Swapping,        // Анимация свапа
        Matching,        // Поиск матчей
        Destroying,      // Анимация уничтожения
        Falling,         // Анимация падения
        Refilling,       // Анимация заполнения
        CheckingCascade, // Проверка каскада
        Shuffling        // Перемешивание доски
    }
}
```

---

## 11.2 GameLoopController.cs

### Назначение

Центральный координатор игрового цикла. Подписывается на события всех систем и управляет переходами между состояниями.

### Ответственности

1. **State Machine** — отслеживание текущего состояния
2. **Cascade Loop** — повторная проверка матчей после refill
3. **Deadlock Detection** — проверка возможных ходов
4. **Input Blocking** — блокировка ввода во время обработки

### Код

```csharp
using System;
using UnityEngine;
using Match3.Board;
using Match3.Input;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;
using Match3.Swap;

namespace Match3.GameLoop
{
    public class GameLoopController : MonoBehaviour
    {
        public event Action<GameState> OnStateChanged;
        public event Action OnCascadeStarted;
        public event Action<int, int> OnCascadeCompleted; // totalDestroyed, cascadeLevel

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private InputBlocker _inputBlocker;
        [SerializeField] private SwapHandler _swapHandler;
        [SerializeField] private MatchFinder _matchFinder;
        [SerializeField] private DestroyHandler _destroyHandler;
        [SerializeField] private FallHandler _fallHandler;
        [SerializeField] private RefillHandler _refillHandler;
        [SerializeField] private BoardShuffler _boardShuffler;

        private GameState _currentState = GameState.Idle;
        private int _cascadeDestroyedCount;
        private int _cascadeLevel;

        public GameState CurrentState => _currentState;

        private void OnEnable()
        {
            _swapHandler.OnSwapStarted += OnSwapStarted;
            _swapHandler.OnSwapCompleted += OnSwapCompleted;
            _swapHandler.OnSwapReverted += OnSwapReverted;
            _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
            _fallHandler.OnFallsCompleted += OnFallsCompleted;
            _refillHandler.OnRefillsCompleted += OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted += OnShuffleCompleted;
        }

        private void OnDisable()
        {
            _swapHandler.OnSwapStarted -= OnSwapStarted;
            _swapHandler.OnSwapCompleted -= OnSwapCompleted;
            _swapHandler.OnSwapReverted -= OnSwapReverted;
            _destroyHandler.OnDestroyCompleted -= OnDestroyCompleted;
            _fallHandler.OnFallsCompleted -= OnFallsCompleted;
            _refillHandler.OnRefillsCompleted -= OnRefillsCompleted;
            _boardShuffler.OnShuffleCompleted -= OnShuffleCompleted;
        }

        private void SetState(GameState state)
        {
            if (_currentState == state) return;
            _currentState = state;
            OnStateChanged?.Invoke(state);
        }

        private void OnSwapStarted(Vector2Int a, Vector2Int b)
        {
            _inputBlocker.Block();
            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;
            SetState(GameState.Swapping);
        }

        private void OnSwapReverted(Vector2Int a, Vector2Int b)
        {
            FinishTurn();
        }

        private void OnSwapCompleted(Vector2Int a, Vector2Int b)
        {
            SetState(GameState.Matching);
            ProcessMatches();
        }

        private void ProcessMatches()
        {
            var matches = _matchFinder.FindAllMatches();

            if (matches.Count > 0)
            {
                if (_cascadeLevel == 0)
                    OnCascadeStarted?.Invoke();

                SetState(GameState.Destroying);
                _destroyHandler.DestroyMatches(matches);
            }
            else
            {
                CheckDeadlock();
            }
        }

        private void OnDestroyCompleted(int count)
        {
            _cascadeDestroyedCount += count;
            _cascadeLevel++;

            SetState(GameState.Falling);
            _fallHandler.ExecuteFalls();
        }

        private void OnFallsCompleted()
        {
            SetState(GameState.Refilling);
            _refillHandler.ExecuteRefills();
        }

        private void OnRefillsCompleted()
        {
            SetState(GameState.CheckingCascade);
            ProcessMatches(); // Cascade loop
        }

        private void CheckDeadlock()
        {
            if (DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                FinishTurn();
                return;
            }

            Debug.Log("[GameLoop] Deadlock detected! Shuffling...");
            SetState(GameState.Shuffling);
            _boardShuffler.Shuffle();
        }

        private void OnShuffleCompleted()
        {
            var matches = _matchFinder.FindAllMatches();
            if (matches.Count > 0)
            {
                SetState(GameState.Destroying);
                _destroyHandler.DestroyMatches(matches);
                return;
            }

            if (!DeadlockChecker.HasPossibleMoves(_board, _matchFinder))
            {
                Debug.LogWarning("[GameLoop] Still deadlocked! Shuffling again...");
                _boardShuffler.Shuffle();
                return;
            }

            FinishTurn();
        }

        private void FinishTurn()
        {
            if (_cascadeLevel > 0)
                OnCascadeCompleted?.Invoke(_cascadeDestroyedCount, _cascadeLevel);

            _cascadeDestroyedCount = 0;
            _cascadeLevel = 0;

            SetState(GameState.Idle);
            _inputBlocker.Unblock();
        }
    }
}
```

### События

| Событие | Параметры | Когда |
|---------|-----------|-------|
| `OnStateChanged` | `GameState` | При смене состояния |
| `OnCascadeStarted` | — | Начало каскада (первый матч) |
| `OnCascadeCompleted` | `int, int` | Конец каскада (total, level) |

---

## 11.3 DeadlockChecker.cs

### Назначение

Статический класс для проверки есть ли возможные ходы на доске.

### Алгоритм

```
Для каждой ячейки (x, y):
  1. Попробовать swap с правым соседом (x+1, y)
  2. Попробовать swap с верхним соседом (x, y+1)
  3. Если хоть один создаёт матч → return true

Если ни один swap не создаёт матч → return false (deadlock)
```

### Код

```csharp
using UnityEngine;
using Match3.Board;
using Match3.Matching;

namespace Match3.GameLoop
{
    public static class DeadlockChecker
    {
        public static bool HasPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            int width = board.Width;
            int height = board.Height;

            // Horizontal swaps
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

            // Vertical swaps
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

            board.SwapElements(posA, posB);
            bool hasMatch = matchFinder.WouldCreateMatch(posA, posB);
            board.SwapElements(posA, posB); // Swap back

            return hasMatch;
        }

        public static int CountPossibleMoves(BoardComponent board, MatchFinder matchFinder)
        {
            // Для hint system - считает все возможные ходы
            int count = 0;
            // ... аналогичная логика
            return count;
        }
    }
}
```

### Сложность

- **Time:** O(W × H) — каждая ячейка проверяется с 2 соседями
- **Space:** O(1)
- **Early exit:** возвращает true при первом найденном ходе

---

## 11.4 BoardShuffler.cs

### Назначение

Перемешивает элементы на доске когда нет возможных ходов.

### Алгоритм

1. Собрать все элементы и их позиции
2. Перемешать позиции (Fisher-Yates)
3. Обновить Board state
4. Анимировать перемещение (DOTween)

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

        // ... остальные методы
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

## 11.5 SwapHandler.cs (упрощён)

### Назначение

Только swap логика: валидация, анимация, определение успеха/неудачи.

### Код

```csharp
using System;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Input;
using Match3.Elements;
using Match3.Matching;

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
        [SerializeField] private SwapAnimator _swapAnimator;
        [SerializeField] private MatchFinder _matchFinder;

        private bool _isProcessing;

        private void OnEnable()
        {
            _inputDetector.OnSwapRequested += HandleSwapRequest;
        }

        private void OnDisable()
        {
            _inputDetector.OnSwapRequested -= HandleSwapRequest;
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

        private void StartSwap(...)
        {
            _isProcessing = true;
            OnSwapStarted?.Invoke(posA, posB);

            _swapAnimator.AnimateSwap(..., () =>
            {
                _board.SwapElements(posA, posB);

                if (_matchFinder.WouldCreateMatch(posA, posB))
                {
                    _isProcessing = false;
                    OnSwapCompleted?.Invoke(posA, posB);
                }
                else
                {
                    RevertSwap(...);
                }
            });
        }

        private void RevertSwap(...)
        {
            _board.SwapElements(posA, posB);

            _swapAnimator.AnimateRevert(..., () =>
            {
                _isProcessing = false;
                OnSwapReverted?.Invoke(posA, posB);
            });
        }
    }
}
```

### Что убрано из SwapHandler

- ❌ Подписки на DestroyHandler, FallHandler, RefillHandler
- ❌ Cascade логика
- ❌ Deadlock проверка
- ❌ BoardShuffler интеграция
- ❌ InputBlocker (теперь в GameLoopController)

---

## 11.6 GameLoopSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.GameLoop;
using Match3.Grid;
using Match3.Board;
using Match3.Input;
using Match3.Swap;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;

namespace Match3.Editor
{
    public static class GameLoopSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 11 - Game Loop")]
        public static void SetupGameLoop()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            var go = grid.gameObject;

            // BoardShuffler
            var shuffler = go.GetComponent<BoardShuffler>()
                ?? Undo.AddComponent<BoardShuffler>(go);

            // GameLoopController
            var gameLoop = go.GetComponent<GameLoopController>()
                ?? Undo.AddComponent<GameLoopController>(go);

            // Wire dependencies...
        }
    }
}
#endif
```

---

## Диаграмма компонентов на GameObject

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
├── SwapHandler            [Stage 6] ← УПРОЩЁН
├── MatchFinder            [Stage 7]
├── MatchHighlighter       [Stage 7] (debug)
├── DestroyAnimator        [Stage 8]
├── DestroyHandler         [Stage 8]
├── FallAnimator           [Stage 9]
├── FallHandler            [Stage 9]
├── RefillAnimator         [Stage 10]
├── RefillHandler          [Stage 10]
├── BoardShuffler          [Stage 11] ← NEW
└── GameLoopController     [Stage 11] ← NEW (координатор)
```

---

## Тестирование

### Тест 1: Cascade

1. Play Mode
2. Создать ситуацию где после матча падение создаёт новый матч
3. Наблюдать автоматический каскад
4. Проверить `OnCascadeCompleted` параметры

### Тест 2: State Machine

```csharp
// Подписаться на события
gameLoopController.OnStateChanged += state =>
    Debug.Log($"State: {state}");
```

### Тест 3: Deadlock

1. Context Menu на GameLoopController → "Debug Check Deadlock"
2. Или создать искусственный deadlock (шахматный паттерн)

### Тест 4: Shuffle

1. Context Menu на BoardShuffler → "Test Shuffle"
2. Проверить анимацию и корректность Board state

---

## Чеклист

### Код
- [x] `GameState.cs` — enum состояний
- [x] `GameLoopController.cs` — координатор
- [x] `DeadlockChecker.cs` — static class
- [x] `BoardShuffler.cs` — MonoBehaviour
- [x] `GameLoopSetup.cs` — Editor menu
- [x] `SwapHandler.cs` — упрощён

### Тестирование
- [ ] Cascade срабатывает автоматически
- [ ] Многоуровневый cascade работает
- [ ] State машина переключается корректно
- [ ] Deadlock определяется
- [ ] Shuffle перемешивает и анимирует
- [ ] После shuffle проверяются матчи
- [ ] Input заблокирован во время обработки

---

## FAQ

### Q: Почему GameLoopController, а не расширение SwapHandler?

A: Single Responsibility Principle. SwapHandler отвечает только за swap — валидацию, анимацию, revert. GameLoopController координирует весь цикл игры. Это упрощает тестирование и расширение.

### Q: Кто блокирует input?

A: GameLoopController. Блокирует при `OnSwapStarted`, разблокирует в `FinishTurn()`.

### Q: Как добавить scoring?

A: Подписаться на `OnCascadeCompleted(totalDestroyed, cascadeLevel)` и вычислить очки.

### Q: Как добавить combo множитель?

A: `cascadeLevel` — это уровень каскада. `score = basePoints * cascadeLevel`.
