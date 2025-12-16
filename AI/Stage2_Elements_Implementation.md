# Этап 2: Elements — Подробный План Реализации

## Обзор

Elements — визуальные игровые объекты на сетке. Отвечают за:
- Тип элемента (цвет)
- Визуальное отображение (спрайт)
- Позицию на сетке
- Связь с Cell

**Принцип Unity Way:** ElementComponent хранит только данные и события. Визуал через SpriteRenderer. Создание/удаление через Factory.

---

## Архитектура

```
ElementType (enum)           — типы элементов
ElementConfig (SO)           — настройки и спрайты
ElementComponent (MB)        — компонент на префабе
ElementFactory (MB)          — фабрика создания
```

**Связи:**
```
Cell ←→ ElementComponent (двусторонняя ссылка)
ElementFactory → ElementConfig (данные для создания)
ElementComponent → SpriteRenderer (визуал)
```

---

## 2.1 ElementType (Enum)

### Назначение
Перечисление типов элементов. Используется для матчинга и визуала.

### Путь файла
`Assets/Scripts/Elements/ElementType.cs`

### Код

```csharp
public enum ElementType
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Purple = 4
}
```

### Примечания
- Явные значения для сериализации
- Порядок соответствует индексам в массиве спрайтов ElementConfig
- Расширяемо (можно добавить Orange = 5 и т.д.)

---

## 2.2 ElementConfig (ScriptableObject)

### Назначение
Хранит настройки элементов: спрайты по типам, префаб. Один конфиг на игру.

### Путь файла
`Assets/Scripts/Elements/ElementConfig.cs`

### Код

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ElementConfig", menuName = "Match3/ElementConfig")]
public class ElementConfig : ScriptableObject
{
    [Header("Visuals")]
    [SerializeField] private Sprite[] _sprites;

    [Header("Prefab")]
    [SerializeField] private ElementComponent _prefab;

    public ElementComponent Prefab => _prefab;

    public Sprite GetSprite(ElementType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _sprites.Length)
        {
            Debug.LogError($"No sprite for {type}");
            return null;
        }
        return _sprites[index];
    }

    public int TypeCount => _sprites.Length;
}
```

### Создание Placeholder спрайтов

В Unity создать 5 квадратных спрайтов разных цветов:

1. **Способ 1 — Через код (Editor script):**
```csharp
// Одноразовый скрипт для генерации
Texture2D tex = new Texture2D(64, 64);
Color[] colors = tex.GetPixels();
for (int i = 0; i < colors.Length; i++) colors[i] = Color.red;
tex.SetPixels(colors);
tex.Apply();
// Сохранить как PNG
```

2. **Способ 2 — Default sprite:**
   - Использовать `Sprites/Square` из Unity (Knob sprite)
   - Цвет задавать через `SpriteRenderer.color`

**Рекомендую Способ 2** — проще, цвет меняется в рантайме.

### Модификация ElementConfig для цветового подхода

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ElementConfig", menuName = "Match3/ElementConfig")]
public class ElementConfig : ScriptableObject
{
    [Header("Visuals")]
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private Color[] _colors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.6f, 0.2f, 0.8f) // Purple
    };

    [Header("Prefab")]
    [SerializeField] private ElementComponent _prefab;

    public ElementComponent Prefab => _prefab;
    public Sprite DefaultSprite => _defaultSprite;

    public Color GetColor(ElementType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _colors.Length)
            return Color.white;
        return _colors[index];
    }

    public int TypeCount => _colors.Length;
}
```

---

## 2.3 ElementComponent (MonoBehaviour)

### Назначение
Компонент элемента. Хранит тип, позицию на сетке, управляет визуалом.

### Путь файла
`Assets/Scripts/Elements/ElementComponent.cs`

### Код

```csharp
using System;
using UnityEngine;

public class ElementComponent : MonoBehaviour
{
    public event Action<ElementComponent> OnDestroyed;

    [SerializeField] private SpriteRenderer _spriteRenderer;

    private ElementType _type;
    private int _x;
    private int _y;

    public ElementType Type => _type;
    public int X => _x;
    public int Y => _y;
    public Vector2Int GridPosition => new Vector2Int(_x, _y);

    public void Initialize(ElementType type, Color color, int x, int y)
    {
        _type = type;
        _x = x;
        _y = y;
        _spriteRenderer.color = color;
    }

    public void SetGridPosition(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        _x = pos.x;
        _y = pos.y;
    }

    public void DestroyElement()
    {
        OnDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
}
```

### Структура префаба

```
Element (GameObject)
├── ElementComponent
└── SpriteRenderer
    - Sprite: Knob (или Square)
    - Sorting Layer: Default
    - Order in Layer: 0
```

### Почему SpriteRenderer через SerializeField?
- Явная зависимость
- Не нужен GetComponent в рантайме
- Видно в Inspector

---

## 2.4 ElementFactory (MonoBehaviour)

### Назначение
Создаёт и уничтожает элементы. Централизованная точка создания.

### Путь файла
`Assets/Scripts/Elements/ElementFactory.cs`

### Код

```csharp
using UnityEngine;

public class ElementFactory : MonoBehaviour
{
    [SerializeField] private ElementConfig _config;
    [SerializeField] private Transform _container;

    public ElementComponent Create(ElementType type, Vector3 worldPosition, int gridX, int gridY)
    {
        var element = Instantiate(_config.Prefab, worldPosition, Quaternion.identity, _container);
        element.Initialize(type, _config.GetColor(type), gridX, gridY);
        return element;
    }

    public ElementComponent CreateRandom(Vector3 worldPosition, int gridX, int gridY)
    {
        var type = GetRandomType();
        return Create(type, worldPosition, gridX, gridY);
    }

    public ElementComponent CreateRandomExcluding(Vector3 worldPosition, int gridX, int gridY, params ElementType[] excluded)
    {
        var type = GetRandomTypeExcluding(excluded);
        return Create(type, worldPosition, gridX, gridY);
    }

    public void Destroy(ElementComponent element)
    {
        if (element != null)
            element.DestroyElement();
    }

    private ElementType GetRandomType()
    {
        int index = Random.Range(0, _config.TypeCount);
        return (ElementType)index;
    }

    private ElementType GetRandomTypeExcluding(ElementType[] excluded)
    {
        ElementType type;
        int attempts = 0;
        do
        {
            type = GetRandomType();
            attempts++;
        } while (System.Array.IndexOf(excluded, type) >= 0 && attempts < 100);

        return type;
    }
}
```

### Примечания
- `_container` — пустой GameObject для организации иерархии
- `CreateRandomExcluding` — для BoardInitializer (предотвращение начальных матчей)
- Object Pooling добавится позже (заменим Instantiate/Destroy)

---

## Настройка сцены

### Шаг 1: Создать файлы

```
Assets/Scripts/Elements/
├── ElementType.cs
├── ElementConfig.cs
├── ElementComponent.cs
└── ElementFactory.cs
```

### Шаг 2: Создать префаб Element

1. Create → 2D Object → Sprite → назвать `Element`
2. Sprite: `Knob` (Packages/Unity UI/Sprites)
3. Scale: (0.8, 0.8, 1) — чуть меньше ячейки
4. Add Component → `ElementComponent`
5. Перетащить SpriteRenderer в поле `_spriteRenderer`
6. Сохранить в `Assets/Prefabs/Element.prefab`

### Шаг 3: Создать ElementConfig

1. `Assets/Configs/` → ПКМ → Create → Match3 → ElementConfig
2. Назначить:
   - Default Sprite: Knob
   - Colors: Red, Blue, Green, Yellow, Purple
   - Prefab: Element.prefab

### Шаг 4: Добавить ElementFactory на сцену

1. Создать пустой GameObject → назвать `ElementFactory`
2. Add Component → `ElementFactory`
3. Назначить:
   - Config: ElementConfig.asset
   - Container: создать child `Elements` для организации

### Иерархия сцены

```
Board
├── GridComponent
ElementFactory
├── Elements (container)
```

---

## Тестирование

### Тест-скрипт (временный)

Добавить на сцену для проверки:

```csharp
using UnityEngine;

public class ElementTest : MonoBehaviour
{
    [SerializeField] private ElementFactory _factory;
    [SerializeField] private GridComponent _grid;

    private void Start()
    {
        // Создать элемент в центре
        var pos = _grid.GridToWorld(3, 3);
        var element = _factory.Create(ElementType.Red, pos, 3, 3);
        Debug.Log($"Created {element.Type} at {element.GridPosition}");

        // Создать случайный
        pos = _grid.GridToWorld(4, 3);
        var random = _factory.CreateRandom(pos, 4, 3);
        Debug.Log($"Created random {random.Type}");

        // Создать с исключением
        pos = _grid.GridToWorld(5, 3);
        var excluded = _factory.CreateRandomExcluding(pos, 5, 3, ElementType.Red, ElementType.Blue);
        Debug.Log($"Created excluded {excluded.Type}"); // Не Red и не Blue
    }
}
```

### Ожидаемый результат

- 3 цветных квадрата на сетке
- Логи с типами в Console
- Элементы в иерархии под `Elements`

---

## Интеграция с Grid

### Связь Cell ↔ Element

После создания элемента нужно связать с ячейкой:

```csharp
// В будущем BoardInitializer или SpawnComponent:
var cell = _grid.GetCell(x, y);
var worldPos = _grid.GridToWorld(x, y);
var element = _factory.Create(type, worldPos, x, y);
cell.Element = element; // Двусторонняя связь
```

Cell уже имеет событие `OnElementChanged` — можно подписаться для отладки или UI.

---

## Чеклист готовности

- [ ] ElementType.cs создан
- [ ] ElementConfig.cs создан
- [ ] ElementConfig.asset создан и настроен
- [ ] ElementComponent.cs обновлён
- [ ] Element.prefab создан
- [ ] ElementFactory.cs создан
- [ ] ElementFactory на сцене с настройками
- [ ] Тест создания элементов работает
- [ ] Элементы отображаются правильных цветов
- [ ] Код компилируется без ошибок

---

## Следующий этап

После завершения Elements → **Этап 3: Spawn & Board Init**
- SpawnComponent
- BoardInitializer (заполнение без начальных матчей)
