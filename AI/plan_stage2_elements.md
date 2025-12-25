# Этап 2: Elements — Детальный план реализации

**Статус: ✅ РЕАЛИЗОВАНО**

## Обзор
Создание системы игровых элементов (тайлов). Элемент — это визуальный объект на сетке, имеющий тип (цвет) и позицию.

**Зависимости:** Этап 1 (Grid System) — уже реализован.

---

## 2.1 ElementType.cs

**Путь:** `Assets/Scripts/Elements/ElementType.cs`

**Описание:** Enum с типами элементов.

```csharp
namespace Match3.Elements
{
    public enum ElementType
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5
    }
}
```

**Решения:**
- `None = 0` — для пустых ячеек и дефолтного значения
- Явные значения — для сериализации и дебага

---

## 2.2 ElementData.cs

**Путь:** `Assets/Scripts/Elements/ElementData.cs`

**Описание:** ScriptableObject с данными одного типа элемента.

```csharp
using UnityEngine;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementData", menuName = "Match3/Element Data")]
    public class ElementData : ScriptableObject
    {
        [SerializeField] private ElementType _type;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;

        public ElementType Type => _type;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
    }
}
```

**Зачем Color:**
- Если спрайт белый/серый — можно тинтить
- Для VFX частиц при уничтожении
- Для debug-подсветки

---

## 2.3 ElementDatabase.cs

**Путь:** `Assets/Scripts/Elements/ElementDatabase.cs`

**Описание:** ScriptableObject — реестр всех типов элементов.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementDatabase", menuName = "Match3/Element Database")]
    public class ElementDatabase : ScriptableObject
    {
        [SerializeField] private List<ElementData> _elements = new();

        private Dictionary<ElementType, ElementData> _lookup;

        public IReadOnlyList<ElementData> Elements => _elements;
        public int Count => _elements.Count;

        public ElementData GetData(ElementType type)
        {
            BuildLookupIfNeeded();
            return _lookup.TryGetValue(type, out var data) ? data : null;
        }

        public ElementData GetRandom()
        {
            if (_elements.Count == 0) return null;
            return _elements[Random.Range(0, _elements.Count)];
        }

        public ElementType GetRandomType()
        {
            return GetRandom()?.Type ?? ElementType.None;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<ElementType, ElementData>();
            foreach (var element in _elements)
            {
                if (element != null && !_lookup.ContainsKey(element.Type))
                    _lookup[element.Type] = element;
            }
        }

        private void OnValidate()
        {
            _lookup = null; // Rebuild on next access
        }
    }
}
```

**Паттерн:**
- Lazy Dictionary для O(1) lookup
- `OnValidate` сбрасывает кэш при изменении в Editor
- `GetRandom()` — для спауна элементов

---

## 2.4 ElementComponent.cs

**Путь:** `Assets/Scripts/Elements/ElementComponent.cs`

**Описание:** MonoBehaviour — компонент игрового элемента.

```csharp
using System;
using UnityEngine;

namespace Match3.Elements
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ElementComponent : MonoBehaviour
    {
        public event Action<ElementComponent> OnDestroyed;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        private ElementType _type;
        private Vector2Int _gridPosition;

        public ElementType Type => _type;
        public Vector2Int GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public void Initialize(ElementData data, Vector2Int gridPos)
        {
            _type = data.Type;
            _gridPosition = gridPos;

            _spriteRenderer.sprite = data.Sprite;
            _spriteRenderer.color = data.Color;

            gameObject.name = $"Element_{data.Type}_{gridPos.x}_{gridPos.y}";
        }

        public void ResetElement()
        {
            _type = ElementType.None;
            _gridPosition = Vector2Int.zero;
            _spriteRenderer.sprite = null;
        }

        public void DestroyElement()
        {
            OnDestroyed?.Invoke(this);
        }

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
```

**Дизайн-решения:**
- `RequireComponent` — гарантирует SpriteRenderer
- `Reset()` — автозаполнение в Editor
- `ResetElement()` — для пулинга (Этап 3)
- `DestroyElement()` — не уничтожает GameObject, только вызывает событие. Реальное уничтожение/возврат в пул — ответственность другой системы
- Публичный `SpriteRenderer` — для анимаций (DOTween)
- `GridPosition` с сеттером — обновляется при свапе/падении

---

## 2.5 Структура папок

```
Assets/
├── Scripts/
│   └── Elements/
│       ├── ElementType.cs
│       ├── ElementData.cs
│       ├── ElementDatabase.cs
│       └── ElementComponent.cs
├── Data/
│   └── Elements/
│       ├── ElementDatabase.asset
│       ├── RedElement.asset
│       ├── BlueElement.asset
│       ├── GreenElement.asset
│       ├── YellowElement.asset
│       └── PurpleElement.asset
├── Prefabs/
│   └── Element.prefab
└── Sprites/
    └── Elements/
        └── (placeholder спрайты)
```

---

## 2.6 Настройка в Unity Editor

### Шаг 1: Создать placeholder спрайты
**Вариант A:** Использовать Unity Default Sprite (Knob) + Color tint
**Вариант B:** Создать 5 цветных кругов/квадратов в любом редакторе

### Шаг 2: Создать ElementData ассеты
1. `Assets/Data/Elements/` → Create → Match3 → Element Data
2. Создать 5 ассетов: RedElement, BlueElement, GreenElement, YellowElement, PurpleElement
3. Для каждого:
   - Указать Type (enum)
   - Назначить Sprite (или оставить null если используем Color tint)
   - Указать Color

### Шаг 3: Создать ElementDatabase
1. `Assets/Data/Elements/` → Create → Match3 → Element Database
2. Добавить все 5 ElementData в список

### Шаг 4: Создать Element Prefab
1. Create Empty GameObject "Element"
2. Add Component: SpriteRenderer
   - Sorting Layer: "Default" (или создать "Elements")
   - Order in Layer: 0
3. Add Component: ElementComponent
   - Назначить SpriteRenderer
4. Сохранить как Prefab в `Assets/Prefabs/Element.prefab`

---

## 2.7 Порядок реализации

| # | Задача | Статус |
|---|--------|--------|
| 1 | Создать `ElementType.cs` | ✅ |
| 2 | Создать `ElementData.cs` | ✅ |
| 3 | Создать `ElementDatabase.cs` | ✅ |
| 4 | Создать `ElementComponent.cs` | ✅ |
| 5 | Создать placeholder спрайты | ⏭️ Используем Color tint |
| 6 | Создать ElementData ассеты (5 шт) | ✅ Editor скрипт |
| 7 | Создать ElementDatabase ассет | ✅ Editor скрипт |
| 8 | Создать Element.prefab | ✅ Editor скрипт |

---

## 2.8 Тестирование

### Ручной тест в Editor
1. На сцену добавить Element.prefab
2. В Inspector у ElementComponent вызвать контекстное меню → Reset (заполнит SpriteRenderer)
3. Создать тестовый скрипт или через Debug:
   - Получить ElementData из Database
   - Вызвать `Initialize(data, new Vector2Int(0,0))`
   - Убедиться что спрайт/цвет применились

### Тестовый скрипт (опционально)
```csharp
#if UNITY_EDITOR
using UnityEngine;

namespace Match3.Elements
{
    public class ElementTester : MonoBehaviour
    {
        [SerializeField] private ElementDatabase _database;
        [SerializeField] private ElementComponent _element;

        [ContextMenu("Test Random Element")]
        private void TestRandomElement()
        {
            var data = _database.GetRandom();
            if (data != null && _element != null)
                _element.Initialize(data, Vector2Int.zero);
        }
    }
}
#endif
```

---

## 2.9 Интеграция с Этапом 1 (Grid)

Элементы НЕ зависят от Grid напрямую. Связь через:
- `GridComponent.GridToWorld()` — определяет позицию элемента в мире
- `ElementComponent.GridPosition` — хранит логическую позицию

Пример использования (будет в Этапе 3 - Spawn):
```csharp
var worldPos = grid.GridToWorld(new Vector2Int(x, y));
var element = Instantiate(prefab, worldPos, Quaternion.identity);
element.Initialize(data, new Vector2Int(x, y));
```

---

## 2.10 Чеклист готовности

- [ ] Скомпилировалось без ошибок
- [ ] ElementDatabase.GetData() возвращает правильные данные
- [ ] ElementDatabase.GetRandom() возвращает случайные элементы
- [ ] ElementComponent.Initialize() применяет sprite и color
- [ ] Prefab настроен корректно (SpriteRenderer назначен)
- [ ] Все ассеты созданы и заполнены

---

## Уточнённые параметры

| Параметр | Значение |
|----------|----------|
| Спрайты | Placeholder (цветные круги, создаём) |
| Размер элемента | Занимает весь cellSize (1 unit) |
| Sorting Layer | Создаём с нуля: "Board", "Elements", "UI" |

---

## 2.11 Sorting Layers (создать в Project Settings)

**Порядок (снизу вверх):**
1. `Default` — фон
2. `Board` — подложка сетки (если будет)
3. `Elements` — игровые элементы
4. `Effects` — VFX, частицы
5. `UI` — интерфейс

**Настройка:** Edit → Project Settings → Tags and Layers → Sorting Layers

---

## 2.12 Placeholder Sprites

Создаём 5 PNG файлов 128x128 (или 256x256) — цветные круги.

**Вариант A — Создать в графическом редакторе:**
- Круг с мягкими краями
- Цвета: Red `#FF4444`, Blue `#4488FF`, Green `#44DD44`, Yellow `#FFDD44`, Purple `#AA44FF`

**Вариант B — Использовать Unity Sprite Editor:**
1. Создать белый круг (Knob из Unity Default)
2. Тинтить через Color в ElementData

**Рекомендация:** Вариант B проще для прототипа. Позже заменим на финальные спрайты.

**Import Settings для спрайтов:**
- Texture Type: Sprite (2D and UI)
- Pixels Per Unit: 128 (если спрайт 128x128, получим 1 unit)
- Filter Mode: Bilinear
- Compression: None (для чёткости placeholder'ов)

---

## 2.13 Editor Setup Script

**Путь:** `Assets/Scripts/Editor/ElementsSetup.cs`

Автоматизирует создание всех ассетов через меню Unity.

### Команды меню

| Меню | Описание |
|------|----------|
| `Match3 → Setup → Create Sorting Layers` | Создаёт Board, Elements, Effects |
| `Match3 → Setup → Create Element Assets` | Создаёт все ассеты и prefab |

### Порядок запуска
1. **Create Sorting Layers** — сначала
2. **Create Element Assets** — после

### Что создаётся

**ElementData ассеты (5 шт):**
| Файл | Type | Color |
|------|------|-------|
| `RedElement.asset` | Red | #FF4444 |
| `BlueElement.asset` | Blue | #4488FF |
| `GreenElement.asset` | Green | #44DD44 |
| `YellowElement.asset` | Yellow | #FFDD44 |
| `PurpleElement.asset` | Purple | #AA44FF |

**ElementDatabase:**
- `Assets/Data/Elements/ElementDatabase.asset`
- Содержит ссылки на все 5 ElementData

**Element Prefab:**
- `Assets/Prefabs/Element.prefab`
- SpriteRenderer (Sorting Layer: Elements)
- ElementComponent (с привязанным SpriteRenderer)

### Особенности
- **Идемпотентность** — не перезаписывает существующие ассеты
- **Автосвязывание** — SpriteRenderer автоматически назначается в ElementComponent
- **Fallback** — если Sorting Layer "Elements" не существует, использует "Default"
