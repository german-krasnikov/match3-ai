# Этап 7: Gravity & Refill — Детальный план реализации

## Анализ текущей архитектуры

Изучив существующий код, выявил следующие паттерны:

| Паттерн | Пример в проекте |
|---------|------------------|
| ScriptableObject для конфигов | `SwapConfig`, `DestroyConfig` |
| Отдельный компонент для анимаций | `SwapAnimationComponent`, `DestroyAnimationComponent` |
| Event-driven завершение | `OnDestructionComplete`, `OnSwapCompleted` |
| DOTween Sequence | Все анимации используют `Sequence` с callback |
| Kill предыдущей анимации | `_currentSequence?.Kill()` |

---

## 7.1 GravityConfig (ScriptableObject)

**Файл:** `Assets/Scripts/Gravity/GravityConfig.cs`

```csharp
[CreateAssetMenu(fileName = "GravityConfig", menuName = "Match3/GravityConfig")]
public class GravityConfig : ScriptableObject
{
    [Header("Fall Animation")]
    [SerializeField, Range(0.05f, 0.3f)] private float _fallDurationPerCell = 0.1f;
    [SerializeField] private Ease _fallEase = Ease.OutBounce;

    [Header("Timing")]
    [SerializeField, Range(0f, 0.05f)] private float _columnDelay = 0.02f;

    [Header("New Elements")]
    [SerializeField, Range(0f, 2f)] private float _spawnHeightOffset = 1f;

    // Properties...
}
```

**Параметры:**
- `_fallDurationPerCell` — время падения на одну клетку (накапливается)
- `_fallEase` — Ease.OutBounce даёт эффект "приземления"
- `_columnDelay` — задержка между колонками для волнового эффекта
- `_spawnHeightOffset` — насколько выше сетки появляются новые элементы

---

## 7.2 FallData (Plain Class)

**Файл:** `Assets/Scripts/Gravity/FallData.cs`

```csharp
public class FallData
{
    public ElementComponent Element { get; }
    public int FromY { get; }
    public int ToY { get; }
    public int Column { get; }

    public int Distance => FromY - ToY;
    public bool IsNewElement { get; }

    public FallData(ElementComponent element, int fromY, int toY, int column, bool isNew = false)
    {
        Element = element;
        FromY = fromY;
        ToY = toY;
        Column = column;
        IsNewElement = isNew;
    }
}
```

**Назначение:** DTO для передачи данных о падении между компонентами.

---

## 7.3 GravityComponent (MonoBehaviour)

**Файл:** `Assets/Scripts/Gravity/GravityComponent.cs`

### Зависимости (SerializeField)
```csharp
[SerializeField] private GridComponent _grid;
```

### События
```csharp
public event Action<List<FallData>> OnGravityCalculated;
```

### Основной метод

```csharp
public List<FallData> ProcessGravity()
{
    var fallData = new List<FallData>();

    // Обрабатываем каждую колонку независимо
    for (int x = 0; x < _grid.Width; x++)
    {
        ProcessColumn(x, fallData);
    }

    OnGravityCalculated?.Invoke(fallData);
    return fallData;
}
```

### Алгоритм ProcessColumn

```
Для колонки X:
1. Сканируем снизу вверх (y = 0 → Height-1)
2. writeIndex = 0 (куда писать следующий элемент)
3. Для каждой ячейки:
   - Если НЕ пустая:
     - Если y != writeIndex → элемент должен упасть
     - Перемещаем элемент в _cells[x, writeIndex]
     - Обновляем element.SetGridPosition(x, writeIndex)
     - Создаём FallData(element, y, writeIndex, x)
     - writeIndex++
   - Если пустая: пропускаем (дырка)
4. После цикла: writeIndex указывает на первую пустую ячейку сверху
```

**Визуализация:**
```
До:          После ProcessColumn:    FallData:
[5] ●        [5] ●                   (нет движения)
[4] ○        [4] ○                   (нет движения)
[3] _        [3] _ (empty)
[2] ●        [2] _ (empty)           ● : 4→2
[1] _        [1] _ (empty)
[0] ●        [0] ● (не двигался)

● = элемент, ○ = другой элемент, _ = пусто
```

### Код ProcessColumn

```csharp
private void ProcessColumn(int x, List<FallData> fallData)
{
    int writeIndex = 0;

    for (int y = 0; y < _grid.Height; y++)
    {
        var cell = _grid.GetCell(x, y);
        if (cell.IsEmpty) continue;

        if (y != writeIndex)
        {
            var element = cell.Element;
            var targetCell = _grid.GetCell(x, writeIndex);

            // Обновляем Grid
            cell.Clear();
            targetCell.Element = element;

            // Обновляем Element
            element.SetGridPosition(x, writeIndex);

            // Записываем данные для анимации
            fallData.Add(new FallData(element, y, writeIndex, x));
        }

        writeIndex++;
    }
}
```

---

## 7.4 FallAnimationComponent (MonoBehaviour)

**Файл:** `Assets/Scripts/Gravity/FallAnimationComponent.cs`

### Зависимости
```csharp
[SerializeField] private GravityConfig _config;
[SerializeField] private GridComponent _grid;
```

### События
```csharp
public event Action OnFallComplete;
```

### Основной метод

```csharp
public void AnimateFalls(List<FallData> fallData, Action onComplete)
{
    _currentSequence?.Kill();

    if (fallData.Count == 0)
    {
        onComplete?.Invoke();
        return;
    }

    _currentSequence = DOTween.Sequence();

    // Группируем по колонкам для волнового эффекта
    var byColumn = fallData.GroupBy(f => f.Column).OrderBy(g => g.Key);

    int columnIndex = 0;
    foreach (var columnGroup in byColumn)
    {
        float columnDelay = columnIndex * _config.ColumnDelay;

        foreach (var fall in columnGroup)
        {
            Vector3 targetPos = _grid.GridToWorld(fall.Column, fall.ToY);
            float duration = fall.Distance * _config.FallDurationPerCell;

            _currentSequence.Insert(columnDelay,
                fall.Element.transform.DOMove(targetPos, duration)
                    .SetEase(_config.FallEase));
        }

        columnIndex++;
    }

    _currentSequence.OnComplete(() =>
    {
        OnFallComplete?.Invoke();
        onComplete?.Invoke();
    });
}
```

### Особенности анимации

1. **Время пропорционально расстоянию** — элемент, падающий на 3 клетки, анимируется дольше чем на 1
2. **Волновой эффект** — колонки начинают падать с небольшой задержкой слева направо
3. **OutBounce** — лёгкий "отскок" при приземлении

---

## 7.5 RefillComponent (MonoBehaviour)

**Файл:** `Assets/Scripts/Gravity/RefillComponent.cs`

### Зависимости
```csharp
[SerializeField] private GridComponent _grid;
[SerializeField] private SpawnComponent _spawn;
[SerializeField] private GravityConfig _config;
```

### События
```csharp
public event Action<List<FallData>> OnRefillCalculated;
```

### Основной метод

```csharp
public List<FallData> SpawnNewElements()
{
    var fallData = new List<FallData>();

    for (int x = 0; x < _grid.Width; x++)
    {
        int emptyCount = CountEmptyFromTop(x);

        for (int i = 0; i < emptyCount; i++)
        {
            int targetY = _grid.Height - 1 - i;
            int spawnY = _grid.Height + (emptyCount - 1 - i);

            // Создаём элемент выше сетки
            var element = _spawn.SpawnRandomAt(x, targetY, useSpawnOffset: false);

            // Устанавливаем начальную позицию выше сетки
            Vector3 spawnPos = _grid.GridToWorld(x, targetY);
            spawnPos.y += (spawnY - targetY + _config.SpawnHeightOffset) * _grid.Config.CellSize;
            element.transform.position = spawnPos;

            fallData.Add(new FallData(element, spawnY, targetY, x, isNew: true));
        }
    }

    OnRefillCalculated?.Invoke(fallData);
    return fallData;
}

private int CountEmptyFromTop(int x)
{
    int count = 0;
    for (int y = _grid.Height - 1; y >= 0; y--)
    {
        if (_grid.GetCell(x, y).IsEmpty)
            count++;
        else
            break; // Считаем только непрерывные пустые сверху
    }
    return count;
}
```

### Логика Refill

```
Колонка после Gravity:     После Refill:
[7] (выше сетки)           [7] ★ spawn position
[6] (выше сетки)           [6] ★ spawn position
[5] _ (empty)              [5] ● (новый, grid pos)
[4] _ (empty)              [4] ● (новый, grid pos)
[3] ●                      [3] ●
[2] ●                      [2] ●
[1] ●                      [1] ●
[0] ●                      [0] ●

★ = визуальная позиция спауна (выше сетки)
● = элемент в grid
```

**Ключевой момент:** Элемент сразу записывается в Grid на целевую позицию (`targetY`), но визуально находится выше сетки. Анимация двигает его вниз.

---

## 7.6 Интеграция компонентов

### Порядок вызовов (будет в GameLoopController)

```csharp
// После DestroyComponent.OnDestructionComplete:

// 1. Гравитация — существующие элементы падают
var gravityFalls = _gravityComponent.ProcessGravity();

// 2. Заполнение — новые элементы появляются
var refillFalls = _refillComponent.SpawnNewElements();

// 3. Объединяем все падения
var allFalls = new List<FallData>();
allFalls.AddRange(gravityFalls);
allFalls.AddRange(refillFalls);

// 4. Анимируем
_fallAnimation.AnimateFalls(allFalls, () =>
{
    // 5. После падения — снова проверяем матчи
    var newMatches = _matchDetector.FindMatches(_grid);
    // ... цикл продолжается
});
```

---

## 7.7 Структура файлов

```
Assets/Scripts/Gravity/
├── GravityConfig.cs        # ScriptableObject настроек
├── FallData.cs             # DTO данных падения
├── GravityComponent.cs     # Логика гравитации
├── FallAnimationComponent.cs  # Анимация падения
└── RefillComponent.cs      # Спаун новых элементов
```

---

## 7.8 Checklist реализации

### GravityConfig
- [ ] Создать ScriptableObject
- [ ] Параметры: FallDurationPerCell, FallEase, ColumnDelay, SpawnHeightOffset
- [ ] Создать asset в `Assets/Configs/GravityConfig.asset`

### FallData
- [ ] Создать класс с полями: Element, FromY, ToY, Column, IsNewElement
- [ ] Property Distance для расчёта времени анимации

### GravityComponent
- [ ] SerializeField: GridComponent
- [ ] Event: OnGravityCalculated
- [ ] Метод ProcessGravity() → List<FallData>
- [ ] Алгоритм: сканирование снизу вверх, сдвиг элементов

### FallAnimationComponent
- [ ] SerializeField: GravityConfig, GridComponent
- [ ] Event: OnFallComplete
- [ ] Метод AnimateFalls(List<FallData>, Action onComplete)
- [ ] DOTween Sequence с группировкой по колонкам

### RefillComponent
- [ ] SerializeField: GridComponent, SpawnComponent, GravityConfig
- [ ] Event: OnRefillCalculated
- [ ] Метод SpawnNewElements() → List<FallData>
- [ ] Подсчёт пустых ячеек, спаун выше сетки

---

## 7.9 Настройка сцены

1. Создать пустой GameObject **"GravitySystem"**
2. Добавить компоненты:
   - `GravityComponent`
   - `FallAnimationComponent`
   - `RefillComponent`
3. Привязать зависимости в Inspector:
   - GridComponent → из BoardManager
   - SpawnComponent → из BoardManager
   - GravityConfig → asset

---

## 7.10 Тестирование

### Ручной тест без GameLoop

```csharp
// Временный тест-код в любом MonoBehaviour:
[ContextMenu("Test Gravity")]
private void TestGravity()
{
    // Удалим несколько элементов вручную
    var cell1 = _grid.GetCell(3, 2);
    var cell2 = _grid.GetCell(3, 4);

    if (cell1.Element != null)
    {
        Destroy(cell1.Element.gameObject);
        cell1.Clear();
    }
    if (cell2.Element != null)
    {
        Destroy(cell2.Element.gameObject);
        cell2.Clear();
    }

    // Запускаем гравитацию
    var falls = _gravity.ProcessGravity();
    var refills = _refill.SpawnNewElements();

    var all = new List<FallData>();
    all.AddRange(falls);
    all.AddRange(refills);

    _fallAnimation.AnimateFalls(all, () => Debug.Log("Fall complete!"));
}
```

### Ожидаемый результат
1. Элементы выше удалённых падают вниз
2. Новые элементы появляются сверху и падают
3. Анимация с "отскоком" (OutBounce)
4. Колонки падают с небольшой задержкой (волна)

---

## 7.11 Возможные улучшения (не в scope)

- **Диагональное падение** — если соседняя колонка имеет пустоту ниже
- **Разная скорость для новых/старых** — новые могут падать быстрее
- **Particle эффект** при приземлении
- **Звук** падения с pitch вариацией

---

## Связь с Этапом 8 (Game Loop)

После реализации этого этапа, GameLoopController будет использовать:

```
[Destroying]
    ↓ OnDestructionComplete
[Falling]
    → GravityComponent.ProcessGravity()
    → RefillComponent.SpawnNewElements()
    → FallAnimationComponent.AnimateFalls()
    ↓ OnFallComplete
[CheckingMatches]
    → MatchDetector.FindMatches()
    ↓ matches found? → loop back to [Destroying]
    ↓ no matches → [Idle]
```
