# Этап 3: Spawn & Board Init — Подробный План Реализации

## Обзор

Spawn & Board Init — система заполнения игрового поля. Отвечает за:
- Спаун элементов в конкретные ячейки
- Первоначальное заполнение всей сетки
- Гарантию отсутствия матчей при старте

**Принцип Unity Way:** SpawnComponent — тонкая прослойка между Grid и Factory. BoardInitializer — отдельный компонент инициализации, который можно отключить/заменить для разных уровней.

---

## Архитектура

```
SpawnComponent (MB)         — спаун одного элемента в ячейку
BoardInitializer (MB)       — заполнение всей сетки при старте
```

**Связи:**
```
BoardInitializer → SpawnComponent → ElementFactory
                                  → GridComponent
SpawnComponent → Cell (связывает element с ячейкой)
```

**Поток данных при инициализации:**
```
[Awake: GridComponent создаёт сетку]
              ↓
[Start: BoardInitializer запускает заполнение]
              ↓
[SpawnComponent.SpawnAt() для каждой ячейки]
              ↓
[ElementFactory создаёт элемент]
              ↓
[Cell.Element = element]
```

---

## 3.1 SpawnComponent (MonoBehaviour)

### Назначение
Создаёт элемент и связывает его с ячейкой сетки. Единая точка спауна — все элементы создаются через этот компонент.

### Путь файла
`Assets/Scripts/Spawn/SpawnComponent.cs`

### Код

```csharp
using UnityEngine;

public class SpawnComponent : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactory _factory;

    [Header("Spawn Settings")]
    [Tooltip("Смещение по Y для спауна (для анимации падения)")]
    [SerializeField] private float _spawnHeightOffset = 5f;

    public ElementComponent SpawnAt(int x, int y, ElementType type, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        // Позиция: обычная или со смещением для падения
        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
        {
            worldPos.y += _spawnHeightOffset;
        }

        var element = _factory.Create(type, worldPos, x, y);
        cell.Element = element;

        return element;
    }

    public ElementComponent SpawnRandomAt(int x, int y, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
        {
            worldPos.y += _spawnHeightOffset;
        }

        var element = _factory.CreateRandom(worldPos, x, y);
        cell.Element = element;

        return element;
    }

    public ElementComponent SpawnRandomExcludingAt(int x, int y, ElementType[] excluded, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
        {
            worldPos.y += _spawnHeightOffset;
        }

        var element = _factory.CreateRandomExcluding(worldPos, x, y, excluded);
        cell.Element = element;

        return element;
    }

    /// <summary>
    /// Получить позицию спауна над колонкой (для падения новых элементов)
    /// </summary>
    public Vector3 GetSpawnPosition(int x)
    {
        return _grid.GridToWorld(x, _grid.Height) + Vector3.up * _spawnHeightOffset;
    }
}
```

### Примечания
- `useSpawnOffset` — для будущей анимации падения новых элементов
- `SpawnRandomExcludingAt` — ключевой метод для BoardInitializer
- `GetSpawnPosition` — для этапа 7 (Refill)

---

## 3.2 BoardInitializer (MonoBehaviour)

### Назначение
Заполняет сетку при старте игры. Гарантирует отсутствие начальных матчей через алгоритм исключения.

### Путь файла
`Assets/Scripts/Spawn/BoardInitializer.cs`

### Алгоритм предотвращения матчей

```
Для каждой ячейки (x, y) слева направо, снизу вверх:
  1. Проверить 2 элемента СЛЕВА (x-1, x-2)
  2. Проверить 2 элемента СНИЗУ (y-1, y-2)
  3. Если оба слева одинаковые → добавить их тип в excluded
  4. Если оба снизу одинаковые → добавить их тип в excluded
  5. Создать элемент случайного типа, исключая excluded
```

**Визуализация:**
```
Заполняем (2,1):
     ?        ← проверяем
   [B][B][?]  ← два Blue слева → исключаем Blue
     [R]      ← один Red снизу → не исключаем

Результат: элемент будет Red, Green, Yellow или Purple (не Blue)
```

### Код

```csharp
using System.Collections.Generic;
using UnityEngine;

public class BoardInitializer : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawner;

    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        // Заполняем снизу вверх, слева направо
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                SpawnWithoutMatch(x, y);
            }
        }
    }

    private void SpawnWithoutMatch(int x, int y)
    {
        var excluded = GetExcludedTypes(x, y);

        if (excluded.Count > 0)
        {
            _spawner.SpawnRandomExcludingAt(x, y, excluded.ToArray());
        }
        else
        {
            _spawner.SpawnRandomAt(x, y);
        }
    }

    private List<ElementType> GetExcludedTypes(int x, int y)
    {
        var excluded = new List<ElementType>();

        // Проверка горизонтали (2 элемента слева)
        var leftType = GetMatchTypeHorizontal(x, y);
        if (leftType.HasValue)
        {
            excluded.Add(leftType.Value);
        }

        // Проверка вертикали (2 элемента снизу)
        var bottomType = GetMatchTypeVertical(x, y);
        if (bottomType.HasValue && !excluded.Contains(bottomType.Value))
        {
            excluded.Add(bottomType.Value);
        }

        return excluded;
    }

    private ElementType? GetMatchTypeHorizontal(int x, int y)
    {
        // Нужно минимум 2 элемента слева
        if (x < 2) return null;

        var cell1 = _grid.GetCell(x - 1, y);
        var cell2 = _grid.GetCell(x - 2, y);

        if (cell1?.Element == null || cell2?.Element == null) return null;

        if (cell1.Element.Type == cell2.Element.Type)
        {
            return cell1.Element.Type;
        }

        return null;
    }

    private ElementType? GetMatchTypeVertical(int x, int y)
    {
        // Нужно минимум 2 элемента снизу
        if (y < 2) return null;

        var cell1 = _grid.GetCell(x, y - 1);
        var cell2 = _grid.GetCell(x, y - 2);

        if (cell1?.Element == null || cell2?.Element == null) return null;

        if (cell1.Element.Type == cell2.Element.Type)
        {
            return cell1.Element.Type;
        }

        return null;
    }

    /// <summary>
    /// Очистка поля (для рестарта)
    /// </summary>
    public void ClearBoard()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var cell = _grid.GetCell(x, y);
                if (cell?.Element != null)
                {
                    Destroy(cell.Element.gameObject);
                    cell.Clear();
                }
            }
        }
    }
}
```

### Примечания
- `Start()` вместо `Awake()` — чтобы GridComponent успел создать сетку в своём `Awake()`
- `ElementType?` (nullable) — удобно для проверки "нет совпадения"
- `ClearBoard()` — бонус для будущего рестарта уровня

---

## Альтернатива: Event-Driven инициализация

Если нужна гарантия порядка (Grid → Board), используем события:

### Модификация BoardInitializer

```csharp
public class BoardInitializer : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawner;
    [SerializeField] private bool _initOnGridCreated = true;

    private void OnEnable()
    {
        if (_initOnGridCreated)
        {
            _grid.OnGridCreated += InitializeBoard;
        }
    }

    private void OnDisable()
    {
        _grid.OnGridCreated -= InitializeBoard;
    }

    // ... остальной код без изменений
}
```

**Когда использовать:**
- `Start()` — простой случай, один уровень
- `OnGridCreated` — сложные сценарии, динамическая загрузка уровней

---

## Настройка сцены

### Шаг 1: Создать файлы

```
Assets/Scripts/Spawn/
├── SpawnComponent.cs
└── BoardInitializer.cs
```

### Шаг 2: Настроить иерархию

```
Board (GameObject)
├── GridComponent       [существует]
├── SpawnComponent      [добавить]
└── BoardInitializer    [добавить]

ElementFactory          [существует]
├── Elements (container)
```

### Шаг 3: Связать компоненты в Inspector

**SpawnComponent:**
- Grid: Board (GridComponent)
- Factory: ElementFactory
- Spawn Height Offset: 5

**BoardInitializer:**
- Grid: Board (GridComponent)
- Spawner: Board (SpawnComponent)

### Альтернативная иерархия (всё на Board)

```
Board
├── GridComponent
├── SpawnComponent
├── BoardInitializer
└── ElementFactory
    └── Elements
```

Преимущество: все компоненты на одном объекте, проще связывать.

---

## Script Execution Order

Убедиться в правильном порядке выполнения:

1. **GridComponent.Awake()** — создаёт сетку
2. **BoardInitializer.Start()** — заполняет сетку

Unity гарантирует: все `Awake()` выполняются до всех `Start()`.

Если нужен явный контроль:
- Edit → Project Settings → Script Execution Order
- GridComponent: -100
- BoardInitializer: 0

---

## Тестирование

### Визуальная проверка

1. Запустить Play Mode
2. Поле должно заполниться цветными элементами
3. Визуально проверить: нет трёх одинаковых в ряд

### Тест-скрипт для проверки матчей

```csharp
using UnityEngine;

public class BoardValidator : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;

    [ContextMenu("Validate No Matches")]
    public void ValidateNoMatches()
    {
        int matchCount = 0;

        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                if (HasHorizontalMatch(x, y)) matchCount++;
                if (HasVerticalMatch(x, y)) matchCount++;
            }
        }

        if (matchCount == 0)
            Debug.Log("<color=green>✓ Board valid: No matches found</color>");
        else
            Debug.LogError($"✗ Board invalid: {matchCount} matches found!");
    }

    private bool HasHorizontalMatch(int x, int y)
    {
        if (x > _grid.Width - 3) return false;

        var c0 = _grid.GetCell(x, y)?.Element;
        var c1 = _grid.GetCell(x + 1, y)?.Element;
        var c2 = _grid.GetCell(x + 2, y)?.Element;

        if (c0 == null || c1 == null || c2 == null) return false;

        bool match = c0.Type == c1.Type && c1.Type == c2.Type;
        if (match) Debug.Log($"Horizontal match at ({x},{y}): {c0.Type}");
        return match;
    }

    private bool HasVerticalMatch(int x, int y)
    {
        if (y > _grid.Height - 3) return false;

        var c0 = _grid.GetCell(x, y)?.Element;
        var c1 = _grid.GetCell(x, y + 1)?.Element;
        var c2 = _grid.GetCell(x, y + 2)?.Element;

        if (c0 == null || c1 == null || c2 == null) return false;

        bool match = c0.Type == c1.Type && c1.Type == c2.Type;
        if (match) Debug.Log($"Vertical match at ({x},{y}): {c0.Type}");
        return match;
    }
}
```

### Использование валидатора

1. Добавить `BoardValidator` на Board
2. Play Mode → ПКМ на компоненте → "Validate No Matches"
3. Должно быть: `✓ Board valid: No matches found`

### Тест многократного запуска

```csharp
[ContextMenu("Test 100 Initializations")]
public void TestMultipleInits()
{
    var initializer = GetComponent<BoardInitializer>();
    int failures = 0;

    for (int i = 0; i < 100; i++)
    {
        initializer.ClearBoard();
        initializer.InitializeBoard();

        if (HasAnyMatch())
        {
            failures++;
            Debug.LogError($"Failure on iteration {i}");
        }
    }

    Debug.Log($"Test complete: {100 - failures}/100 passed");
}
```

---

## Отладка

### Gizmos для SpawnComponent (опционально)

```csharp
#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (_grid == null) return;

    // Показать зону спауна над сеткой
    Gizmos.color = new Color(0, 1, 0, 0.3f);

    for (int x = 0; x < _grid.Width; x++)
    {
        Vector3 spawnPos = GetSpawnPosition(x);
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
    }
}
#endif
```

### Debug логи

Временно добавить в BoardInitializer:

```csharp
private void SpawnWithoutMatch(int x, int y)
{
    var excluded = GetExcludedTypes(x, y);

    #if UNITY_EDITOR
    if (excluded.Count > 0)
        Debug.Log($"({x},{y}) excluding: {string.Join(", ", excluded)}");
    #endif

    // ... остальной код
}
```

---

## Чеклист готовности

- [ ] SpawnComponent.cs создан
- [ ] BoardInitializer.cs создан
- [ ] Компоненты добавлены на сцену
- [ ] Зависимости связаны в Inspector
- [ ] Play Mode: поле заполняется элементами
- [ ] Все ячейки имеют элементы
- [ ] Нет начальных матчей (3+ в ряд)
- [ ] BoardValidator подтверждает валидность
- [ ] Код компилируется без ошибок

---

## Возможные проблемы и решения

### Проблема: Пустые ячейки после инициализации

**Причина:** GridComponent.Awake() не успел выполниться
**Решение:** Использовать Start() в BoardInitializer или OnGridCreated событие

### Проблема: Иногда появляются матчи

**Причина:** Баг в алгоритме исключения или недостаток типов
**Решение:**
- Проверить логику GetMatchTypeHorizontal/Vertical
- Убедиться что типов >= 4 (иначе возможна ситуация когда все исключены)

### Проблема: Все элементы одного цвета

**Причина:** ElementConfig.TypeCount возвращает 0 или 1
**Решение:** Проверить ElementConfig.asset — массив цветов должен содержать 5 элементов

---

## Следующий этап

После завершения Spawn & Board Init → **Этап 4: Match Detection**
- MatchData (plain class)
- MatchDetector (поиск совпадений)
- Алгоритмы поиска горизонтальных/вертикальных матчей
