# Step 2: Pieces (Элементы) - Детальный План Реализации

> **Статус: ✅ ЗАВЕРШЁН**
>
> Дата: 2024-12-27
> Файлов создано: 3
> Namespace: `Match3.Pieces`

---

## Предусловия (Step 1 выполнен)

Уже реализовано в `Assets/Scripts/Core/`:

| Файл | Что содержит |
|------|-------------|
| `GridPosition.cs` | readonly struct с X, Y, навигация (Up/Down/Left/Right), IsAdjacentTo |
| `PieceType.cs` | enum (None, Red, Blue, Green, Yellow, Purple, Orange) + extensions |
| `IPiece.cs` | Type, Position, GameObject, SetWorldPosition(), OnDestroyed |
| `IGrid.cs` | Width, Height, GridToWorld, WorldToGrid, IsValidPosition |
| `IBoardState.cs` | GetPieceAt, SetPieceAt, ClearCell, IsEmpty, AllPositions |

**Важно:** `PieceType` уже в Core — не дублируем!

---

## Цель Step 2

Создать компоненты для игровых фишек:
- `PieceConfig` — настройки спрайтов/цветов
- `PieceView` — визуальное представление
- `PieceComponent` — реализация IPiece

---

## Файловая структура

```
Assets/Scripts/
├── Core/                      # ✅ Уже есть
│   ├── GridPosition.cs
│   ├── PieceType.cs
│   └── Interfaces/
│       └── IPiece.cs
│
└── Pieces/                    # 🆕 Создаём
    ├── PieceConfig.cs         # ScriptableObject
    ├── PieceView.cs           # Только визуал
    └── PieceComponent.cs      # Реализует IPiece

Assets/Configs/
└── PieceConfig.asset          # 🆕 Создаём

Assets/Prefabs/Pieces/
└── Piece.prefab               # 🆕 Создаём
```

---

## Реализация

### 2.1 PieceConfig.cs

**Путь:** `Assets/Scripts/Pieces/PieceConfig.cs`

```csharp
using UnityEngine;
using Match3.Core;

namespace Match3.Pieces
{
    [CreateAssetMenu(fileName = "PieceConfig", menuName = "Match3/Piece Config")]
    public class PieceConfig : ScriptableObject
    {
        [System.Serializable]
        public class PieceVisualData
        {
            public PieceType Type;
            public Sprite Sprite;
            public Color Color = Color.white;
        }

        [SerializeField] private PieceVisualData[] _pieces;

        public Sprite GetSprite(PieceType type)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i].Type == type)
                    return _pieces[i].Sprite;
            }
            return null;
        }

        public Color GetColor(PieceType type)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i].Type == type)
                    return _pieces[i].Color;
            }
            return Color.white;
        }
    }
}
```

**Заметки:**
- Использует `PieceType` из `Match3.Core`
- Простой foreach заменён на for (performance)
- Массив для сериализации в Inspector

---

### 2.2 PieceView.cs

**Путь:** `Assets/Scripts/Pieces/PieceView.cs`

```csharp
using UnityEngine;

namespace Match3.Pieces
{
    /// <summary>
    /// Отвечает ТОЛЬКО за визуальное представление фишки (SRP).
    /// </summary>
    public class PieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public void Setup(Sprite sprite, Color color)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = color;
        }

        public void SetAlpha(float alpha)
        {
            var c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }

        public SpriteRenderer Renderer => _spriteRenderer;
    }
}
```

**Заметки:**
- Минимальный компонент — только визуал
- `SetAlpha` пригодится для анимаций (Step 6)

---

### 2.3 PieceComponent.cs

**Путь:** `Assets/Scripts/Pieces/PieceComponent.cs`

```csharp
using System;
using UnityEngine;
using Match3.Core;

namespace Match3.Pieces
{
    /// <summary>
    /// Основной компонент игровой фишки. Реализует IPiece.
    /// </summary>
    public class PieceComponent : MonoBehaviour, IPiece
    {
        public event Action<IPiece> OnDestroyed;

        [SerializeField] private PieceView _view;

        private PieceType _type;
        private GridPosition _position;

        // === IPiece ===
        public PieceType Type => _type;
        public GridPosition Position
        {
            get => _position;
            set => _position = value;
        }
        public GameObject GameObject => gameObject;

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }

        // === Public API ===
        public void Initialize(PieceType type, PieceConfig config)
        {
            _type = type;
            _view.Setup(config.GetSprite(type), config.GetColor(type));
        }

        public void Initialize(PieceType type, Sprite sprite, Color color)
        {
            _type = type;
            _view.Setup(sprite, color);
        }

        public void ResetForPool()
        {
            _type = PieceType.None;
            _position = default;
            gameObject.SetActive(false);
        }

        public PieceView View => _view;

        // === Unity ===
        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
    }
}
```

**Заметки:**
- Два перегруженных `Initialize`: с config и без
- `ResetForPool()` для Object Pool (Step 3)
- `View` exposed для анимаций (Step 6)

---

## Префаб

**Путь:** `Assets/Prefabs/Pieces/Piece.prefab`

### Иерархия:
```
Piece (GameObject)
├── PieceComponent
├── PieceView
└── SpriteRenderer
```

### Настройка в Inspector:

1. **GameObject "Piece":**
   - Layer: Default
   - Tag: Untagged

2. **SpriteRenderer:**
   - Sprite: (пусто или placeholder)
   - Sorting Layer: "Pieces" (создать)
   - Order in Layer: 0

3. **PieceView:**
   - _spriteRenderer → SpriteRenderer (этот же объект)

4. **PieceComponent:**
   - _view → PieceView (этот же объект)

---

## ScriptableObject Asset

**Путь:** `Assets/Configs/PieceConfig.asset`

### Создание:
1. RMB в Configs → Create → Match3 → Piece Config
2. Заполнить 6 элементов в массиве `_pieces`

### Данные (временные, если нет спрайтов):

| Index | Type   | Sprite | Color (hex) |
|-------|--------|--------|-------------|
| 0     | Red    | —      | FF4444      |
| 1     | Blue   | —      | 4444FF      |
| 2     | Green  | —      | 44FF44      |
| 3     | Yellow | —      | FFFF44      |
| 4     | Purple | —      | AA44FF      |
| 5     | Orange | —      | FFAA44      |

**Без спрайтов:** использовать Unity Default Sprite (белый квадрат), цвет из Color.

---

## Sorting Layers

Создать в Project Settings → Tags and Layers → Sorting Layers:

```
1. Background
2. Grid
3. Pieces      ← для фишек
4. Effects
5. UI
```

---

## Чеклист

### Код:
- [x] `Assets/Scripts/Pieces/PieceConfig.cs`
- [x] `Assets/Scripts/Pieces/PieceView.cs`
- [x] `Assets/Scripts/Pieces/PieceComponent.cs`

### Assets:
- [x] `Assets/Configs/PieceConfig.asset`
- [x] `Assets/Prefabs/Pieces/Piece.prefab`
- [x] Sorting Layer "Pieces"

### Валидация:
- [x] `PieceComponent` компилируется без ошибок
- [x] Префаб корректно отображает спрайт/цвет
- [x] `Initialize()` работает с PieceConfig

---

## Тест в Editor

Быстрая проверка без Play Mode:

1. Создать пустую сцену
2. Drag Piece.prefab в Hierarchy
3. Добавить тестовый скрипт:

```csharp
public class PieceTest : MonoBehaviour
{
    [SerializeField] private PieceComponent _piece;
    [SerializeField] private PieceConfig _config;
    [SerializeField] private PieceType _testType = PieceType.Red;

    [ContextMenu("Test Initialize")]
    private void TestInitialize()
    {
        _piece.Initialize(_testType, _config);
    }
}
```

4. ContextMenu → Test Initialize
5. Проверить что цвет/спрайт применился

---

## Зависимости для следующих шагов

| Step | Что использует |
|------|----------------|
| 3 (Spawner) | `PieceComponent.Initialize()`, `ResetForPool()`, `PieceConfig` |
| 5 (Swap) | `IPiece.Position`, `SetWorldPosition()` |
| 6 (Match/Destroy) | `IPiece.Type`, `OnDestroyed`, `PieceView` для анимаций |

---

## НЕ реализуем сейчас

- Анимации появления/уничтожения → Step 6
- Object Pool → Step 3
- Специальные фишки (бомбы и т.д.) → out of scope
