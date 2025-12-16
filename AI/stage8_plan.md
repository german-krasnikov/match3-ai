# Этап 8: Game Loop — Детальный план реализации

## Анализ текущего состояния

### Проблема
`SwapComponent` сейчас содержит логику игрового цикла (строки 78-154):
- Проверка матчей после свапа
- Вызов DestroyComponent
- Вызов GravityComponent + RefillComponent
- Управление FallAnimation

Это нарушает **Single Responsibility** — SwapComponent должен только менять элементы местами.

### Что отсутствует
1. **Каскадные матчи** — после падения нет повторной проверки
2. **Явный BoardState** — состояние неявно хранится в `_isSwapping`
3. **Централизованное управление** — логика размазана по SwapComponent

---

## Архитектура решения

```
                    GameLoopController
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
        InputComponent  [State]    [Orchestration]
              │            │            │
              │            │     ┌──────┴──────┐
              │            │     ▼             ▼
              │            │  SwapComponent  MatchFlow
              │            │     │             │
              │            │     │    ┌────────┴────────┐
              │            │     │    ▼        ▼        ▼
              │            │     │  Destroy  Gravity  Refill
              └────────────┴─────┴─────────────────────────┘
```

---

## Файлы для создания

### 8.1 `BoardState.cs` — Enum состояний
```
Assets/Scripts/GameLoop/BoardState.cs
```

| State | Описание |
|-------|----------|
| `Idle` | Ожидание ввода |
| `Swapping` | Анимация обмена |
| `CheckingMatches` | Проверка совпадений |
| `Destroying` | Анимация уничтожения |
| `Falling` | Анимация падения |
| `Refilling` | Спаун новых элементов |

### 8.2 `GameLoopController.cs` — Главный контроллер
```
Assets/Scripts/GameLoop/GameLoopController.cs
```

**Ответственность:**
- Хранит текущий `BoardState`
- Блокирует/разблокирует ввод
- Оркестрирует последовательность: Swap → Match → Destroy → Gravity → Refill → (loop)
- Реализует каскадную проверку матчей

---

## Детальная реализация

### 8.1 BoardState.cs

```csharp
public enum BoardState
{
    Idle,
    Swapping,
    CheckingMatches,
    Destroying,
    Falling,
    Refilling
}
```

**Файл:** ~10 строк

---

### 8.2 GameLoopController.cs

#### Зависимости (SerializeField)
```csharp
[SerializeField] private InputComponent _input;
[SerializeField] private SwapComponent _swap;
[SerializeField] private SwapAnimationComponent _swapAnimation;
[SerializeField] private MatchDetectorComponent _matchDetector;
[SerializeField] private DestroyComponent _destroy;
[SerializeField] private GravityComponent _gravity;
[SerializeField] private RefillComponent _refill;
[SerializeField] private FallAnimationComponent _fallAnimation;
```

#### Публичные члены
```csharp
public event Action<BoardState> OnStateChanged;
public BoardState CurrentState { get; private set; }
```

#### Приватные члены
```csharp
private Cell _swapCellA;
private Cell _swapCellB;
```

#### Методы

| Метод | Описание |
|-------|----------|
| `OnEnable()` | Подписка на `_input.OnSwapRequested` |
| `OnDisable()` | Отписка |
| `SetState(BoardState)` | Смена состояния + событие + управление `_input.IsEnabled` |
| `OnSwapRequested(Cell, Cell)` | Entry point — начало цикла |
| `ExecuteSwap()` | Выполнить свап, запустить анимацию |
| `OnSwapAnimationComplete()` | После анимации → CheckMatches |
| `CheckMatches()` | Найти матчи или SwapBack |
| `OnSwapBackComplete()` | После swap back → Idle |
| `ProcessMatches(List<MatchData>)` | Destroying state |
| `OnDestroyComplete()` | После уничтожения → Gravity |
| `ProcessGravity()` | Falling state |
| `OnFallComplete()` | После падения → повторная проверка или Idle |

#### Диаграмма состояний (реализация в коде)

```
[Idle]
    │ OnSwapRequested
    ▼
[Swapping] ─── SwapAnimation.AnimateSwap()
    │ OnSwapAnimationComplete
    ▼
[CheckingMatches]
    │
    ├── no matches ──► SwapBack ──► [Swapping] ──► OnSwapBackComplete ──► [Idle]
    │
    └── matches found
            │
            ▼
      [Destroying] ─── DestroyComponent.DestroyMatches()
            │ OnDestroyComplete
            ▼
      [Falling] ─── Gravity + Refill + FallAnimation
            │ OnFallComplete
            ▼
      [CheckingMatches] ◄─── LOOP until no matches
            │
            ▼
         [Idle]
```

**Файл:** ~120-150 строк

---

### 8.3 Рефакторинг SwapComponent

#### Удалить
- Всю логику после `OnSwapAnimationComplete()` (match detection, destroy, gravity)
- Ссылки на `_matchDetector`, `_destroy`, `_gravity`, `_fallAnimation`, `_refill`
- Методы `OnDestructionComplete()`, `OnFallComplete()`

#### Оставить
- `RequestSwap(Cell, Cell)` — только валидация + swap data + start animation
- `SwapBack(Cell, Cell)` — обратный свап
- `SwapCellData(Cell, Cell)` — обмен данными
- Events: `OnSwapStarted`, `OnSwapCompleted`

#### Изменить
- `OnSwapAnimationComplete()` — просто вызвать `OnSwapCompleted?.Invoke(a, b, true)`
- `OnSwapBackComplete()` — просто вызвать `OnSwapCompleted?.Invoke(a, b, false)`

**Новый размер:** ~60-70 строк (вместо 175)

---

### 8.4 Изменения в InputComponent

#### Удалить
- Подписку на `_swap.OnSwapStarted/OnSwapCompleted`
- Методы `OnSwapStarted()`, `OnSwapCompleted()`

#### Причина
Управление `IsEnabled` переходит к `GameLoopController` через `SetState()`.

---

## Порядок реализации (Постепенный рефакторинг)

### Фаза 1: Создание новых компонентов (без изменения существующих)

#### Шаг 1.1: BoardState.cs
- [ ] Enum с 6 состояниями
- [ ] Файл полностью независим

#### Шаг 1.2: GameLoopController.cs — базовая версия
- [ ] Все SerializeField зависимости
- [ ] `CurrentState` property
- [ ] `SetState()` с логированием и управлением `_input.IsEnabled`
- [ ] `OnStateChanged` event
- [ ] **Пока НЕ подключаем к InputComponent**

**Проверка:** Компилируется, можно добавить на сцену

---

### Фаза 2: GameLoopController берёт управление (SwapComponent ещё работает)

#### Шаг 2.1: Добавить флаг в SwapComponent
```csharp
[SerializeField] private bool _useLegacyFlow = true;
```
Когда `false` — SwapComponent не вызывает match/destroy/gravity сам.

#### Шаг 2.2: GameLoopController подписывается на события
- [ ] Подписка на `_input.OnSwapRequested`
- [ ] Подписка на `_swap.OnSwapCompleted`
- [ ] Подписка на `_destroy.OnDestructionComplete`
- [ ] При получении события — `SetState()` + логирование

#### Шаг 2.3: Реализовать flow в GameLoopController
- [ ] `OnSwapRequested()` → вызывает `_swap.RequestSwap()`, state = Swapping
- [ ] `OnSwapCompleted()` → проверяет матчи
- [ ] `ProcessMatches()` / `HandleNoMatch()` → destroy или swap back
- [ ] `OnDestroyComplete()` → gravity + refill
- [ ] `OnFallComplete()` → **каскадная проверка** (НОВОЕ!)
- [ ] Цикл пока есть матчи

**Проверка:** `_useLegacyFlow = false`, полный цикл работает через GameLoopController

---

### Фаза 3: Очистка SwapComponent

#### Шаг 3.1: Удалить legacy код из SwapComponent
- [ ] Удалить `_useLegacyFlow`
- [ ] Удалить ссылки: `_matchDetector`, `_destroy`, `_gravity`, `_fallAnimation`, `_refill`
- [ ] Удалить методы: `OnDestructionComplete()`, `OnFallComplete()`
- [ ] Упростить `OnSwapAnimationComplete()` — только событие

#### Шаг 3.2: Очистить InputComponent
- [ ] Удалить подписку на `_swap.OnSwapStarted/OnSwapCompleted`
- [ ] Удалить `OnSwapStarted()`, `OnSwapCompleted()`
- [ ] `IsEnabled` теперь управляется только из GameLoopController

---

### Фаза 4: Тестирование и полировка

#### Шаг 4.1: Интеграционные тесты
- [ ] Обычный матч (3 в ряд)
- [ ] Swap без матча (возврат)
- [ ] Каскадный матч (матч после падения)
- [ ] Двойной/тройной каскад
- [ ] Быстрые свайпы (блокировка ввода)

#### Шаг 4.2: Защита от edge cases
- [ ] Лимит каскадов (20) с warning
- [ ] Null checks во всех callbacks
- [ ] Graceful degradation если компонент отсутствует

---

## Структура папки GameLoop

```
Assets/Scripts/GameLoop/
├── BoardState.cs           (~10 lines)
└── GameLoopController.cs   (~120-150 lines)
```

---

## События между компонентами

```
InputComponent
    └── OnSwapRequested(Cell, Cell) ──► GameLoopController

SwapComponent
    ├── OnSwapStarted(Cell, Cell)
    └── OnSwapCompleted(Cell, Cell, bool) ──► GameLoopController

SwapAnimationComponent
    └── callback in AnimateSwap() ──► GameLoopController

DestroyComponent
    └── OnDestructionComplete ──► GameLoopController

FallAnimationComponent
    └── callback in AnimateFalls() ──► GameLoopController
```

---

## Проверка готовности

После реализации должно работать:

1. **Базовый цикл:**
   - Свайп → матч → уничтожение → падение → idle ✓

2. **Swap back:**
   - Свайп без матча → возврат → idle ✓

3. **Каскад:**
   - Свайп → матч → падение → новый матч → повтор цикла ✓

4. **Блокировка ввода:**
   - Ввод заблокирован во всех состояниях кроме Idle ✓

---

## Риски и митигация

| Риск | Митигация |
|------|-----------|
| Гонки событий | Строгий порядок подписок в OnEnable, отписка в OnDisable |
| Infinite loop | Счётчик каскадов с лимитом (20) + warning log |
| Null refs | Проверки в SetState(), graceful degradation |
| Animation callbacks | Один callback на группу, не на элемент |

---

## Оценка сложности

| Компонент | Новые строки | Изменённые строки |
|-----------|-------------|-------------------|
| BoardState.cs | 10 | — |
| GameLoopController.cs | 130 | — |
| SwapComponent.cs | — | -100 (удаление) |
| InputComponent.cs | — | -15 (удаление) |

**Итого:** +140 новых, -115 удалённых = чистый прирост ~25 строк

При этом код станет значительно чище и модульнее.
