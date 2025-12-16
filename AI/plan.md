# Match3 — План разработки базовых механик

## Конфигурация проекта

- **Размер сетки:** настраиваемый
- **Типов элементов:** 5
- **Object Pooling:** позже
- **Анимации:** DOTween

---

## Этап 1: Grid System (Сетка)

### 1.1 GridConfig (ScriptableObject)
- [ ] Ширина сетки (int)
- [ ] Высота сетки (int)
- [ ] Размер ячейки в world units (float)
- [ ] Offset для центрирования (Vector2)

### 1.2 Cell (plain class)
- [ ] Координаты (int x, int y)
- [ ] Ссылка на элемент (ElementComponent)
- [ ] Свойство IsEmpty

### 1.3 GridComponent (MonoBehaviour)
- [ ] Создание логической сетки Cell[,]
- [ ] Метод GetCell(x, y)
- [ ] Метод GridToWorld(x, y) → Vector3
- [ ] Метод WorldToGrid(Vector3) → (x, y)
- [ ] Gizmos для визуализации в Editor

**Результат:** Пустая сетка отображается в Scene view

---

## Этап 2: Elements (Элементы)

### 2.1 ElementType (Enum)
- [ ] Red, Blue, Green, Yellow, Purple

### 2.2 ElementConfig (ScriptableObject)
- [ ] Массив спрайтов по типам
- [ ] Префаб элемента

### 2.3 ElementComponent (MonoBehaviour)
- [ ] ElementType тип
- [ ] Grid position (int x, int y)
- [ ] Ссылка на SpriteRenderer
- [ ] Метод SetType(ElementType)
- [ ] Метод SetGridPosition(x, y)
- [ ] Event OnDestroyed

### 2.4 ElementFactory (MonoBehaviour)
- [ ] Ссылка на ElementConfig
- [ ] Метод CreateElement(ElementType, Vector3 position)
- [ ] Метод CreateRandomElement(Vector3 position)
- [ ] Метод DestroyElement(ElementComponent)

**Результат:** Можно создавать элементы разных типов

---

## Этап 3: Spawn & Board Init (Спаун)

### 3.1 SpawnComponent (MonoBehaviour)
- [ ] Ссылки на GridComponent, ElementFactory
- [ ] Метод SpawnAt(int x, int y, ElementType)
- [ ] Метод SpawnRandomAt(int x, int y)
- [ ] Позиция спауна над сеткой для падения

### 3.2 BoardInitializer (MonoBehaviour)
- [ ] Заполнение сетки при старте
- [ ] Алгоритм предотвращения начальных матчей:
  - Проверка 2 элементов слева
  - Проверка 2 элементов снизу
  - Исключение типов из рандома

**Результат:** Поле заполняется без матчей при старте

---

## Этап 4: Match Detection (Поиск совпадений)

### 4.1 MatchData (plain class)
- [ ] List<Cell> cells — ячейки в матче
- [ ] ElementType type
- [ ] bool IsHorizontal
- [ ] int Length

### 4.2 MatchDetector (plain class или MonoBehaviour)
- [ ] Метод FindMatches(GridComponent) → List<MatchData>
- [ ] Приватный метод FindHorizontalMatches()
- [ ] Приватный метод FindVerticalMatches()
- [ ] Объединение пересекающихся матчей

**Результат:** Логика поиска работает (проверяем через тесты/логи)

---

## Этап 5: Input & Swap (Ввод и обмен)

### 5.1 InputComponent (MonoBehaviour)
- [ ] Обработка mouse down/up
- [ ] Определение начальной ячейки
- [ ] Определение направления свайпа (4 направления)
- [ ] Минимальная дистанция свайпа
- [ ] Event OnSwapRequested(Cell from, Cell to)

### 5.2 SwapComponent (MonoBehaviour)
- [ ] Валидация: ячейки соседние
- [ ] Валидация: обе ячейки не пустые
- [ ] Метод Swap(Cell a, Cell b)
- [ ] Метод SwapBack(Cell a, Cell b)
- [ ] Обновление данных в Grid и Elements
- [ ] Events: OnSwapStarted, OnSwapCompleted

### 5.3 SwapAnimationComponent (MonoBehaviour)
- [ ] DOTween анимация перемещения
- [ ] Настройка длительности
- [ ] Callback по завершении

**Результат:** Элементы меняются местами по свайпу

---

## Этап 6: Destruction (Уничтожение)

### 6.1 DestroyComponent (MonoBehaviour)
- [ ] Метод DestroyMatches(List<MatchData>)
- [ ] Очистка ячеек в Grid
- [ ] Запуск анимаций уничтожения
- [ ] Event OnDestructionComplete

### 6.2 DestroyAnimationComponent (MonoBehaviour)
- [ ] DOTween: scale to 0
- [ ] DOTween: fade out
- [ ] Настройка длительности
- [ ] Callback по завершении всех анимаций

**Результат:** Матчи уничтожаются с анимацией

---

## Этап 7: Gravity & Refill (Падение и заполнение)

### 7.1 GravityComponent (MonoBehaviour)
- [ ] Метод ProcessGravity() → List<FallData>
- [ ] Сканирование снизу вверх по колонкам
- [ ] FallData: element, fromY, toY
- [ ] Обновление Grid после падения

### 7.2 FallAnimationComponent (MonoBehaviour)
- [ ] DOTween анимация падения
- [ ] Скорость или время на клетку
- [ ] Event OnFallComplete

### 7.3 RefillComponent (MonoBehaviour)
- [ ] Подсчёт пустых ячеек сверху в каждой колонке
- [ ] Спаун новых элементов выше сетки
- [ ] Запуск падения новых элементов

**Результат:** Элементы падают, пустоты заполняются

---

## Этап 8: Game Loop (Игровой цикл)

### 8.1 BoardState (Enum)
- [ ] Idle
- [ ] WaitingForInput
- [ ] Swapping
- [ ] CheckingMatches
- [ ] Destroying
- [ ] Falling
- [ ] Refilling

### 8.2 GameLoopController (MonoBehaviour)
- [ ] Текущий state
- [ ] Блокировка ввода в не-Idle состояниях
- [ ] Основной цикл:

```
[Idle]
   ↓ input
[Swapping] → animate swap
   ↓
[CheckingMatches]
   ↓ no match          ↓ match found
[Swapping Back]     [Destroying] → animate
   ↓                    ↓
[Idle]              [Falling] → animate
                        ↓
                    [Refilling] → spawn + animate
                        ↓
                    [CheckingMatches] → loop until no matches
                        ↓
                    [Idle]
```

### 8.3 Интеграция компонентов
- [ ] Связать все компоненты через события
- [ ] Тестирование полного цикла

**Результат:** Полностью играбельный Match3

---

## Структура файлов

```
Assets/
├── Scripts/
│   ├── Grid/
│   │   ├── GridConfig.cs
│   │   ├── Cell.cs
│   │   └── GridComponent.cs
│   ├── Elements/
│   │   ├── ElementType.cs
│   │   ├── ElementConfig.cs
│   │   ├── ElementComponent.cs
│   │   └── ElementFactory.cs
│   ├── Spawn/
│   │   ├── SpawnComponent.cs
│   │   └── BoardInitializer.cs
│   ├── Match/
│   │   ├── MatchData.cs
│   │   └── MatchDetector.cs
│   ├── Input/
│   │   └── InputComponent.cs
│   ├── Swap/
│   │   ├── SwapComponent.cs
│   │   └── SwapAnimationComponent.cs
│   ├── Destroy/
│   │   ├── DestroyComponent.cs
│   │   └── DestroyAnimationComponent.cs
│   ├── Gravity/
│   │   ├── GravityComponent.cs
│   │   ├── FallAnimationComponent.cs
│   │   └── RefillComponent.cs
│   └── GameLoop/
│       ├── BoardState.cs
│       └── GameLoopController.cs
├── Configs/
│   ├── GridConfig.asset
│   └── ElementConfig.asset
└── Prefabs/
    └── Element.prefab
```

---

## Прогресс

| Этап | Название | Статус |
|------|----------|--------|
| 1 | Grid System | ✅ Готово |
| 2 | Elements | ✅ Готово |
| 3 | Spawn & Board Init | ✅ Готово |
| 4 | Match Detection | ⬜ Не начат |
| 5 | Input & Swap | ⬜ Не начат |
| 6 | Destruction | ⬜ Не начат |
| 7 | Gravity & Refill | ⬜ Не начат |
| 8 | Game Loop | ⬜ Не начат |
