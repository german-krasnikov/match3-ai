# Step 7: DESTRUCTION SYSTEM - Система уничтожения

> **Зависимости:** IGrid, IGridElement, ElementComponent (шаги 1-3)
> **Используется в:** GameLoop (шаг 9)
> **Статус:** Независимая реализация с STUB-заглушками

---

## Обзор

DestructionComponent отвечает за:
- Анимированное уничтожение элементов по списку позиций
- Параллельное выполнение анимаций для всех элементов
- Очистку ячеек в Grid после уничтожения
- Публикацию событий начала/завершения уничтожения

---

## Файловая структура

```
Assets/Scripts/
├── Core/Interfaces/
│   └── IDestructionSystem.cs      # Интерфейс (если не создан в step1)
└── Destruction/
    └── DestructionComponent.cs    # Основной компонент
```

---

## 1. Интерфейс IDestructionSystem

**Файл:** `Assets/Scripts/Core/Interfaces/IDestructionSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IDestructionSystem
{
    Task DestroyElements(List<Vector2Int> positions);
    Task DestroyElement(Vector2Int pos);

    event Action<List<Vector2Int>> OnDestructionStarted;
    event Action<List<Vector2Int>> OnDestructionCompleted;
}
```

**Примечание:** Если интерфейс уже создан в step1, пропустить.

---

## 2. DestructionComponent

**Файл:** `Assets/Scripts/Destruction/DestructionComponent.cs`

### 2.1 Структура класса

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class DestructionComponent : MonoBehaviour, IDestructionSystem
{
    // === СОБЫТИЯ ===
    public event Action<List<Vector2Int>> OnDestructionStarted;
    public event Action<List<Vector2Int>> OnDestructionCompleted;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _destroyDuration = 0.2f;
    [SerializeField] private Ease _scaleEase = Ease.InBack;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public async Task DestroyElements(List<Vector2Int> positions);
    public async Task DestroyElement(Vector2Int pos);

    // === ПРИВАТНЫЕ МЕТОДЫ ===
    private async Task AnimateDestruction(ElementComponent element);
}
```

### 2.2 Реализация DestroyElements (параллельное уничтожение)

```csharp
public async Task DestroyElements(List<Vector2Int> positions)
{
    if (positions == null || positions.Count == 0)
        return;

    OnDestructionStarted?.Invoke(positions);

    // Собираем все задачи анимации
    var tasks = new List<Task>();

    foreach (var pos in positions)
    {
        var element = _grid.GetElementAt(pos);
        if (element == null)
            continue;

        // Очищаем ячейку сразу (логически элемент уже удалён)
        _grid.ClearCell(pos);

        // Запускаем анимацию параллельно
        var elementComponent = element as ElementComponent;
        if (elementComponent != null)
        {
            tasks.Add(AnimateDestruction(elementComponent));
        }
    }

    // Ждём завершения всех анимаций
    if (tasks.Count > 0)
    {
        await Task.WhenAll(tasks);
    }

    OnDestructionCompleted?.Invoke(positions);
}
```

### 2.3 Реализация DestroyElement (одиночное уничтожение)

```csharp
public async Task DestroyElement(Vector2Int pos)
{
    await DestroyElements(new List<Vector2Int> { pos });
}
```

### 2.4 Реализация AnimateDestruction

```csharp
private async Task AnimateDestruction(ElementComponent element)
{
    if (element == null || element.gameObject == null)
        return;

    var spriteRenderer = element.GetComponent<SpriteRenderer>();

    // Создаём последовательность анимаций
    var sequence = DOTween.Sequence();

    // Scale down с эффектом "втягивания"
    sequence.Join(
        element.transform
            .DOScale(0f, _destroyDuration)
            .SetEase(_scaleEase)
    );

    // Fade out параллельно
    if (spriteRenderer != null)
    {
        sequence.Join(
            spriteRenderer
                .DOFade(0f, _destroyDuration)
        );
    }

    // Ждём завершения анимации
    await sequence.AsyncWaitForCompletion();

    // Уничтожаем GameObject
    if (element != null && element.gameObject != null)
    {
        Destroy(element.gameObject);
    }
}
```

---

## 3. Полный код DestructionComponent

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class DestructionComponent : MonoBehaviour, IDestructionSystem
{
    // === СОБЫТИЯ ===
    public event Action<List<Vector2Int>> OnDestructionStarted;
    public event Action<List<Vector2Int>> OnDestructionCompleted;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _destroyDuration = 0.2f;
    [SerializeField] private Ease _scaleEase = Ease.InBack;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

    public async Task DestroyElements(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count == 0)
            return;

        OnDestructionStarted?.Invoke(positions);

        var tasks = new List<Task>();

        foreach (var pos in positions)
        {
            var element = _grid.GetElementAt(pos);
            if (element == null)
                continue;

            _grid.ClearCell(pos);

            var elementComponent = element as ElementComponent;
            if (elementComponent != null)
            {
                tasks.Add(AnimateDestruction(elementComponent));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        OnDestructionCompleted?.Invoke(positions);
    }

    public async Task DestroyElement(Vector2Int pos)
    {
        await DestroyElements(new List<Vector2Int> { pos });
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===

    private async Task AnimateDestruction(ElementComponent element)
    {
        if (element == null || element.gameObject == null)
            return;

        var spriteRenderer = element.GetComponent<SpriteRenderer>();

        var sequence = DOTween.Sequence();

        sequence.Join(
            element.transform
                .DOScale(0f, _destroyDuration)
                .SetEase(_scaleEase)
        );

        if (spriteRenderer != null)
        {
            sequence.Join(
                spriteRenderer.DOFade(0f, _destroyDuration)
            );
        }

        await sequence.AsyncWaitForCompletion();

        if (element != null && element.gameObject != null)
        {
            Destroy(element.gameObject);
        }
    }
}
```

---

## 4. STUB-заглушки для тестирования

### 4.1 StubGrid (если GridComponent недоступен)

```csharp
// Временная заглушка для тестирования без Grid
public class StubGrid : MonoBehaviour, IGrid
{
    private Dictionary<Vector2Int, IGridElement> _elements = new();

    public int Width => 8;
    public int Height => 8;
    public float CellSize => 1f;

    public IGridElement GetElementAt(Vector2Int pos)
    {
        _elements.TryGetValue(pos, out var element);
        return element;
    }

    public void SetElementAt(Vector2Int pos, IGridElement element)
    {
        _elements[pos] = element;
    }

    public void ClearCell(Vector2Int pos)
    {
        _elements.Remove(pos);
        Debug.Log($"[StubGrid] Cleared cell at {pos}");
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x, gridPos.y, 0);

    public Vector2Int WorldToGrid(Vector3 worldPos)
        => new Vector2Int((int)worldPos.x, (int)worldPos.y);

    public bool IsValidPosition(Vector2Int pos)
        => pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

    public event Action<Vector2Int, IGridElement> OnElementPlaced;
    public event Action<Vector2Int> OnCellCleared;
}
```

### 4.2 StubElement (если ElementComponent недоступен)

```csharp
// Временная заглушка для тестирования
public class StubElement : MonoBehaviour, IGridElement
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector2Int GridPosition { get; set; }
    public ElementType Type => ElementType.Red;
    public GameObject GameObject => gameObject;

    public static StubElement Create(Vector2Int pos, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = $"StubElement_{pos.x}_{pos.y}";
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = Vector3.one * 0.9f;

        if (parent != null)
            go.transform.SetParent(parent);

        // Удаляем MeshRenderer, добавляем SpriteRenderer
        Destroy(go.GetComponent<MeshRenderer>());
        Destroy(go.GetComponent<MeshFilter>());

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.red;

        var element = go.AddComponent<StubElement>();
        element._spriteRenderer = sr;
        element.GridPosition = pos;

        return element;
    }
}
```

---

## 5. Тестовый компонент

**Файл:** `Assets/Scripts/Destruction/DestructionTester.cs` (только для тестирования)

```csharp
using System.Collections.Generic;
using UnityEngine;

public class DestructionTester : MonoBehaviour
{
    [SerializeField] private DestructionComponent _destruction;
    [SerializeField] private GridComponent _grid; // или StubGrid

    [Header("Test Settings")]
    [SerializeField] private List<Vector2Int> _testPositions;

    private void Start()
    {
        // Подписка на события
        _destruction.OnDestructionStarted += OnDestructionStarted;
        _destruction.OnDestructionCompleted += OnDestructionCompleted;
    }

    private void OnDestroy()
    {
        _destruction.OnDestructionStarted -= OnDestructionStarted;
        _destruction.OnDestructionCompleted -= OnDestructionCompleted;
    }

    [ContextMenu("Test Destroy Elements")]
    private async void TestDestroy()
    {
        Debug.Log($"[Test] Starting destruction of {_testPositions.Count} elements");
        await _destruction.DestroyElements(_testPositions);
        Debug.Log("[Test] Destruction completed");
    }

    [ContextMenu("Create Test Elements")]
    private void CreateTestElements()
    {
        foreach (var pos in _testPositions)
        {
            var element = StubElement.Create(pos, transform);
            _grid.SetElementAt(pos, element);
        }
        Debug.Log($"[Test] Created {_testPositions.Count} test elements");
    }

    private void OnDestructionStarted(List<Vector2Int> positions)
    {
        Debug.Log($"[Event] Destruction started: {positions.Count} elements");
    }

    private void OnDestructionCompleted(List<Vector2Int> positions)
    {
        Debug.Log($"[Event] Destruction completed: {positions.Count} elements");
    }
}
```

---

## 6. Настройка сцены для тестирования

### 6.1 Иерархия объектов

```
Scene
├── Main Camera
├── ---SYSTEMS---
│   ├── Grid (GridComponent или StubGrid)
│   └── Destruction (DestructionComponent)
└── ---TEST---
    └── DestructionTester (DestructionTester)
```

### 6.2 Настройка компонентов

**DestructionComponent:**
- `_destroyDuration`: 0.2 (или по вкусу)
- `_scaleEase`: InBack
- `_grid`: ссылка на Grid объект

**DestructionTester:**
- `_destruction`: ссылка на Destruction
- `_grid`: ссылка на Grid
- `_testPositions`: [(0,0), (1,1), (2,2)]

---

## 7. Порядок тестирования

1. **Создать структуру папок:**
   ```
   Assets/Scripts/Destruction/
   ```

2. **Создать DestructionComponent.cs** с полным кодом

3. **Создать StubGrid и StubElement** (если шаги 2-3 не готовы)

4. **Настроить тестовую сцену:**
   - Создать пустые GameObject-ы для систем
   - Добавить компоненты
   - Настроить ссылки через Inspector

5. **Запустить тест:**
   - Context Menu → "Create Test Elements"
   - Context Menu → "Test Destroy Elements"
   - Проверить:
     - [ ] Элементы уменьшаются (scale → 0)
     - [ ] Элементы становятся прозрачными (fade)
     - [ ] Анимации выполняются параллельно
     - [ ] События OnDestructionStarted/Completed вызываются
     - [ ] GameObject-ы уничтожаются после анимации
     - [ ] Ячейки в Grid очищаются

---

## 8. Интеграция с GameLoop (шаг 9)

GameLoop будет вызывать DestructionComponent так:

```csharp
// В GameLoopComponent
[SerializeField] private DestructionComponent _destruction;

private async Task ProcessMatches(List<Vector2Int> matches)
{
    SetState(GameState.Destroying);
    await _destruction.DestroyElements(matches);
    // ... далее gravity
}
```

---

## 9. Чеклист реализации

- [ ] Создать папку `Assets/Scripts/Destruction/`
- [ ] Создать `IDestructionSystem.cs` (если нет)
- [ ] Создать `DestructionComponent.cs`
- [ ] Реализовать `DestroyElements()` с параллельными анимациями
- [ ] Реализовать `DestroyElement()` для одиночного удаления
- [ ] Реализовать `AnimateDestruction()` с DOTween
- [ ] Добавить события `OnDestructionStarted/Completed`
- [ ] Очищать ячейки Grid перед анимацией
- [ ] Создать STUB-заглушки (если нужны)
- [ ] Протестировать на тестовой сцене
- [ ] Проверить параллельность анимаций
- [ ] Проверить очистку ячеек
- [ ] Проверить вызов событий

---

## 10. Возможные улучшения (опционально, после MVP)

1. **Эффект частиц при уничтожении:**
   ```csharp
   [SerializeField] private ParticleSystem _destroyVfxPrefab;

   private void SpawnVfx(Vector3 position)
   {
       Instantiate(_destroyVfxPrefab, position, Quaternion.identity);
   }
   ```

2. **Звук при уничтожении:**
   ```csharp
   public event Action<int> OnElementsDestroyed; // количество
   ```

3. **Пул объектов вместо Destroy:**
   ```csharp
   // Вернуть в пул вместо Destroy
   _elementPool.Return(element);
   ```

4. **Разные анимации для разных типов матчей:**
   ```csharp
   private async Task AnimateMatch3(ElementComponent element);
   private async Task AnimateMatch4(ElementComponent element);
   private async Task AnimateMatch5(ElementComponent element);
   ```

---

## Примечания

- **DOTween:** Убедиться, что DOTween инициализирован (`DOTween.Init()` или авто-инициализация)
- **async/await:** Unity поддерживает через `Task`, DOTween — через `AsyncWaitForCompletion()`
- **Null-checks:** Важны, т.к. элементы могут быть уничтожены из других систем
- **Порядок:** Сначала `ClearCell`, потом анимация — чтобы другие системы не работали с "удаляемыми" элементами
