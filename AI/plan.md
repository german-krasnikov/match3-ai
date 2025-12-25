# Match-3 Game Architecture - Декомпозиция базовых механик

## Обзор
Пошаговый план создания Match-3 игры с использованием композиции (Unity Way).
Каждый этап можно внедрять и тестировать отдельно.

---

## Параметры проекта (согласовано)

- **Размер сетки**: 8x8 (классика)
- **Типы элементов**: 5 цветов (Red, Blue, Green, Yellow, Purple)
- **Стиль анимаций**: Bouncy/Casual (пружинящие, overshoot, весело)
- **Scoring**: НЕТ на этом этапе, добавим позже

---

## Этап 1: Grid System (Сетка) ✅

### 1.1 Структура данных сетки
- [x] `GridData` - ScriptableObject с параметрами сетки
  - `width`, `height` (int)
  - `cellSize` (float)
  - `spacing` (float)
- [x] `Cell` - struct для ячейки
  - `Vector2Int position`

### 1.2 GridComponent
- [x] `GridComponent : MonoBehaviour` - управление сеткой
  - `[SerializeField] GridData _gridData`
  - `Cell[,] _cells` - двумерный массив
  - `Vector3 GridToWorld(Vector2Int pos)` - конверсия координат
  - `Vector2Int WorldToGrid(Vector3 pos)`
  - `bool IsValidPosition(Vector2Int pos)`
  - `event Action OnGridReady`

### 1.3 Визуализация
- [x] Gizmos встроены в GridComponent (OnDrawGizmos)

### 1.4 Editor Setup
- [x] `GridSceneSetup` - создание сетки через меню

**Файлы:**
- `Assets/Scripts/Grid/GridData.cs`
- `Assets/Scripts/Grid/Cell.cs`
- `Assets/Scripts/Grid/GridComponent.cs`
- `Assets/Scripts/Editor/GridSceneSetup.cs`
- `Assets/Data/Grid/DefaultGridData.asset`

---

## Этап 2: Elements (Элементы/Тайлы) ✅

### 2.1 Типы элементов
- [x] `ElementType` - enum (Red, Blue, Green, Yellow, Purple)
- [x] `ElementData` - ScriptableObject
  - `ElementType type`
  - `Sprite sprite`
  - `Color color`
- [x] `ElementDatabase` - ScriptableObject со списком всех ElementData

### 2.2 ElementComponent
- [x] `ElementComponent : MonoBehaviour` - компонент элемента
  - `[SerializeField] SpriteRenderer _spriteRenderer`
  - `ElementType Type { get; private set; }`
  - `Vector2Int GridPosition { get; set; }`
  - `void Initialize(ElementData data, Vector2Int gridPos)`
  - `event Action<ElementComponent> OnDestroyed`

### 2.3 Префаб элемента
- [x] Prefab: SpriteRenderer + ElementComponent
  - Настройка сортировки спрайтов

### 2.4 Editor Setup (бонус)
- [x] `ElementsSetup` - автоматическое создание ассетов
  - Menu: Match3 → Setup → Create Sorting Layers
  - Menu: Match3 → Setup → Create Element Assets

**Файлы:**
- `Assets/Scripts/Elements/ElementType.cs`
- `Assets/Scripts/Elements/ElementData.cs`
- `Assets/Scripts/Elements/ElementDatabase.cs`
- `Assets/Scripts/Elements/ElementComponent.cs`
- `Assets/Scripts/Editor/ElementsSetup.cs`

**Детальный план:** `AI/plan_stage2_elements.md`

---

## Этап 3: Spawn System (Спаун) ✅

### 3.1 ElementFactory
- [x] `ElementFactory : MonoBehaviour` - создание элементов
  - `[SerializeField] ElementPool _pool`
  - `[SerializeField] ElementDatabase _database`
  - `ElementComponent Create(ElementType type, Vector3 worldPos, Vector2Int gridPos)`
  - `ElementComponent CreateRandom(Vector3 worldPos, Vector2Int gridPos)`
  - `ElementComponent CreateRandomExcluding(...)` - для алгоритма без матчей
  - `void Return(ElementComponent element)` - для пулинга

### 3.2 Object Pooling
- [x] `ElementPool` - пул объектов для переиспользования
  - `Stack<ElementComponent> _pool`
  - `Prewarm()` - предзаполнение 64 объектами
  - `ElementComponent Get()`
  - `void Release(ElementComponent element)`

### 3.3 InitialBoardSpawner
- [x] `InitialBoardSpawner : MonoBehaviour` - начальное заполнение
  - `[SerializeField] GridComponent _grid`
  - `[SerializeField] ElementFactory _factory`
  - `void SpawnInitialBoard()`
  - `ElementComponent[,] SpawnedElements` - для передачи в BoardComponent
  - Алгоритм: заполнение без начальных матчей (проверка 2 соседей)

**Файлы:**
- `Assets/Scripts/Spawn/ElementFactory.cs`
- `Assets/Scripts/Spawn/ElementPool.cs`
- `Assets/Scripts/Spawn/InitialBoardSpawner.cs`

**Детальный план:** `AI/plan_stage3_spawn.md`

---

## Этап 4: Board State (Состояние доски) ✅

### 4.1 BoardComponent
- [x] `BoardComponent : MonoBehaviour` - хранит состояние
  - `ElementComponent[,] _elements` - элементы на сетке
  - `ElementComponent GetElement(Vector2Int pos)`
  - `void SetElement(Vector2Int pos, ElementComponent element)`
  - `ElementComponent RemoveElement(Vector2Int pos)` - возвращает для пулинга
  - `bool IsEmpty(Vector2Int pos)`
  - `List<Vector2Int> GetEmptyPositions()`
  - `List<int> GetEmptyRowsInColumn(int)` - для Fall System
  - `ElementType? GetElementType(Vector2Int)` - для MatchFinder
  - `void SwapElements(Vector2Int, Vector2Int)` - для Swap System
  - События: OnElementSet, OnElementRemoved

### 4.2 Интеграция с Grid
- [x] BoardComponent зависит от GridComponent
  - Инициализация массива по размерам сетки
- [x] InitialBoardSpawner интегрирован с BoardComponent

**Файлы:**
- `Assets/Scripts/Board/BoardComponent.cs`
- `Assets/Scripts/Editor/BoardSystemSetup.cs`

**Editor Setup:** `Match3 → Setup Scene → Stage 4 - Board System`

**Детальный план:** `AI/plan_stage4_board.md`

---

## Этап 5: Input System (Ввод) ✅

### 5.1 InputDetector
- [x] `InputDetector : MonoBehaviour` - обработка ввода
  - Raycast для определения тайла под курсором/тачем
  - `event Action<Vector2Int> OnElementSelected`
  - `event Action<Vector2Int, Vector2Int> OnSwapRequested`

### 5.2 Логика свайпа
- [x] Определение направления свайпа
  - Первый клик → сохранить позицию
  - Драг/второй клик → определить направление
  - Валидация: только соседние ячейки (4 направления)

### 5.3 InputBlocker
- [x] `InputBlocker` - блокировка ввода во время анимаций
  - `bool IsBlocked { get; set; }`
  - Проверка перед обработкой ввода

### 5.4 SelectionHighlighter
- [x] `SelectionHighlighter` - визуальная подсветка выбранного элемента
  - Пульсирующая анимация через DOTween

**Файлы:**
- `Assets/Scripts/Input/InputDetector.cs`
- `Assets/Scripts/Input/SwipeDirection.cs`
- `Assets/Scripts/Input/InputBlocker.cs`
- `Assets/Scripts/Input/SelectionHighlighter.cs`
- `Assets/Scripts/Editor/InputSystemSetup.cs`

**Детальный план:** `AI/plan_stage5_input.md`

---

## Этап 6: Swap System (Свап) ✅

### 6.1 SwapHandler
- [x] `SwapHandler : MonoBehaviour` - логика обмена
  - `[SerializeField] BoardComponent _board`
  - `bool CanSwap(Vector2Int a, Vector2Int b)` - проверка соседства
  - `void ExecuteSwap(Vector2Int a, Vector2Int b)`
  - `event Action<Vector2Int, Vector2Int> OnSwapStarted`
  - `event Action<Vector2Int, Vector2Int> OnSwapCompleted`
  - `event Action<Vector2Int, Vector2Int> OnSwapReverted`

### 6.2 SwapAnimator
- [x] `SwapAnimator : MonoBehaviour` - анимация обмена (DOTween)
  - `[SerializeField] float _swapDuration = 0.25f`
  - `Tween AnimateSwap(ElementComponent a, ElementComponent b)`
  - Sequence: одновременное движение двух элементов

### 6.3 SwapValidator
- [x] Проверка: привёл ли свап к матчу? (stub в SwapHandler)
  - Если нет → реверс свапа
  - Если да → продолжить цикл
  - **Примечание:** Временная заглушка, реальная логика в Этапе 7

**Файлы:**
- `Assets/Scripts/Swap/SwapHandler.cs`
- `Assets/Scripts/Swap/SwapAnimator.cs`
- `Assets/Scripts/Editor/SwapSystemSetup.cs`

**Детальный план:** `AI/plan_stage6_swap.md`

---

## Этап 7: Match Detection (Поиск матчей) ✅

### 7.1 MatchFinder
- [x] `MatchFinder : MonoBehaviour` - алгоритм поиска
  - `List<Match> FindAllMatches()` - полный скан доски
  - `List<Match> FindMatchesAt(Vector2Int pos)` - проверка конкретной позиции
  - `bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)` - для валидации свапа
  - Горизонтальный проход: 3+ подряд
  - Вертикальный проход: 3+ подряд
  - Объединение пересекающихся матчей (L/T → Cross)

### 7.2 Match структура
- [x] `Match` - readonly struct данных о совпадении
  - `ElementType Type`
  - `IReadOnlyList<Vector2Int> Positions`
  - `MatchOrientation Orientation` (Horizontal/Vertical/Cross)
  - `int Count`, `bool IsValid`

### 7.3 MatchHighlighter (debug)
- [x] Визуальная подсветка найденных матчей через Gizmos
- [x] Context Menu: "Find And Highlight All Matches"

### 7.4 Интеграция в SwapHandler
- [x] Добавить `[SerializeField] MatchFinder _matchFinder`
- [x] Заменить заглушку `CheckForMatch()` на реальную проверку

**Файлы:**
- `Assets/Scripts/Match/Match.cs`
- `Assets/Scripts/Match/MatchFinder.cs`
- `Assets/Scripts/Match/MatchHighlighter.cs`
- `Assets/Scripts/Editor/MatchSystemSetup.cs`

**Детальный план:** `AI/plan_stage7_match.md`

---

## Этап 8: Destroy System (Уничтожение) ✅

### 8.1 DestroyHandler
- [x] `DestroyHandler : MonoBehaviour`
  - `void DestroyMatches(List<Match> matches)`
  - Удаление из BoardComponent
  - Возврат в пул (ElementPool)
  - `event Action<int> OnDestroyCompleted`

### 8.2 DestroyAnimator
- [x] `DestroyAnimator : MonoBehaviour` - анимации уничтожения
  - Punch → Shrink → Fade (DOTween)
  - Stagger delay между элементами
  - Reset scale/alpha для пулинга

### 8.3 Интеграция в SwapHandler
- [x] После успешного свапа вызывается DestroyMatches
- [x] Input блокируется до завершения анимации

### 8.4 Particle Effects (опционально)
- [ ] VFX при уничтожении элементов

**Файлы:**
- `Assets/Scripts/Destroy/DestroyHandler.cs`
- `Assets/Scripts/Destroy/DestroyAnimator.cs`
- `Assets/Scripts/Editor/DestroySystemSetup.cs`

**Детальный план:** `AI/plan_stage8_destroy.md`

---

## Этап 9: Fall System (Падение) ✅

### 9.1 FallCalculator
- [x] `FallCalculator` - расчёт падения
  - `List<FallData> CalculateFalls(BoardComponent board)`
  - Для каждого столбца снизу вверх:
    - Найти пустые ячейки
    - Определить кто куда падает

### 9.2 FallData
- [x] `FallData` - struct
  - `ElementComponent element`
  - `Vector2Int from`
  - `Vector2Int to`
  - `int distance`

### 9.3 FallHandler
- [x] `FallHandler : MonoBehaviour`
  - `void ExecuteFalls(List<FallData> falls)`
  - Обновление BoardComponent
  - `event Action OnFallsCompleted`

### 9.4 FallAnimator
- [x] `FallAnimator : MonoBehaviour` - анимация падения
  - `[SerializeField] float _fallSpeed = 10f` (units/sec)
  - `[SerializeField] AnimationCurve _fallCurve` (ease)
  - `Sequence AnimateFalls(List<FallData> falls)`
  - Bounce эффект при приземлении

**Файлы:**
- `Assets/Scripts/Fall/FallCalculator.cs`
- `Assets/Scripts/Fall/FallData.cs`
- `Assets/Scripts/Fall/FallHandler.cs`
- `Assets/Scripts/Fall/FallAnimator.cs`

**Детальный план:** `AI/plan_stage9_fall.md`

---

## Этап 10: Refill System (Заполнение сверху)

### 10.1 RefillCalculator
- [ ] `RefillCalculator` - определение новых элементов
  - `List<RefillData> CalculateRefills(BoardComponent board, GridComponent grid)`
  - Для каждой пустой позиции:
    - Вычислить spawn позицию выше сетки
    - Создать RefillData

### 10.2 RefillData
- [ ] `RefillData` - readonly struct
  - `Vector2Int TargetPosition`
  - `Vector2Int SpawnPosition`
  - `Vector3 SpawnWorldPosition`
  - `Vector3 TargetWorldPosition`
  - `int FallDistance`

### 10.3 RefillHandler
- [ ] `RefillHandler : MonoBehaviour`
  - `[SerializeField] ElementFactory _factory`
  - `[SerializeField] RefillAnimator _animator`
  - `void ExecuteRefills()`
  - `event Action OnRefillsStarted`
  - `event Action OnRefillsCompleted`

### 10.4 RefillAnimator
- [ ] `RefillAnimator : MonoBehaviour` - анимация появления и падения
  - Spawn scale effect (0.5 → 1.0)
  - Fall с InQuad easing
  - Bounce при приземлении

### 10.5 Интеграция в SwapHandler
- [ ] `FallHandler.OnFallsCompleted` → `RefillHandler.ExecuteRefills()`
- [ ] `RefillHandler.OnRefillsCompleted` → cascade check (Stage 11)

**Файлы:**
- `Assets/Scripts/Refill/RefillCalculator.cs`
- `Assets/Scripts/Refill/RefillData.cs`
- `Assets/Scripts/Refill/RefillHandler.cs`
- `Assets/Scripts/Refill/RefillAnimator.cs`
- `Assets/Scripts/Editor/RefillSystemSetup.cs`

**Детальный план:** `AI/plan_stage10_refill.md`

---

## Этап 11: Game Loop (Игровой цикл)

### 11.1 GameLoopController
- [ ] `GameLoopController : MonoBehaviour` - State Machine
  - States: `Idle`, `Swapping`, `Matching`, `Destroying`, `Falling`, `Refilling`
  - `event Action<GameState> OnStateChanged`

### 11.2 Цикл обработки
```
Idle → [Swap Input] → Swapping
Swapping → [Swap Complete] → Matching
Matching → [Matches Found] → Destroying → Falling → Refilling → Matching
Matching → [No Matches] → Idle
```

### 11.3 Cascade Handler
- [ ] Повторный цикл проверки после заполнения
  - Пока есть матчи → уничтожить → падение → заполнение → проверка

### 11.4 DeadlockChecker
- [ ] `DeadlockChecker` - проверка возможных ходов
  - `bool HasPossibleMoves(BoardComponent board)`
  - Если нет ходов → перемешать доску

**Файлы:**
- `Assets/Scripts/GameLoop/GameLoopController.cs`
- `Assets/Scripts/GameLoop/GameState.cs`
- `Assets/Scripts/GameLoop/CascadeHandler.cs`
- `Assets/Scripts/GameLoop/DeadlockChecker.cs`

---

## Этап 12: Scene Setup (Настройка сцены)

### 12.1 GameManager Prefab
- [ ] Root GameObject с компонентами:
  - GridComponent
  - BoardComponent
  - GameLoopController
  - InputDetector
  - InputBlocker

### 12.2 Factories & Handlers
- [ ] Child GameObject "Systems":
  - ElementFactory
  - SwapHandler + SwapAnimator
  - DestroyHandler + DestroyAnimator
  - FallHandler + FallAnimator
  - RefillHandler

### 12.3 Wiring
- [ ] Все зависимости через SerializeField
- [ ] События связывают компоненты

---

## Порядок реализации (рекомендуемый)

1. **Grid + Elements** - можно увидеть сетку и элементы
2. **Spawn + Board** - заполненная доска
3. **Input** - выбор и свайп элементов
4. **Swap** - обмен с анимацией
5. **Match Detection** - поиск совпадений
6. **Destroy** - удаление с анимацией
7. **Fall** - падение элементов
8. **Refill** - заполнение сверху
9. **Game Loop** - объединение в цикл
10. **Polish** - анимации, эффекты, звуки

---

## Структура папок

```
Assets/Scripts/
├── Grid/
├── Elements/
├── Spawn/
├── Board/
├── Input/
├── Swap/
├── Match/
├── Destroy/
├── Fall/
├── Refill/
├── GameLoop/
└── Utils/
```
