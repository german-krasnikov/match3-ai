# Step 2: Gem System

## Цель

Определить типы элементов Match3 и их визуальное представление.

---

## Архитектура

```
GemType (enum)           -- Типы gem-ов
       |
       v
GemConfig (SO)           -- Конфигурация: sprite per type
       |
       v
GemView (MonoBehaviour)  -- Визуализация на prefab-е
       |
       v
GemData (struct)         -- Данные gem-а для BoardData
```

**Принципы:**
- `GemType` — чистый enum, никаких зависимостей
- `GemConfig` — ScriptableObject, read-only данные
- `GemView` — один компонент, одна ответственность (визуализация)
- `GemData` — immutable struct для логики

---

## Компоненты

### 1. GemType.cs

**Тип:** enum
**Путь:** `Assets/Scripts/Gem/GemType.cs`

```csharp
namespace Match3.Gem
{
    public enum GemType
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5
    }
}
```

**Заметки:**
- Явные значения для стабильной сериализации
- 6 типов по умолчанию (легко расширить)

---

### 2. GemTypeData.cs

**Тип:** Serializable struct
**Путь:** `Assets/Scripts/Gem/GemTypeData.cs`

```csharp
using System;
using UnityEngine;

namespace Match3.Gem
{
    [Serializable]
    public struct GemTypeData
    {
        public GemType Type;
        public Sprite Sprite;
    }
}
```

**Поля:**
| Поле | Тип | Описание |
|------|-----|----------|
| Type | GemType | Тип gem-а |
| Sprite | Sprite | Спрайт для отображения |

---

### 3. GemConfig.cs

**Тип:** ScriptableObject
**Путь:** `Assets/Scripts/Gem/GemConfig.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Gem
{
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Match3/GemConfig")]
    public class GemConfig : ScriptableObject
    {
        [SerializeField] private List<GemTypeData> _gems;

        /// <summary>
        /// Количество доступных типов gem-ов.
        /// </summary>
        public int TypeCount => _gems.Count;

        /// <summary>
        /// Возвращает спрайт для указанного типа.
        /// </summary>
        public Sprite GetSprite(GemType type)
        {
            foreach (var gem in _gems)
            {
                if (gem.Type == type)
                    return gem.Sprite;
            }
            Debug.LogWarning($"GemConfig: Sprite not found for {type}");
            return null;
        }

        /// <summary>
        /// Возвращает случайный тип из доступных.
        /// </summary>
        public GemType GetRandomType()
        {
            int index = Random.Range(0, _gems.Count);
            return _gems[index].Type;
        }

        /// <summary>
        /// Возвращает все доступные типы.
        /// </summary>
        public IReadOnlyList<GemType> GetAllTypes()
        {
            var types = new List<GemType>(_gems.Count);
            foreach (var gem in _gems)
            {
                types.Add(gem.Type);
            }
            return types;
        }
    }
}
```

**Public API:**
| Метод/Свойство | Сигнатура | Описание |
|----------------|-----------|----------|
| TypeCount | `int` | Количество типов |
| GetSprite | `Sprite GetSprite(GemType type)` | Спрайт по типу |
| GetRandomType | `GemType GetRandomType()` | Случайный тип |
| GetAllTypes | `IReadOnlyList<GemType> GetAllTypes()` | Все типы |

**Зависимости:** Нет

---

### 4. GemData.cs

**Тип:** struct
**Путь:** `Assets/Scripts/Gem/GemData.cs`

```csharp
using UnityEngine;

namespace Match3.Gem
{
    public readonly struct GemData
    {
        public GemType Type { get; }
        public Vector2Int Position { get; }

        public GemData(GemType type, Vector2Int position)
        {
            Type = type;
            Position = position;
        }

        public GemData WithPosition(Vector2Int newPosition)
        {
            return new GemData(Type, newPosition);
        }

        public override string ToString()
        {
            return $"Gem({Type}, {Position})";
        }
    }
}
```

**Заметки:**
- `readonly struct` — immutable, копируется по значению
- `WithPosition` — создает копию с новой позицией (для перемещений)

**Public API:**
| Член | Сигнатура | Описание |
|------|-----------|----------|
| Type | `GemType` | Тип gem-а |
| Position | `Vector2Int` | Позиция на сетке |
| Constructor | `GemData(GemType, Vector2Int)` | Создание |
| WithPosition | `GemData WithPosition(Vector2Int)` | Копия с новой позицией |

---

### 5. GemView.cs

**Тип:** MonoBehaviour
**Путь:** `Assets/Scripts/Gem/GemView.cs`

```csharp
using UnityEngine;

namespace Match3.Gem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private GemType _type;

        /// <summary>
        /// Текущий тип gem-а.
        /// </summary>
        public GemType Type => _type;

        /// <summary>
        /// Позиция на сетке (для удобства доступа из BoardView).
        /// </summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>
        /// Инициализирует gem с типом и конфигом.
        /// </summary>
        public void Setup(GemType type, GemConfig config)
        {
            _type = type;
            _spriteRenderer.sprite = config.GetSprite(type);
        }

        /// <summary>
        /// Устанавливает позицию в мировых координатах.
        /// </summary>
        public void SetWorldPosition(Vector3 pos)
        {
            transform.position = pos;
        }

        /// <summary>
        /// Устанавливает позицию на сетке (для отслеживания).
        /// </summary>
        public void SetGridPosition(Vector2Int pos)
        {
            GridPosition = pos;
        }

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
```

**Зависимости (SerializeField):**
| Поле | Тип | Описание |
|------|-----|----------|
| _spriteRenderer | SpriteRenderer | Рендерер спрайта |

**Public API:**
| Метод/Свойство | Сигнатура | Описание |
|----------------|-----------|----------|
| Type | `GemType` (get) | Текущий тип |
| GridPosition | `Vector2Int` (get) | Позиция на сетке |
| Setup | `void Setup(GemType, GemConfig)` | Инициализация |
| SetWorldPosition | `void SetWorldPosition(Vector3)` | Установка world position |
| SetGridPosition | `void SetGridPosition(Vector2Int)` | Установка grid position |

**Заметки:**
- `Reset()` — автозаполнение в Inspector
- `RequireComponent` — гарантирует наличие SpriteRenderer
- Не содержит событий — это чистый View

---

## Prefab: Gem.prefab

**Путь:** `Assets/Prefabs/Gem.prefab`

**Структура:**
```
Gem (GameObject)
  ├── SpriteRenderer
  │     - Sprite: (none, устанавливается через Setup)
  │     - Sorting Layer: Default
  │     - Order in Layer: 0
  └── GemView (script)
        - _spriteRenderer: (ссылка на SpriteRenderer выше)
```

**Настройки Transform:**
- Position: (0, 0, 0)
- Scale: (1, 1, 1)

---

## ScriptableObject Asset

**Путь:** `Assets/ScriptableObjects/GemConfig.asset`

**Настройки по умолчанию:**
```
_gems:
  - Type: Red,    Sprite: gem_red.png
  - Type: Blue,   Sprite: gem_blue.png
  - Type: Green,  Sprite: gem_green.png
  - Type: Yellow, Sprite: gem_yellow.png
  - Type: Purple, Sprite: gem_purple.png
  - Type: Orange, Sprite: gem_orange.png
```

---

## Интеграция с другими шагами

### Step 3 (Spawn System) будет использовать:

```csharp
// SpawnSystem.cs
public GemType GenerateType(Vector2Int pos, BoardData board)
{
    // Использует GemConfig.GetRandomType() + anti-match логику
}

// BoardView.cs
public void CreateGem(Vector2Int pos, GemData gem)
{
    // Instantiate(gemPrefab)
    // gemView.Setup(gem.Type, _gemConfig)
    // gemView.SetWorldPosition(_gridData.GridToWorld(pos))
    // gemView.SetGridPosition(pos)
}
```

### Step 6 (Match System) будет использовать:

```csharp
// MatchSystem.cs
public List<MatchData> FindAllMatches(BoardData board)
{
    // Сравнивает GemData.Type для поиска матчей
}
```

---

## Файловая структура

```
Assets/
  Scripts/
    Gem/
      GemType.cs          # enum
      GemTypeData.cs      # serializable struct
      GemConfig.cs        # ScriptableObject
      GemData.cs          # readonly struct
      GemView.cs          # MonoBehaviour
  Prefabs/
    Gem.prefab            # Prefab с SpriteRenderer + GemView
  ScriptableObjects/
    GemConfig.asset       # Asset с конфигурацией типов
```

---

## Checklist для реализации

- [ ] Создать `Assets/Scripts/Gem/` папку
- [ ] Реализовать `GemType.cs`
- [ ] Реализовать `GemTypeData.cs`
- [ ] Реализовать `GemConfig.cs`
- [ ] Реализовать `GemData.cs`
- [ ] Реализовать `GemView.cs`
- [ ] Создать `Assets/Prefabs/` папку (если нет)
- [ ] Создать `Gem.prefab` с SpriteRenderer + GemView
- [ ] Создать `Assets/ScriptableObjects/` папку (если нет)
- [ ] Создать `GemConfig.asset` через Create > Match3 > GemConfig
- [ ] Добавить placeholder спрайты (или временные цветные квадраты)
- [ ] Заполнить GemConfig 6 типами

---

## Тестирование

Для проверки работоспособности можно создать временный тест-скрипт:

```csharp
// TestGemSystem.cs (временный, удалить после проверки)
public class TestGemSystem : MonoBehaviour
{
    [SerializeField] private GemConfig _config;
    [SerializeField] private GemView _gemPrefab;

    private void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            var type = _config.GetRandomType();
            var gem = Instantiate(_gemPrefab, new Vector3(i, 0, 0), Quaternion.identity);
            gem.Setup(type, _config);
        }
    }
}
```

Ожидаемый результат: 6 gem-ов в ряд с разными спрайтами.
