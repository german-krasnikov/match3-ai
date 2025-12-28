# Step 3: ELEMENT SYSTEM — Подробный план реализации

> **Scope:** Только Element System. Grid (Step 2) считаем реализованным по общему плану.

---

## Зависимости

### От предыдущих шагов (Step 1-2)
```csharp
// Step 1: Core — используем напрямую
ElementType          // enum с типами элементов
IGridElement         // интерфейс элемента сетки

// Step 2: Grid — используем STUB для тестирования
IGrid.GridToWorld()  // конвертация позиции
```

### STUB для независимой разработки
```csharp
// Заглушка для Grid — позволяет тестировать без реальной сетки
private Vector3 StubGridToWorld(Vector2Int gridPos)
    => new Vector3(gridPos.x, gridPos.y, 0);
```

---

## Структура файлов

```
Assets/
├── Scripts/
│   └── Elements/
│       ├── ElementComponent.cs        # MonoBehaviour элемента
│       ├── ElementFactoryComponent.cs # Фабрика создания
│       └── ElementColorConfig.cs      # ScriptableObject с цветами
├── ScriptableObjects/
│   └── ElementColors.asset            # Инстанс конфига цветов
└── Prefabs/
    └── Element.prefab                 # Префаб элемента
```

---

## Порядок реализации

### 3.1 ElementColorConfig.cs — ScriptableObject

**Цель:** Хранить цвета для каждого ElementType, настраиваемые в Inspector.

```csharp
using UnityEngine;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementColors", menuName = "Match3/Element Color Config")]
    public class ElementColorConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ElementColor
        {
            public ElementType type;
            public Color color;
        }

        [SerializeField] private ElementColor[] _colors;

        public Color GetColor(ElementType type)
        {
            foreach (var ec in _colors)
            {
                if (ec.type == type)
                    return ec.color;
            }
            return Color.white; // fallback
        }
    }
}
```

**Подзадачи:**
- [ ] Создать файл `ElementColorConfig.cs`
- [ ] Создать папку `Assets/ScriptableObjects/`
- [ ] Создать asset через контекстное меню: Create → Match3 → Element Color Config
- [ ] Настроить 5 цветов в Inspector:
  - Red: `#FF4444`
  - Green: `#44FF44`
  - Blue: `#4444FF`
  - Yellow: `#FFFF44`
  - Purple: `#AA44FF`

---

### 3.2 ElementComponent.cs — Компонент элемента

**Цель:** Визуальное представление элемента на сетке. Реализует `IGridElement`.

```csharp
using UnityEngine;
using Match3.Core;

namespace Match3.Elements
{
    public class ElementComponent : MonoBehaviour, IGridElement
    {
        [Header("Dependencies")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Runtime (Debug)")]
        [SerializeField] private ElementType _type;
        [SerializeField] private Vector2Int _gridPosition;

        // Ссылка на конфиг устанавливается при создании
        private ElementColorConfig _colorConfig;

        // === IGridElement ===
        public Vector2Int GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }

        public ElementType Type => _type;
        public GameObject GameObject => gameObject;

        // === Public API ===
        public void Initialize(ElementType type, ElementColorConfig colorConfig)
        {
            _type = type;
            _colorConfig = colorConfig;
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_spriteRenderer == null || _colorConfig == null)
                return;

            _spriteRenderer.color = _colorConfig.GetColor(_type);
        }
    }
}
```

**Подзадачи:**
- [ ] Создать файл `ElementComponent.cs`
- [ ] Убедиться что `_type` и `_gridPosition` видны в Inspector (для дебага)
- [ ] `Initialize()` принимает конфиг цветов и сразу вызывает `UpdateVisual()`

---

### 3.3 Element.prefab — Префаб элемента

**Цель:** Готовый к спауну GameObject с настроенными компонентами.

**Структура префаба:**
```
Element (GameObject)
├── Transform
├── SpriteRenderer
│   └── Sprite: Unity Default "Square" (или кастомный)
│   └── Sorting Layer: Default
│   └── Order in Layer: 0
└── ElementComponent
    └── _spriteRenderer: ссылка на SpriteRenderer выше
```

**Подзадачи:**
- [ ] Создать папку `Assets/Prefabs/`
- [ ] Создать пустой GameObject, назвать "Element"
- [ ] Добавить SpriteRenderer с квадратным спрайтом
- [ ] Добавить ElementComponent
- [ ] Связать SpriteRenderer в ElementComponent через Inspector
- [ ] Сохранить как префаб в `Assets/Prefabs/Element.prefab`

---

### 3.4 ElementFactoryComponent.cs — Фабрика элементов

**Цель:** Создавать и уничтожать элементы. Единая точка входа для спауна.

```csharp
using UnityEngine;
using Match3.Core;

namespace Match3.Elements
{
    public class ElementFactoryComponent : MonoBehaviour, IElementFactory
    {
        [Header("Dependencies")]
        [SerializeField] private ElementComponent _elementPrefab;
        [SerializeField] private ElementColorConfig _colorConfig;

        [Header("Settings")]
        [SerializeField] private Transform _elementsParent; // опционально, для иерархии

        // === IElementFactory ===
        public ElementComponent Create(ElementType type, Vector3 worldPosition)
        {
            var element = Instantiate(_elementPrefab, worldPosition, Quaternion.identity, _elementsParent);
            element.Initialize(type, _colorConfig);
            return element;
        }

        public void Destroy(ElementComponent element)
        {
            if (element == null)
                return;

            Object.Destroy(element.gameObject);
        }
    }
}
```

**Подзадачи:**
- [ ] Создать файл `ElementFactoryComponent.cs`
- [ ] `_elementsParent` — опциональный Transform для организации иерархии (может быть null)
- [ ] При создании устанавливать parent для чистоты Hierarchy

> **Примечание:** `GridPosition` устанавливается вызывающим кодом (SpawnComponent) отдельно после создания:
> ```csharp
> var element = _factory.Create(type, worldPos);
> element.GridPosition = gridPos;
> _grid.SetElementAt(gridPos, element);
> ```

---

## Тестирование

### 3.5 Тестовый скрипт (временный)

**Цель:** Проверить работу Element System независимо от Grid.

```csharp
using UnityEngine;
using Match3.Core;
using Match3.Elements;

namespace Match3.Tests
{
    public class ElementSystemTest : MonoBehaviour
    {
        [SerializeField] private ElementFactoryComponent _factory;

        private void Start()
        {
            // STUB: Имитация Grid.GridToWorld()
            Vector3 StubGridToWorld(Vector2Int pos) => new Vector3(pos.x, pos.y, 0);

            // Создаём по одному элементу каждого типа
            var types = new[] { ElementType.Red, ElementType.Green, ElementType.Blue, ElementType.Yellow, ElementType.Purple };

            for (int i = 0; i < types.Length; i++)
            {
                var gridPos = new Vector2Int(i, 0);
                var worldPos = StubGridToWorld(gridPos);

                var element = _factory.Create(types[i], worldPos);
                element.GridPosition = gridPos; // Устанавливаем отдельно (вариант B)

                Debug.Log($"Created {types[i]} at grid {gridPos}, world {worldPos}");
            }
        }
    }
}
```

**Подзадачи:**
- [ ] Создать тестовую сцену или использовать SampleScene
- [ ] Создать пустой GameObject с ElementFactoryComponent
- [ ] Настроить ссылки на prefab и colorConfig
- [ ] Создать тестовый GameObject с ElementSystemTest
- [ ] Запустить и проверить что 5 разноцветных квадратов появились в ряд
- [ ] Удалить тестовый скрипт после проверки (или оставить в Tests/)

---

## Чеклист готовности

### Файлы созданы
- [ ] `Assets/Scripts/Elements/ElementColorConfig.cs`
- [ ] `Assets/Scripts/Elements/ElementComponent.cs`
- [ ] `Assets/Scripts/Elements/ElementFactoryComponent.cs`
- [ ] `Assets/ScriptableObjects/ElementColors.asset`
- [ ] `Assets/Prefabs/Element.prefab`

### Функциональность
- [ ] ElementColorConfig возвращает правильные цвета для всех 5 типов
- [ ] ElementComponent реализует IGridElement
- [ ] ElementComponent корректно отображает цвет при Initialize()
- [ ] ElementFactoryComponent создаёт элементы с правильными цветами
- [ ] ElementFactoryComponent корректно уничтожает элементы
- [ ] Тест: 5 элементов разных цветов отображаются в сцене

### Unity Way соответствие
- [ ] Каждый компонент имеет одну ответственность
- [ ] Зависимости через [SerializeField]
- [ ] Нет GetComponent в runtime
- [ ] Приватные поля с `_` префиксом
- [ ] Компоненты готовы к интеграции с Grid (Step 2)

---

## Интеграция с другими системами

После реализации Step 2 (Grid) и Step 3 (Elements):

```csharp
// SpawnComponent (Step 4) будет использовать так:
var gridPos = new Vector2Int(x, y);
var worldPos = _grid.GridToWorld(gridPos);
var element = _factory.Create(type, worldPos);
element.GridPosition = gridPos;
_grid.SetElementAt(gridPos, element);
```

---

## Примечания

1. **Namespace:** Используем `Match3.Elements` для изоляции от других систем
2. **Цвета:** Яркие, хорошо различимые (не пастельные)
3. **Спрайт:** Пока используем Unity Default Square, потом заменим на красивый
4. **Pooling:** Не реализуем (согласно плану, добавим позже)
