# Step 9: GAME LOOP - Координация систем

> **Цель:** Создать центральный координатор (State Machine), который управляет всеми системами игры и обеспечивает корректный игровой цикл Match-3.

## Обзор

`GameLoopComponent` — это "мозг" игры. Он:
- Управляет состояниями игры через State Machine
- Координирует последовательность: Input → Swap → Match → Destroy → Gravity → Repeat
- Блокирует input во время анимаций
- Обрабатывает каскадные матчи

---

## Зависимости (из других шагов)

```csharp
// Все системы подключаются через SerializeField
[SerializeField] private GridComponent _grid;           // Step 2
[SerializeField] private SpawnComponent _spawn;         // Step 4
[SerializeField] private SwapComponent _swap;           // Step 6
[SerializeField] private MatchDetectionComponent _matchDetection; // Step 5
[SerializeField] private DestructionComponent _destruction;       // Step 7
[SerializeField] private GravityComponent _gravity;     // Step 8
[SerializeField] private InputComponent _input;         // Step 6
```

### STUB-ы для изолированной разработки

```csharp
// STUB: SpawnComponent
public class StubSpawnComponent : MonoBehaviour
{
    public event Action OnGridFilled;
    public void FillGrid() => OnGridFilled?.Invoke();
}

// STUB: SwapComponent
public class StubSwapComponent : MonoBehaviour
{
    public Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2)
        => Task.FromResult(true);
    public Task SwapBack(Vector2Int pos1, Vector2Int pos2)
        => Task.CompletedTask;
}

// STUB: MatchDetectionComponent
public class StubMatchDetectionComponent : MonoBehaviour
{
    private int _callCount = 0;
    public List<Vector2Int> FindAllMatches()
    {
        _callCount++;
        // Первый вызов возвращает матчи, второй — пустой (завершает каскад)
        return _callCount == 1
            ? new List<Vector2Int> { new(0, 0), new(1, 0), new(2, 0) }
            : new List<Vector2Int>();
    }
    public void Reset() => _callCount = 0;
}

// STUB: DestructionComponent
public class StubDestructionComponent : MonoBehaviour
{
    public Task DestroyElements(List<Vector2Int> positions)
        => Task.CompletedTask;
}

// STUB: GravityComponent
public class StubGravityComponent : MonoBehaviour
{
    public Task ApplyGravity() => Task.CompletedTask;
}

// STUB: InputComponent
public class StubInputComponent : MonoBehaviour
{
    public event Action<Vector2Int, Vector2Int> OnSwapRequested;
    public void SimulateSwap(Vector2Int from, Vector2Int to)
        => OnSwapRequested?.Invoke(from, to);
}
```

---

## Файловая структура

```
Assets/Scripts/GameLoop/
└── GameLoopComponent.cs
```

---

## Реализация

### GameLoopComponent.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Match3.GameLoop
{
    /// <summary>
    /// Центральный координатор игры. Управляет State Machine и
    /// последовательностью игровых действий.
    /// </summary>
    public class GameLoopComponent : MonoBehaviour
    {
        // === СОБЫТИЯ ===
        public event Action<GameState> OnStateChanged;
        public event Action OnGameReady;
        public event Action<int> OnMatchesDestroyed; // количество уничтоженных элементов

        // === ЗАВИСИМОСТИ ===
        [Header("Systems")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SpawnComponent _spawn;
        [SerializeField] private SwapComponent _swap;
        [SerializeField] private MatchDetectionComponent _matchDetection;
        [SerializeField] private DestructionComponent _destruction;
        [SerializeField] private GravityComponent _gravity;
        [SerializeField] private InputComponent _input;

        // === СОСТОЯНИЕ ===
        private GameState _currentState = GameState.Initializing;

        public GameState CurrentState => _currentState;
        public bool IsInputAllowed => _currentState == GameState.WaitingForInput;

        // === UNITY CALLBACKS ===

        private void OnEnable()
        {
            _input.OnSwapRequested += HandleSwapRequested;
            _spawn.OnGridFilled += HandleGridFilled;
        }

        private void OnDisable()
        {
            _input.OnSwapRequested -= HandleSwapRequested;
            _spawn.OnGridFilled -= HandleGridFilled;
        }

        private void Start()
        {
            Initialize();
        }

        // === ИНИЦИАЛИЗАЦИЯ ===

        private void Initialize()
        {
            SetState(GameState.Initializing);
            _spawn.FillGrid();
        }

        private void HandleGridFilled()
        {
            SetState(GameState.WaitingForInput);
            OnGameReady?.Invoke();
        }

        // === STATE MACHINE ===

        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);

            Debug.Log($"[GameLoop] State: {_currentState}");
        }

        // === ОБРАБОТКА INPUT ===

        private void HandleSwapRequested(Vector2Int pos1, Vector2Int pos2)
        {
            // Защита от input во время анимаций
            if (!IsInputAllowed)
            {
                Debug.Log("[GameLoop] Input blocked - not in WaitingForInput state");
                return;
            }

            // Запускаем асинхронную обработку
            _ = ProcessSwapAsync(pos1, pos2);
        }

        // === ОСНОВНОЙ ИГРОВОЙ ЦИКЛ ===

        /// <summary>
        /// Полный цикл обработки свапа:
        /// Swap → Check → (Destroy → Gravity → Check)* → WaitingForInput
        /// </summary>
        private async Task ProcessSwapAsync(Vector2Int pos1, Vector2Int pos2)
        {
            // 1. SWAP
            SetState(GameState.Swapping);
            await _swap.TrySwap(pos1, pos2);

            // 2. CHECK MATCHES
            SetState(GameState.CheckingMatches);
            var matches = _matchDetection.FindAllMatches();

            // 3. NO MATCHES → SWAP BACK
            if (matches.Count == 0)
            {
                SetState(GameState.Swapping);
                await _swap.SwapBack(pos1, pos2);
                SetState(GameState.WaitingForInput);
                return;
            }

            // 4. CASCADE LOOP (матчи → удаление → гравитация → повтор)
            await ProcessCascadeAsync(matches);

            // 5. DONE
            SetState(GameState.WaitingForInput);
        }

        /// <summary>
        /// Каскадный цикл: уничтожение → гравитация → проверка новых матчей
        /// Повторяется пока есть матчи
        /// </summary>
        private async Task ProcessCascadeAsync(List<Vector2Int> initialMatches)
        {
            var matches = initialMatches;
            int totalDestroyed = 0;

            while (matches.Count > 0)
            {
                // DESTROY
                SetState(GameState.Destroying);
                await _destruction.DestroyElements(matches);
                totalDestroyed += matches.Count;

                // GRAVITY
                SetState(GameState.Falling);
                await _gravity.ApplyGravity();

                // CHECK FOR NEW MATCHES
                SetState(GameState.CheckingMatches);
                matches = _matchDetection.FindAllMatches();
            }

            if (totalDestroyed > 0)
            {
                OnMatchesDestroyed?.Invoke(totalDestroyed);
            }
        }

        // === PUBLIC API ===

        /// <summary>
        /// Принудительный рестарт игры
        /// </summary>
        public void RestartGame()
        {
            // Очистка сетки (если нужно)
            Initialize();
        }
    }
}
```

---

## Диаграмма State Machine

```
                    ┌──────────────────┐
                    │   Initializing   │
                    └────────┬─────────┘
                             │ FillGrid()
                             ▼
    ┌──────────────────────────────────────────────┐
    │                                              │
    │              WaitingForInput                 │◄───────────────┐
    │                                              │                │
    └──────────────────────┬───────────────────────┘                │
                           │ OnSwapRequested                        │
                           ▼                                        │
                    ┌──────────────────┐                            │
                    │     Swapping     │                            │
                    └────────┬─────────┘                            │
                             │ TrySwap()                            │
                             ▼                                      │
                    ┌──────────────────┐                            │
                    │ CheckingMatches  │                            │
                    └────────┬─────────┘                            │
                             │                                      │
              ┌──────────────┴──────────────┐                       │
              │                             │                       │
        matches.Count == 0            matches.Count > 0             │
              │                             │                       │
              ▼                             ▼                       │
       ┌──────────────┐              ┌──────────────┐               │
       │   Swapping   │              │  Destroying  │               │
       │  (SwapBack)  │              └──────┬───────┘               │
       └──────┬───────┘                     │ DestroyElements()     │
              │                             ▼                       │
              │                      ┌──────────────┐               │
              │                      │   Falling    │               │
              │                      └──────┬───────┘               │
              │                             │ ApplyGravity()        │
              │                             ▼                       │
              │                      ┌──────────────┐               │
              │                      │CheckingMatches│              │
              │                      └──────┬───────┘               │
              │                             │                       │
              │               ┌─────────────┴─────────────┐         │
              │               │                           │         │
              │         matches > 0                 matches == 0    │
              │               │                           │         │
              │               └──────► (loop) ◄───────────┘         │
              │                                                     │
              └─────────────────────────────────────────────────────┘
```

---

## Ключевые аспекты реализации

### 1. Защита от input во время анимаций

```csharp
public bool IsInputAllowed => _currentState == GameState.WaitingForInput;

private void HandleSwapRequested(Vector2Int pos1, Vector2Int pos2)
{
    if (!IsInputAllowed) return; // БЛОК!
    // ...
}
```

InputComponent должен проверять `IsInputAllowed` перед отправкой события, но GameLoop тоже проверяет как fail-safe.

### 2. Асинхронность без блокировки

```csharp
// Fire-and-forget паттерн с discard
_ = ProcessSwapAsync(pos1, pos2);
```

Это позволяет вернуться из HandleSwapRequested немедленно, пока async операции выполняются.

### 3. Каскадный цикл

```csharp
while (matches.Count > 0)
{
    await _destruction.DestroyElements(matches);
    await _gravity.ApplyGravity();
    matches = _matchDetection.FindAllMatches();
}
```

Цикл продолжается пока новые матчи появляются после гравитации.

### 4. События для UI/Audio

```csharp
public event Action<GameState> OnStateChanged;
public event Action<int> OnMatchesDestroyed;
```

Внешние системы (UI, Audio) подписываются на события вместо прямых вызовов.

---

## Подзадачи

- [ ] Создать структуру папки `Assets/Scripts/GameLoop/`
- [ ] Реализовать GameLoopComponent с State Machine
- [ ] Реализовать Initialize() — подписка на события, запуск FillGrid
- [ ] Реализовать SetState() — переключение состояний с событием
- [ ] Реализовать HandleSwapRequested() — защита от input
- [ ] Реализовать ProcessSwapAsync() — полный цикл обработки
- [ ] Реализовать ProcessCascadeAsync() — каскадный цикл
- [ ] Добавить события OnStateChanged, OnGameReady, OnMatchesDestroyed
- [ ] Тест: симулировать swap через stub, проверить последовательность состояний
- [ ] Тест: каскад (несколько итераций матчей)
- [ ] Тест: неудачный swap (SwapBack)

---

## Интеграция со сценой

```
GameObject: GameManager
├── GameLoopComponent
│   ├── _grid: → GridComponent (на Grid объекте)
│   ├── _spawn: → SpawnComponent (на Grid объекте)
│   ├── _swap: → SwapComponent (на Grid объекте)
│   ├── _matchDetection: → MatchDetectionComponent (на Grid объекте)
│   ├── _destruction: → DestructionComponent (на Grid объекте)
│   ├── _gravity: → GravityComponent (на Grid объекте)
│   └── _input: → InputComponent (на InputManager объекте)
```

---

## Расширения (после MVP)

1. **Scoring System** — подписка на OnMatchesDestroyed
2. **Combo Counter** — отслеживание каскадов
3. **Move Counter** — ограничение ходов
4. **Game Over Check** — проверка возможных ходов
5. **Pause/Resume** — дополнительные состояния

---

## Тестовый сценарий

```csharp
// Юнит-тест последовательности состояний
[Test]
public void ProcessSwap_WithMatches_FollowsCorrectStateSequence()
{
    var states = new List<GameState>();
    _gameLoop.OnStateChanged += state => states.Add(state);

    _stubInput.SimulateSwap(new Vector2Int(0, 0), new Vector2Int(1, 0));

    // Ожидаемая последовательность:
    Assert.AreEqual(GameState.Swapping, states[0]);
    Assert.AreEqual(GameState.CheckingMatches, states[1]);
    Assert.AreEqual(GameState.Destroying, states[2]);
    Assert.AreEqual(GameState.Falling, states[3]);
    Assert.AreEqual(GameState.CheckingMatches, states[4]);
    Assert.AreEqual(GameState.WaitingForInput, states[5]);
}
```
