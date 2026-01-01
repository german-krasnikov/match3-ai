# Match3 — Global Plan

## Архитектура

```
┌─────────────────────────────────────────────────────────┐
│                    GameStateMachine                     │
│  (Idle → Swap → Match → Destroy → Fall → Check → Idle) │
└─────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│ InputSystem │      │  BoardData  │      │ BoardView   │
│  (Swipe)    │      │  (Model)    │      │  (Visual)   │
└─────────────┘      └─────────────┘      └─────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
   ┌───────────┐     ┌────────────┐     ┌────────────┐
   │MatchSystem│     │ SpawnSystem│     │ FallSystem │
   └───────────┘     └────────────┘     └────────────┘
```

**Принципы:**
- `BoardData` — чистые данные (int[,] grid, gem positions)
- `BoardView` — визуализация (создаёт/двигает/удаляет GameObjects)
- Системы работают с данными, вызывают события, View реагирует
- StateMachine управляет последовательностью

---

## Зависимости между шагами

```
Step 1 (Grid) ──────┬──► Step 2 (Gems) ──► Step 3 (Spawn)
                    │                            │
                    │                            ▼
                    │                      Step 4 (Fall)
                    │                            │
                    └──► Step 5 (Input/Swap) ◄───┘
                                │
                                ▼
                         Step 6 (Match)
                                │
                                ▼
                         Step 7 (Destroy)
                                │
                                ▼
                         Step 8 (GameLoop)
```

---

## Step 1: Grid System

**Цель:** Создать структуру сетки и её визуальное представление.

### Подзадачи:
1.1. `GridConfig` — ScriptableObject с размером сетки (width=8, height=8), размером ячейки
1.2. `GridData` — хранит размеры, конвертирует grid coords ↔ world position
1.3. `GridView` — создаёт визуальную сетку (опционально: спрайты ячеек)

### Файлы:
```
Assets/Scripts/
  Grid/
    GridConfig.cs         # ScriptableObject [CreateAssetMenu]
    GridData.cs           # class, не MonoBehaviour
    GridView.cs           # MonoBehaviour, рисует сетку
```

### Stub-интерфейс для других шагов:
```csharp
// GridData.cs — другие системы используют:
public Vector2Int Size { get; }
public Vector3 GridToWorld(Vector2Int pos);
public Vector2Int WorldToGrid(Vector3 worldPos);
public bool IsValidPosition(Vector2Int pos);
```

---

## Step 2: Gem System

**Цель:** Определить типы элементов и их визуальное представление.

### Подзадачи:
2.1. `GemType` — enum (Red, Blue, Green, Yellow, Purple, Orange)
2.2. `GemConfig` — ScriptableObject со списком типов и их спрайтами
2.3. `GemView` — MonoBehaviour на prefab-е, отображает спрайт по типу
2.4. `GemData` — struct с типом и позицией

### Файлы:
```
Assets/Scripts/
  Gem/
    GemType.cs            # enum
    GemConfig.cs          # ScriptableObject, List<GemTypeData>
    GemData.cs            # struct { GemType type; Vector2Int pos; }
    GemView.cs            # MonoBehaviour на prefab-е
Assets/Prefabs/
    Gem.prefab            # SpriteRenderer + GemView
```

### Stub-интерфейс:
```csharp
// GemView.cs
public void Setup(GemType type, GemConfig config);
public void SetWorldPosition(Vector3 pos);
public GemType Type { get; }

// GemConfig.cs
public Sprite GetSprite(GemType type);
public GemType GetRandomType();
public int TypeCount { get; }
```

---

## Step 3: Spawn System

**Цель:** Заполнить сетку элементами при старте и спаунить новые сверху.

### Подзадачи:
3.1. `BoardData` — хранит GemData[,] сетку, CRUD операции
3.2. `SpawnSystem` — генерирует тип с анти-матч проверкой
3.3. `BoardView` — создаёт GemView для каждой позиции
3.4. Initial fill — заполнение при старте игры

### Файлы:
```
Assets/Scripts/
  Board/
    BoardData.cs          # class, GemData?[,] _gems
    BoardView.cs          # MonoBehaviour, управляет GemView instances
  Spawn/
    SpawnSystem.cs        # class, логика генерации типов
```

### Stub-интерфейс:
```csharp
// BoardData.cs
public GemData? GetGem(Vector2Int pos);
public void SetGem(Vector2Int pos, GemData gem);
public void RemoveGem(Vector2Int pos);
public bool IsEmpty(Vector2Int pos);
public event Action<Vector2Int, GemData> OnGemAdded;
public event Action<Vector2Int> OnGemRemoved;

// SpawnSystem.cs
public GemType GenerateType(Vector2Int pos, BoardData board);

// BoardView.cs
public GemView GetView(Vector2Int pos);
public void CreateGem(Vector2Int pos, GemData gem);
public void DestroyGem(Vector2Int pos);
```

---

## Step 4: Fall System

**Цель:** Заставить элементы падать вниз после удаления.

### Подзадачи:
4.1. `FallSystem` — находит пустые ячейки, сдвигает данные вниз
4.2. `FallAnimator` — анимирует падение GemView (DOTween InOutQuad)
4.3. Интеграция со SpawnSystem — спаун новых сверху после падения

### Файлы:
```
Assets/Scripts/
  Fall/
    FallSystem.cs         # class, логика падения в BoardData
    FallAnimator.cs       # MonoBehaviour или static, анимации
```

### Stub-интерфейс:
```csharp
// FallSystem.cs
public struct FallMove { Vector2Int from; Vector2Int to; }
public List<FallMove> CalculateFalls(BoardData board);
public void ApplyFalls(BoardData board, List<FallMove> moves);

// FallAnimator.cs
public Tween AnimateFall(GemView gem, Vector3 targetPos, float distance);
public event Action OnAllFallsComplete;
```

---

## Step 5: Input & Swap System

**Цель:** Обработка свайпов и обмен элементов.

### Подзадачи:
5.1. `SwipeDetector` — детектит свайп, возвращает from + direction
5.2. `SwapSystem` — валидирует свап (8 направлений, соседние клетки)
5.3. `SwapAnimator` — анимация обмена, анимация возврата при неудаче
5.4. Интеграция с MatchSystem для валидации

### Файлы:
```
Assets/Scripts/
  Input/
    SwipeDetector.cs      # MonoBehaviour, обрабатывает touch/mouse
  Swap/
    SwapSystem.cs         # class, логика обмена
    SwapAnimator.cs       # анимации свапа
```

### Stub-интерфейс:
```csharp
// SwipeDetector.cs
public event Action<Vector2Int, Vector2Int> OnSwipeDetected; // from, to

// SwapSystem.cs
public bool IsValidSwap(Vector2Int from, Vector2Int to, BoardData board);
public void PerformSwap(BoardData board, Vector2Int a, Vector2Int b);
public bool WillMatch(Vector2Int a, Vector2Int b, BoardData board); // для валидации

// SwapAnimator.cs
public Tween AnimateSwap(GemView a, GemView b);
public Tween AnimateSwapBack(GemView a, GemView b);
```

---

## Step 6: Match System

**Цель:** Поиск матчей (3+ в линию по горизонтали/вертикали).

### Подзадачи:
6.1. `MatchSystem` — находит все матчи на доске
6.2. `MatchData` — структура матча (позиции, тип, направление)
6.3. Проверка после свапа — есть ли матч

### Файлы:
```
Assets/Scripts/
  Match/
    MatchSystem.cs        # class, алгоритм поиска
    MatchData.cs          # struct, данные о матче
```

### Stub-интерфейс:
```csharp
// MatchData.cs
public struct MatchData {
    public List<Vector2Int> positions;
    public GemType type;
}

// MatchSystem.cs
public List<MatchData> FindAllMatches(BoardData board);
public List<MatchData> FindMatchesAt(BoardData board, Vector2Int pos);
public bool HasAnyMatch(BoardData board);
```

---

## Step 7: Destroy System

**Цель:** Удаление matched элементов с анимацией.

### Подзадачи:
7.1. `DestroySystem` — удаляет gem-ы из BoardData
7.2. `DestroyAnimator` — анимация scale to zero, каскадом с задержкой
7.3. События для начисления очков (stub для будущего)

### Файлы:
```
Assets/Scripts/
  Destroy/
    DestroySystem.cs      # class, удаление из данных
    DestroyAnimator.cs    # анимации уничтожения
```

### Stub-интерфейс:
```csharp
// DestroySystem.cs
public void DestroyGems(BoardData board, List<Vector2Int> positions);
public event Action<List<Vector2Int>> OnGemsDestroyed;

// DestroyAnimator.cs
public Tween AnimateDestroy(List<GemView> gems, float cascadeDelay);
public event Action OnDestroyComplete;
```

---

## Step 8: Game Loop (StateMachine)

**Цель:** Управление игровым циклом через StateMachine.

### Подзадачи:
8.1. `GameState` — enum состояний
8.2. `GameStateMachine` — переключает состояния, вызывает системы
8.3. `GameController` — точка входа, связывает всё вместе

### States:
```
Idle        → ждём ввода игрока
Swapping    → анимация свапа
Matching    → поиск матчей
Destroying  → анимация уничтожения
Falling     → падение + спаун
Checking    → проверка новых матчей → Matching или Idle
```

### Файлы:
```
Assets/Scripts/
  Game/
    GameState.cs          # enum
    GameStateMachine.cs   # class, FSM логика
    GameController.cs     # MonoBehaviour, точка входа
```

### Stub-интерфейс:
```csharp
// GameState.cs
public enum GameState { Idle, Swapping, Matching, Destroying, Falling, Checking }

// GameStateMachine.cs
public GameState CurrentState { get; }
public void SetState(GameState state);
public event Action<GameState> OnStateChanged;

// GameController.cs
// Связывает все системы, подписывается на события, управляет flow
```

---

## Структура папок (итого)

```
Assets/
  Scripts/
    Grid/
      GridConfig.cs
      GridData.cs
      GridView.cs
    Gem/
      GemType.cs
      GemConfig.cs
      GemData.cs
      GemView.cs
    Board/
      BoardData.cs
      BoardView.cs
    Spawn/
      SpawnSystem.cs
    Fall/
      FallSystem.cs
      FallAnimator.cs
    Input/
      SwipeDetector.cs
    Swap/
      SwapSystem.cs
      SwapAnimator.cs
    Match/
      MatchSystem.cs
      MatchData.cs
    Destroy/
      DestroySystem.cs
      DestroyAnimator.cs
    Game/
      GameState.cs
      GameStateMachine.cs
      GameController.cs
  Prefabs/
    Gem.prefab
  ScriptableObjects/
    GridConfig.asset
    GemConfig.asset
  Scenes/
    Game.unity
```

---

## Порядок реализации

| Step | Название | Зависит от | Результат |
|------|----------|------------|-----------|
| 1 | Grid System | — | Сетка с конвертацией координат |
| 2 | Gem System | — | Prefab и типы элементов |
| 3 | Spawn System | 1, 2 | BoardData + BoardView + начальное заполнение |
| 4 | Fall System | 3 | Падение элементов с анимацией |
| 5 | Input & Swap | 3, 4 | Свайп + обмен элементов |
| 6 | Match System | 3 | Поиск матчей |
| 7 | Destroy System | 3, 6 | Удаление с анимацией |
| 8 | Game Loop | все | StateMachine, полный цикл |

---

## Настройки по умолчанию

- **Grid:** 8×8, cell size 1.0, origin (0,0) левый нижний
- **Gems:** 6 типов (Red, Blue, Green, Yellow, Purple, Orange)
- **Fall:** DOTween Ease.InOutQuad, speed ~8 units/sec
- **Swap:** 8 направлений, валидация на матч
- **Match:** минимум 3 в линию, H+V
- **Destroy:** scale 0, cascade delay 0.05s
