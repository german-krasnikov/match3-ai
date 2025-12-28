# Step 4: SPAWN SYSTEM - Система спауна элементов

> **Статус:** ✅ РЕАЛИЗОВАНО
> **Зависимости:** Core (интерфейсы), Grid (GridComponent), Elements (ElementFactoryComponent)
> **Выход:** SpawnComponent — заполнение сетки без начальных матчей

---

## Обзор

SpawnComponent отвечает за:
1. Начальное заполнение сетки элементами
2. Спаун новых элементов сверху (для гравитации)
3. Гарантию отсутствия матчей при начальном заполнении

---

## Архитектура

```
SpawnComponent : MonoBehaviour, ISpawnSystem
    ├── Зависимости (SerializeField)
    │   ├── GridComponent _grid
    │   └── ElementFactoryComponent _factory
    │
    ├── Публичные методы
    │   ├── FillGrid()           → Заполнить всю сетку
    │   ├── SpawnAt(pos)         → Спавн в конкретной позиции
    │   └── SpawnAtTop(column)   → Спавн над сеткой (для гравитации)
    │
    ├── Приватные методы
    │   └── GetRandomTypeWithoutMatch(pos) → Выбор типа без создания матча
    │
    └── События
        └── OnGridFilled         → Сетка заполнена
```

---

## Файловая структура

```
Assets/Scripts/Spawn/
└── SpawnComponent.cs
```

---

## Реализация

### SpawnComponent.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3.Spawn
{
    /// <summary>
    /// Система спауна элементов на сетке.
    /// Гарантирует отсутствие матчей при начальном заполнении.
    /// </summary>
    public class SpawnComponent : MonoBehaviour, ISpawnSystem
    {
        // === СОБЫТИЯ ===
        public event Action OnGridFilled;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactoryComponent _factory;

        // === КЭШИРОВАННЫЕ ДАННЫЕ ===
        private readonly List<ElementType> _allTypes = new()
        {
            ElementType.Red,
            ElementType.Green,
            ElementType.Blue,
            ElementType.Yellow,
            ElementType.Purple
        };

        private readonly List<ElementType> _availableTypes = new();

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Заполняет всю сетку элементами без создания матчей.
        /// Заполнение идёт снизу вверх, слева направо.
        /// </summary>
        public void FillGrid()
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    SpawnAt(pos);
                }
            }

            OnGridFilled?.Invoke();
        }

        /// <summary>
        /// Спавнит элемент в указанной позиции сетки.
        /// </summary>
        public ElementComponent SpawnAt(Vector2Int gridPos)
        {
            if (!_grid.IsValidPosition(gridPos))
                return null;

            if (_grid.GetElementAt(gridPos) != null)
                return null;

            var type = GetRandomTypeWithoutMatch(gridPos);
            var worldPos = _grid.GridToWorld(gridPos);
            var element = _factory.Create(type, worldPos);

            element.GridPosition = gridPos;
            _grid.SetElementAt(gridPos, element);

            return element;
        }

        /// <summary>
        /// Спавнит элемент над сеткой для последующего падения.
        /// Используется системой гравитации.
        /// </summary>
        public ElementComponent SpawnAtTop(int column)
        {
            // Позиция над сеткой (Height = первая строка над видимой областью)
            var spawnGridPos = new Vector2Int(column, _grid.Height);
            var worldPos = _grid.GridToWorld(spawnGridPos);

            // Для верхнего спауна берём случайный тип без проверки матчей
            // (матчи проверятся после падения)
            var type = _allTypes[Random.Range(0, _allTypes.Count)];
            var element = _factory.Create(type, worldPos);

            return element;
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Выбирает случайный тип элемента, который не создаст матч в данной позиции.
        /// Проверяет 2 элемента слева и 2 элемента снизу.
        /// </summary>
        private ElementType GetRandomTypeWithoutMatch(Vector2Int pos)
        {
            // Начинаем со всех доступных типов
            _availableTypes.Clear();
            _availableTypes.AddRange(_allTypes);

            // Проверка 2 элементов слева
            if (pos.x >= 2)
            {
                var left1 = _grid.GetElementAt(pos + Vector2Int.left);
                var left2 = _grid.GetElementAt(pos + Vector2Int.left * 2);

                if (left1 != null && left2 != null && left1.Type == left2.Type)
                {
                    _availableTypes.Remove(left1.Type);
                }
            }

            // Проверка 2 элементов снизу
            if (pos.y >= 2)
            {
                var down1 = _grid.GetElementAt(pos + Vector2Int.down);
                var down2 = _grid.GetElementAt(pos + Vector2Int.down * 2);

                if (down1 != null && down2 != null && down1.Type == down2.Type)
                {
                    _availableTypes.Remove(down1.Type);
                }
            }

            // Выбираем случайный из оставшихся
            return _availableTypes[Random.Range(0, _availableTypes.Count)];
        }
    }
}
```

---

## STUB-ы для независимого тестирования

До готовности GridComponent и ElementFactoryComponent используем заглушки:

```csharp
#if UNITY_EDITOR
namespace Match3.Spawn
{
    /// <summary>
    /// STUB: Заглушка для тестирования SpawnComponent без реальных зависимостей.
    /// Удалить после интеграции с реальными компонентами.
    /// </summary>
    public class SpawnComponentStubTest : MonoBehaviour
    {
        // STUB: Grid
        private IGridElement[,] _stubGrid = new IGridElement[8, 8];

        private IGridElement StubGetElementAt(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= 8 || pos.y < 0 || pos.y >= 8)
                return null;
            return _stubGrid[pos.x, pos.y];
        }

        private void StubSetElementAt(Vector2Int pos, IGridElement el)
        {
            if (pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8)
                _stubGrid[pos.x, pos.y] = el;
        }

        private Vector3 StubGridToWorld(Vector2Int gridPos)
            => new Vector3(gridPos.x, gridPos.y, 0);

        private bool StubIsValidPosition(Vector2Int pos)
            => pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;

        // STUB: Factory
        private ElementComponent StubCreate(ElementType type, Vector3 pos)
        {
            Debug.Log($"[STUB] Created {type} at {pos}");
            return null;
        }
    }
}
#endif
```

---

## Алгоритм GetRandomTypeWithoutMatch

### Визуализация проверки

```
Заполнение сетки (снизу вверх, слева направо):

y=2  [?] ← текущая позиция (2,2)
y=1  [B]    Проверяем: left1=(1,2), left2=(0,2) — ещё не заполнены
y=0  [R][G][B][?]...
     x=0 1  2  3

При заполнении (3,0):
- Проверяем left1=(2,0)=B, left2=(1,0)=G → разные, OK
- Проверяем down — нет элементов (y=0)
- Все типы доступны

При заполнении (2,2):
         [?] ← (2,2)
    [R]  [R]
    [G]  [B]  [R]
- left1=(1,2)=R, left2=(0,2)=R → совпадают! Исключаем Red
- down1=(2,1)=B, down2=(2,0)=R → разные, OK
- Доступны: Green, Blue, Yellow, Purple
```

### Почему порядок важен

Заполнение **снизу вверх, слева направо** гарантирует, что:
- При проверке `left` — элементы слева уже существуют
- При проверке `down` — элементы снизу уже существуют

---

## Интеграция с другими системами

### 1. С GridComponent (Step 2)

```csharp
// SpawnComponent использует:
_grid.Width           // Ширина сетки
_grid.Height          // Высота сетки
_grid.IsValidPosition // Проверка границ
_grid.GridToWorld     // Конвертация координат
_grid.GetElementAt    // Получение элемента
_grid.SetElementAt    // Установка элемента
```

### 2. С ElementFactoryComponent (Step 3)

```csharp
// SpawnComponent использует:
_factory.Create(type, worldPos)  // Создание элемента
```

### 3. С GravitySystem (Step 8) — будущее

```csharp
// GravitySystem будет вызывать:
_spawn.SpawnAtTop(column)  // Спавн новых элементов сверху
```

### 4. С GameLoop (Step 9) — будущее

```csharp
// GameLoop будет вызывать:
_spawn.FillGrid()  // Начальное заполнение
_spawn.OnGridFilled += StartGame;  // Подписка на событие
```

---

## Подзадачи

### Этап 1: Базовая структура
- [x] Создать папку `Assets/Scripts/Spawn/`
- [x] Создать `SpawnComponent.cs` с базовой структурой
- [x] Добавить SerializeField зависимости

### Этап 2: Основная логика
- [x] Реализовать `GetRandomTypeWithoutMatch()`
- [x] Реализовать `SpawnAt()` с интеграцией Grid + Factory
- [x] Реализовать `FillGrid()` с правильным порядком обхода

### Этап 3: Поддержка гравитации
- [x] Реализовать `SpawnAtTop()` для спауна над сеткой

### Этап 4: Тестирование
- [x] Создать `SpawnTester.cs` для тестирования
- [x] Создать `SpawnSceneSetup.cs` Editor скрипт
- [ ] Визуально проверить отсутствие матчей (нет 3+ одинаковых в ряд)
- [ ] Проверить что все 64 ячейки заполнены

---

## Тестовый сценарий

```csharp
public class SpawnTestScene : MonoBehaviour
{
    [SerializeField] private SpawnComponent _spawn;

    private void Start()
    {
        _spawn.OnGridFilled += OnGridFilled;
        _spawn.FillGrid();
    }

    private void OnGridFilled()
    {
        Debug.Log("Grid filled! Check for matches visually.");
    }
}
```

### Критерии успеха

1. ✅ Все 64 ячейки заполнены элементами
2. ✅ Нет горизонтальных линий из 3+ одинаковых элементов
3. ✅ Нет вертикальных линий из 3+ одинаковых элементов
4. ✅ Каждый запуск даёт разное расположение (рандом)
5. ✅ SpawnAtTop() создаёт элемент выше видимой сетки

---

## Edge Cases

### 1. Все типы исключены (невозможно в Match-3)

При 5 типах и проверке только 2 направлений (left, down) максимум можно исключить 2 типа → всегда остаётся минимум 3 типа.

### 2. Граничные позиции

- `pos.x < 2` → не проверяем left (нет 2 элементов слева)
- `pos.y < 2` → не проверяем down (нет 2 элементов снизу)

### 3. Повторный вызов FillGrid()

Текущая реализация пропускает занятые ячейки (`if (_grid.GetElementAt(gridPos) != null) return null`). Для полного перезаполнения сначала нужно очистить сетку.

---

## Оптимизации (на будущее)

1. **Pre-allocated list** — `_availableTypes` уже переиспользуется
2. **Избегаем аллокаций** — не создаём новые списки в каждом вызове
3. **Кэш типов** — `_allTypes` инициализируется один раз

---

## Чеклист перед интеграцией

- [x] Namespace: `Match3.Spawn`
- [x] Класс реализует `ISpawnSystem`
- [x] Все зависимости через `[SerializeField]`
- [x] События используют `?.Invoke()`
- [x] Нет `GetComponent` в runtime-методах
- [x] Нет публичных полей (только SerializeField + свойства)

---

## Созданные файлы

```
Assets/Scripts/Spawn/
├── SpawnComponent.cs      # Основной компонент
└── SpawnTester.cs         # Тестовый компонент

Assets/Scripts/Editor/
└── SpawnSceneSetup.cs     # Editor utility
```

## Инструкция по тестированию

1. Убедись что Grid и Elements настроены (Menu: Match3/Setup/1 и /2)
2. Запусти `Match3/Setup/3. Setup Spawn System`
3. Play → нажми Space → сетка заполнится
4. Визуально проверь что нет 3+ одинаковых в ряд
