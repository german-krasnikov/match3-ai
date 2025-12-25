# Этап 3: Spawn System — Детальный план реализации

**Статус: ⏳ В РАБОТЕ**

## Обзор

Система создания и управления жизненным циклом элементов. Включает Object Pooling для производительности и алгоритм начального заполнения без матчей.

**Зависимости:**
- Этап 1 (Grid System) ✅
- Этап 2 (Elements) ✅

---

## Архитектура

```
ElementPool          ← Stack<ElementComponent>, Get/Release
    ↑
ElementFactory       ← Создаёт через пул, знает Database
    ↑
InitialBoardSpawner  ← Заполняет сетку без начальных матчей
```

**Unity Way принципы:**
- Каждый класс = одна ответственность
- Зависимости через `[SerializeField]`
- События для обратной связи

---

## 3.1 ElementPool.cs

**Путь:** `Assets/Scripts/Spawn/ElementPool.cs`

**Описание:** Object Pool для переиспользования элементов. Избегаем Instantiate/Destroy в рантайме.

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Elements;

namespace Match3.Spawn
{
    public class ElementPool : MonoBehaviour
    {
        [SerializeField] private ElementComponent _prefab;
        [SerializeField] private int _initialSize = 64;

        private Stack<ElementComponent> _pool;
        private Transform _poolContainer;

        public int PooledCount => _pool?.Count ?? 0;
        public int TotalCreated { get; private set; }

        private void Awake()
        {
            _pool = new Stack<ElementComponent>(_initialSize);
            _poolContainer = new GameObject("PooledElements").transform;
            _poolContainer.SetParent(transform);
            _poolContainer.gameObject.SetActive(false);

            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var element = CreateNew();
                Release(element);
            }
        }

        public ElementComponent Get()
        {
            var element = _pool.Count > 0 ? _pool.Pop() : CreateNew();
            element.gameObject.SetActive(true);
            return element;
        }

        public void Release(ElementComponent element)
        {
            element.ResetElement();
            element.transform.SetParent(_poolContainer);
            element.gameObject.SetActive(false);
            _pool.Push(element);
        }

        private ElementComponent CreateNew()
        {
            var element = Instantiate(_prefab, _poolContainer);
            TotalCreated++;
            return element;
        }
    }
}
```

**Дизайн-решения:**
| Решение | Почему |
|---------|--------|
| `Stack<T>` | O(1) Get/Release, LIFO лучше для кэша |
| `_initialSize = 64` | 8x8 = 64 клетки, хватит на всю доску |
| Скрытый контейнер | Пуленные объекты не мешают в Hierarchy |
| `Prewarm()` в Awake | Избегаем лагов при первом спауне |
| `TotalCreated` | Для дебага: сколько реально создали |

---

## 3.2 ElementFactory.cs

**Путь:** `Assets/Scripts/Spawn/ElementFactory.cs`

**Описание:** Фабрика создания элементов. Скрывает детали пулинга от потребителей.

```csharp
using System;
using UnityEngine;
using Match3.Elements;

namespace Match3.Spawn
{
    public class ElementFactory : MonoBehaviour
    {
        public event Action<ElementComponent> OnElementCreated;
        public event Action<ElementComponent> OnElementReturned;

        [SerializeField] private ElementPool _pool;
        [SerializeField] private ElementDatabase _database;

        public ElementComponent Create(ElementType type, Vector3 worldPos, Vector2Int gridPos)
        {
            var data = _database.GetData(type);
            if (data == null)
            {
                Debug.LogError($"[ElementFactory] No data for type: {type}");
                return null;
            }

            return CreateInternal(data, worldPos, gridPos);
        }

        public ElementComponent CreateRandom(Vector3 worldPos, Vector2Int gridPos)
        {
            var data = _database.GetRandom();
            return CreateInternal(data, worldPos, gridPos);
        }

        public ElementComponent CreateRandomExcluding(
            Vector3 worldPos,
            Vector2Int gridPos,
            params ElementType[] excluded)
        {
            var data = GetRandomExcluding(excluded);
            return CreateInternal(data, worldPos, gridPos);
        }

        public void Return(ElementComponent element)
        {
            OnElementReturned?.Invoke(element);
            _pool.Release(element);
        }

        private ElementComponent CreateInternal(ElementData data, Vector3 worldPos, Vector2Int gridPos)
        {
            var element = _pool.Get();
            element.transform.position = worldPos;
            element.Initialize(data, gridPos);
            OnElementCreated?.Invoke(element);
            return element;
        }

        private ElementData GetRandomExcluding(ElementType[] excluded)
        {
            // Простой подход: пробуем до 10 раз
            for (int i = 0; i < 10; i++)
            {
                var data = _database.GetRandom();
                if (!IsExcluded(data.Type, excluded))
                    return data;
            }
            // Fallback: любой
            return _database.GetRandom();
        }

        private bool IsExcluded(ElementType type, ElementType[] excluded)
        {
            foreach (var ex in excluded)
                if (ex == type) return true;
            return false;
        }
    }
}
```

**Дизайн-решения:**
| Решение | Почему |
|---------|--------|
| События `OnElementCreated/Returned` | Для подписчиков (статистика, звуки) |
| `CreateRandomExcluding` | Критично для алгоритма без матчей |
| Factory не знает про Grid | Single Responsibility — координаты получает извне |
| `params ElementType[]` | Удобный API: `CreateRandomExcluding(pos, Red, Blue)` |

---

## 3.3 InitialBoardSpawner.cs

**Путь:** `Assets/Scripts/Spawn/InitialBoardSpawner.cs`

**Описание:** Заполняет доску при старте. Гарантирует отсутствие начальных матчей.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;
using Match3.Elements;

namespace Match3.Spawn
{
    public class InitialBoardSpawner : MonoBehaviour
    {
        public event Action OnSpawnCompleted;

        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactory _factory;

        private ElementComponent[,] _spawnedElements;

        public ElementComponent[,] SpawnedElements => _spawnedElements;

        public void SpawnInitialBoard()
        {
            _spawnedElements = new ElementComponent[_grid.Width, _grid.Height];

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    SpawnAt(x, y);
                }
            }

            OnSpawnCompleted?.Invoke();
        }

        private void SpawnAt(int x, int y)
        {
            var gridPos = new Vector2Int(x, y);
            var worldPos = _grid.GridToWorld(gridPos);
            var excluded = GetExcludedTypes(x, y);

            var element = excluded.Count > 0
                ? _factory.CreateRandomExcluding(worldPos, gridPos, excluded.ToArray())
                : _factory.CreateRandom(worldPos, gridPos);

            _spawnedElements[x, y] = element;
        }

        private List<ElementType> GetExcludedTypes(int x, int y)
        {
            var excluded = new List<ElementType>(2);

            // Проверка 2 слева: если два одинаковых — исключить этот тип
            if (x >= 2)
            {
                var left1 = _spawnedElements[x - 1, y];
                var left2 = _spawnedElements[x - 2, y];
                if (left1 != null && left2 != null && left1.Type == left2.Type)
                    excluded.Add(left1.Type);
            }

            // Проверка 2 снизу: если два одинаковых — исключить этот тип
            if (y >= 2)
            {
                var down1 = _spawnedElements[x, y - 1];
                var down2 = _spawnedElements[x, y - 2];
                if (down1 != null && down2 != null && down1.Type == down2.Type)
                    excluded.Add(down1.Type);
            }

            return excluded;
        }
    }
}
```

**Алгоритм "без начальных матчей":**
```
Для каждой ячейки (x, y) слева-направо, снизу-вверх:
  1. Проверить 2 ячейки слева: если AA — исключить A
  2. Проверить 2 ячейки снизу: если BB — исключить B
  3. Создать элемент случайного типа, кроме исключённых
```

**Почему это работает:**
- При 5 типах максимум исключаем 2 → остаётся минимум 3 варианта
- Заполняем слева-направо, снизу-вверх → левые и нижние соседи уже существуют
- Матчи 3+ невозможны, т.к. третий элемент всегда отличается

**Визуализация:**
```
Заполнение:
(0,0) → (1,0) → (2,0) → ...
  ↓
(0,1) → (1,1) → (2,1) → ...
  ↓
...

При спауне (2,1):
- Проверяем (1,1) и (0,1) — горизонталь
- Проверяем (2,0) и (2,-1) — вертикаль (нет, y<0)
```

---

## 3.4 Интеграция компонентов

**Порядок инициализации:**
```
GridComponent.Awake()
    ↓
ElementPool.Awake() → Prewarm
    ↓
InitialBoardSpawner.Start() или ручной вызов SpawnInitialBoard()
```

**Wiring в сцене:**

```
[GameManager] (Root)
├── GridComponent
│
├── [SpawnSystem]
│   ├── ElementPool
│   │   └── _prefab → Element.prefab
│   │
│   ├── ElementFactory
│   │   ├── _pool → ElementPool
│   │   └── _database → ElementDatabase.asset
│   │
│   └── InitialBoardSpawner
│       ├── _grid → GridComponent
│       └── _factory → ElementFactory
│
└── [Elements] (контейнер для активных элементов)
```

---

## 3.5 Дополнительно: SpawnedElements → BoardComponent

**Важно:** `InitialBoardSpawner` временно хранит `ElementComponent[,]`.

В Этапе 4 появится `BoardComponent`, который заберёт это состояние:

```csharp
// Этап 4: BoardComponent.cs (preview)
public class BoardComponent : MonoBehaviour
{
    [SerializeField] private InitialBoardSpawner _initialSpawner;

    private void Start()
    {
        _initialSpawner.SpawnInitialBoard();
        _elements = _initialSpawner.SpawnedElements;
    }
}
```

Или через событие:
```csharp
_initialSpawner.OnSpawnCompleted += () => _elements = _initialSpawner.SpawnedElements;
```

---

## 3.6 Удаление тестового кода

После реализации Этапа 3:
- [x] Удалить `ElementSpawnerTest.cs`
- [x] Удалить тестовые объекты со сцены

---

## 3.7 Editor Setup (опционально)

**Путь:** `Assets/Scripts/Editor/SpawnSystemSetup.cs`

Добавляет меню для быстрой настройки:

| Меню | Действие |
|------|----------|
| `Match3 → Setup → Create Spawn System` | Создаёт GameObject со всеми компонентами |

```csharp
using UnityEngine;
using UnityEditor;
using Match3.Spawn;
using Match3.Elements;
using Match3.Grid;

namespace Match3.Editor
{
    public static class SpawnSystemSetup
    {
        [MenuItem("Match3/Setup/Create Spawn System")]
        public static void CreateSpawnSystem()
        {
            var root = new GameObject("SpawnSystem");

            var pool = new GameObject("ElementPool").AddComponent<ElementPool>();
            pool.transform.SetParent(root.transform);

            var factory = new GameObject("ElementFactory").AddComponent<ElementFactory>();
            factory.transform.SetParent(root.transform);

            var spawner = new GameObject("InitialBoardSpawner").AddComponent<InitialBoardSpawner>();
            spawner.transform.SetParent(root.transform);

            // Wire references через SerializedObject
            // ...

            Selection.activeGameObject = root;
            Debug.Log("[SpawnSystemSetup] Created SpawnSystem hierarchy");
        }
    }
}
```

---

## 3.8 Порядок реализации

| # | Задача | Файл | Зависимости |
|---|--------|------|-------------|
| 1 | Создать `ElementPool.cs` | `Scripts/Spawn/ElementPool.cs` | Element.prefab |
| 2 | Создать `ElementFactory.cs` | `Scripts/Spawn/ElementFactory.cs` | ElementPool, ElementDatabase |
| 3 | Создать `InitialBoardSpawner.cs` | `Scripts/Spawn/InitialBoardSpawner.cs` | GridComponent, ElementFactory |
| 4 | Настроить сцену | — | Wiring references |
| 5 | Тестирование | — | Play Mode |
| 6 | Удалить `ElementSpawnerTest.cs` | — | После проверки |

---

## 3.9 Тестирование

### Автоматический тест в Play Mode
1. Запустить сцену
2. Проверить: все 64 ячейки заполнены
3. Проверить: нет матчей 3+ (визуально или скриптом)

### Debug-скрипт для проверки матчей
```csharp
#if UNITY_EDITOR
[ContextMenu("Validate No Matches")]
private void ValidateNoMatches()
{
    for (int x = 0; x < _grid.Width; x++)
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            var current = _spawnedElements[x, y];
            if (current == null) continue;

            // Проверка горизонтали
            if (x >= 2)
            {
                var a = _spawnedElements[x - 1, y];
                var b = _spawnedElements[x - 2, y];
                if (a?.Type == current.Type && b?.Type == current.Type)
                    Debug.LogError($"Horizontal match at ({x},{y})");
            }

            // Проверка вертикали
            if (y >= 2)
            {
                var a = _spawnedElements[x, y - 1];
                var b = _spawnedElements[x, y - 2];
                if (a?.Type == current.Type && b?.Type == current.Type)
                    Debug.LogError($"Vertical match at ({x},{y})");
            }
        }
    }
    Debug.Log("Validation complete");
}
#endif
```

### Проверка пулинга
```csharp
Debug.Log($"Pool: {_pool.PooledCount} available, {_pool.TotalCreated} total created");
```

---

## 3.10 Чеклист готовности

- [ ] `ElementPool` создаёт и переиспользует элементы
- [ ] `ElementFactory.CreateRandom()` возвращает элемент из пула
- [ ] `ElementFactory.CreateRandomExcluding()` исключает указанные типы
- [ ] `InitialBoardSpawner` заполняет всю сетку
- [ ] Нет начальных матчей 3+
- [ ] Пул prewarm = 64 элемента
- [ ] Удалён `ElementSpawnerTest.cs`
- [ ] Все references в Inspector настроены

---

## 3.11 Возможные проблемы

| Проблема | Решение |
|----------|---------|
| Элементы не видны | Проверить Sorting Layer = "Elements" |
| NullReference при спауне | Проверить wiring Factory → Pool → Prefab |
| Есть начальные матчи | Проверить порядок обхода (y, потом x) |
| Лаг при запуске | Увеличить prewarm, уменьшить размер спрайтов |

---

## 3.12 Связь с последующими этапами

| Этап | Использует |
|------|------------|
| 4: BoardComponent | `InitialBoardSpawner.SpawnedElements` |
| 8: Destroy | `ElementFactory.Return()` |
| 10: Refill | `ElementFactory.CreateRandom()` |
