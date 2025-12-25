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

## Этап 1: Grid System (Сетка)

### 1.1 Структура данных сетки
- [ ] `GridData` - ScriptableObject с параметрами сетки
  - `width`, `height` (int)
  - `cellSize` (float)
  - `spacing` (float)
- [ ] `Cell` - struct для ячейки
  - `Vector2Int position`
  - `bool isBlocked`

### 1.2 GridComponent
- [ ] `GridComponent : MonoBehaviour` - управление сеткой
  - `[SerializeField] GridData _gridData`
  - `Cell[,] _cells` - двумерный массив
  - `Vector3 GridToWorld(Vector2Int pos)` - конверсия координат
  - `Vector2Int WorldToGrid(Vector3 pos)`
  - `bool IsValidPosition(Vector2Int pos)`
  - `event Action<GridComponent> OnGridReady`

### 1.3 Визуализация (опционально)
- [ ] `GridVisualizer` - debug отрисовка сетки в Editor
  - Gizmos для границ ячеек

**Файлы:**
- `Assets/Scripts/Grid/GridData.cs`
- `Assets/Scripts/Grid/Cell.cs`
- `Assets/Scripts/Grid/GridComponent.cs`
- `Assets/Scripts/Grid/GridVisualizer.cs`

---

## Этап 2: Elements (Элементы/Тайлы)

### 2.1 Типы элементов
- [ ] `ElementType` - enum (Red, Blue, Green, Yellow, Purple)
- [ ] `ElementData` - ScriptableObject
  - `ElementType type`
  - `Sprite sprite`
  - `Color color`
- [ ] `ElementDatabase` - ScriptableObject со списком всех ElementData

### 2.2 ElementComponent
- [ ] `ElementComponent : MonoBehaviour` - компонент элемента
  - `[SerializeField] SpriteRenderer _spriteRenderer`
  - `ElementType Type { get; private set; }`
  - `Vector2Int GridPosition { get; set; }`
  - `void Initialize(ElementData data, Vector2Int gridPos)`
  - `event Action<ElementComponent> OnDestroyed`

### 2.3 Префаб элемента
- [ ] Prefab: SpriteRenderer + ElementComponent
  - Настройка сортировки спрайтов

**Файлы:**
- `Assets/Scripts/Elements/ElementType.cs`
- `Assets/Scripts/Elements/ElementData.cs`
- `Assets/Scripts/Elements/ElementDatabase.cs`
- `Assets/Scripts/Elements/ElementComponent.cs`

---

## Этап 3: Spawn System (Спаун)

### 3.1 ElementFactory
- [ ] `ElementFactory : MonoBehaviour` - создание элементов
  - `[SerializeField] ElementComponent _elementPrefab`
  - `[SerializeField] ElementDatabase _database`
  - `ElementComponent Create(ElementType type, Vector3 worldPos)`
  - `ElementComponent CreateRandom(Vector3 worldPos)`
  - `void Return(ElementComponent element)` - для пулинга

### 3.2 Object Pooling
- [ ] `ElementPool` - пул объектов для переиспользования
  - `Stack<ElementComponent> _pool`
  - `ElementComponent Get()`
  - `void Release(ElementComponent element)`

### 3.3 InitialBoardSpawner
- [ ] `InitialBoardSpawner : MonoBehaviour` - начальное заполнение
  - `[SerializeField] GridComponent _grid`
  - `[SerializeField] ElementFactory _factory`
  - `void SpawnInitialBoard()`
  - Алгоритм: заполнение без начальных матчей (проверка 2 соседей)

**Файлы:**
- `Assets/Scripts/Spawn/ElementFactory.cs`
- `Assets/Scripts/Spawn/ElementPool.cs`
- `Assets/Scripts/Spawn/InitialBoardSpawner.cs`

---

## Этап 4: Board State (Состояние доски)

### 4.1 BoardComponent
- [ ] `BoardComponent : MonoBehaviour` - хранит состояние
  - `ElementComponent[,] _elements` - элементы на сетке
  - `ElementComponent GetElement(Vector2Int pos)`
  - `void SetElement(Vector2Int pos, ElementComponent element)`
  - `void RemoveElement(Vector2Int pos)`
  - `bool IsEmpty(Vector2Int pos)`
  - `List<Vector2Int> GetEmptyPositions()`

### 4.2 Интеграция с Grid
- [ ] BoardComponent зависит от GridComponent
  - Инициализация массива по размерам сетки

**Файлы:**
- `Assets/Scripts/Board/BoardComponent.cs`

---

## Этап 5: Input System (Ввод)

### 5.1 InputDetector
- [ ] `InputDetector : MonoBehaviour` - обработка ввода
  - Raycast для определения тайла под курсором/тачем
  - `event Action<Vector2Int> OnElementSelected`
  - `event Action<Vector2Int, Vector2Int> OnSwapRequested`

### 5.2 Логика свайпа
- [ ] Определение направления свайпа
  - Первый клик → сохранить позицию
  - Драг/второй клик → определить направление
  - Валидация: только соседние ячейки (4 направления)

### 5.3 InputBlocker
- [ ] `InputBlocker` - блокировка ввода во время анимаций
  - `bool IsBlocked { get; set; }`
  - Проверка перед обработкой ввода

**Файлы:**
- `Assets/Scripts/Input/InputDetector.cs`
- `Assets/Scripts/Input/SwipeDirection.cs`
- `Assets/Scripts/Input/InputBlocker.cs`

---

## Этап 6: Swap System (Свап)

### 6.1 SwapHandler
- [ ] `SwapHandler : MonoBehaviour` - логика обмена
  - `[SerializeField] BoardComponent _board`
  - `bool CanSwap(Vector2Int a, Vector2Int b)` - проверка соседства
  - `void ExecuteSwap(Vector2Int a, Vector2Int b)`
  - `event Action<Vector2Int, Vector2Int> OnSwapStarted`
  - `event Action<Vector2Int, Vector2Int> OnSwapCompleted`
  - `event Action<Vector2Int, Vector2Int> OnSwapReverted`

### 6.2 SwapAnimator
- [ ] `SwapAnimator : MonoBehaviour` - анимация обмена (DOTween)
  - `[SerializeField] float _swapDuration = 0.25f`
  - `Tween AnimateSwap(ElementComponent a, ElementComponent b)`
  - Sequence: одновременное движение двух элементов

### 6.3 SwapValidator
- [ ] Проверка: привёл ли свап к матчу?
  - Если нет → реверс свапа
  - Если да → продолжить цикл

**Файлы:**
- `Assets/Scripts/Swap/SwapHandler.cs`
- `Assets/Scripts/Swap/SwapAnimator.cs`
- `Assets/Scripts/Swap/SwapValidator.cs`

---

## Этап 7: Match Detection (Поиск матчей)

### 7.1 MatchFinder
- [ ] `MatchFinder` - алгоритм поиска
  - `List<Match> FindAllMatches(BoardComponent board)`
  - `List<Match> FindMatchesAt(Vector2Int pos)` - проверка конкретной позиции
  - Горизонтальный проход: 3+ подряд
  - Вертикальный проход: 3+ подряд
  - Объединение пересекающихся матчей

### 7.2 Match структура
- [ ] `Match` - данные о найденном совпадении
  - `List<Vector2Int> positions`
  - `ElementType type`
  - `int Count => positions.Count`
  - `bool IsHorizontal`, `bool IsVertical`, `bool IsCross`

### 7.3 MatchHighlighter (debug)
- [ ] Визуальная подсветка найденных матчей для отладки

**Файлы:**
- `Assets/Scripts/Match/MatchFinder.cs`
- `Assets/Scripts/Match/Match.cs`
- `Assets/Scripts/Match/MatchHighlighter.cs`

---

## Этап 8: Destroy System (Уничтожение)

### 8.1 DestroyHandler
- [ ] `DestroyHandler : MonoBehaviour`
  - `void DestroyMatches(List<Match> matches)`
  - Удаление из BoardComponent
  - Возврат в пул (ElementPool)
  - `event Action<List<Match>> OnMatchesDestroyed`

### 8.2 DestroyAnimator
- [ ] `DestroyAnimator : MonoBehaviour` - анимации уничтожения
  - `[SerializeField] float _destroyDuration = 0.3f`
  - `Sequence AnimateDestroy(List<ElementComponent> elements)`
  - Эффекты: Scale → 0, Fade out, Particle burst
  - DOTween Sequence для синхронизации

### 8.3 Particle Effects (опционально)
- [ ] VFX при уничтожении элементов
  - ParticleSystem на каждый тип элемента

**Файлы:**
- `Assets/Scripts/Destroy/DestroyHandler.cs`
- `Assets/Scripts/Destroy/DestroyAnimator.cs`

---

## Этап 9: Fall System (Падение)

### 9.1 FallCalculator
- [ ] `FallCalculator` - расчёт падения
  - `List<FallData> CalculateFalls(BoardComponent board)`
  - Для каждого столбца снизу вверх:
    - Найти пустые ячейки
    - Определить кто куда падает

### 9.2 FallData
- [ ] `FallData` - struct
  - `ElementComponent element`
  - `Vector2Int from`
  - `Vector2Int to`
  - `int distance`

### 9.3 FallHandler
- [ ] `FallHandler : MonoBehaviour`
  - `void ExecuteFalls(List<FallData> falls)`
  - Обновление BoardComponent
  - `event Action OnFallsCompleted`

### 9.4 FallAnimator
- [ ] `FallAnimator : MonoBehaviour` - анимация падения
  - `[SerializeField] float _fallSpeed = 10f` (units/sec)
  - `[SerializeField] AnimationCurve _fallCurve` (ease)
  - `Sequence AnimateFalls(List<FallData> falls)`
  - Bounce эффект при приземлении

**Файлы:**
- `Assets/Scripts/Fall/FallCalculator.cs`
- `Assets/Scripts/Fall/FallData.cs`
- `Assets/Scripts/Fall/FallHandler.cs`
- `Assets/Scripts/Fall/FallAnimator.cs`

---

## Этап 10: Refill System (Заполнение сверху)

### 10.1 RefillCalculator
- [ ] `RefillCalculator` - определение новых элементов
  - `List<RefillData> CalculateRefills(BoardComponent board)`
  - Для каждого пустого места в верхнем ряду:
    - Создать новый элемент
    - Начальная позиция выше сетки

### 10.2 RefillData
- [ ] `RefillData` - struct
  - `ElementType type`
  - `Vector2Int targetPosition`
  - `Vector3 spawnPosition` (выше сетки)

### 10.3 RefillHandler
- [ ] `RefillHandler : MonoBehaviour`
  - `[SerializeField] ElementFactory _factory`
  - `void ExecuteRefills(List<RefillData> refills)`
  - `event Action OnRefillsCompleted`

**Файлы:**
- `Assets/Scripts/Refill/RefillCalculator.cs`
- `Assets/Scripts/Refill/RefillData.cs`
- `Assets/Scripts/Refill/RefillHandler.cs`

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
