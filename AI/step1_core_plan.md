# Step 1: CORE - Интерфейсы и Enums

> **Статус:** План реализации
> **Зависимости:** Нет (первый модуль)
> **Зависят от этого:** Все остальные модули (2-9)

---

## Цель

Создать фундамент проекта: enums для типов данных и интерфейсы-контракты для всех систем. Это позволит параллельным командам работать независимо, опираясь на стабильные контракты.

---

## Структура файлов

```
Assets/Scripts/Core/
├── ElementType.cs          # Enum типов элементов
├── GameState.cs            # Enum состояний игры
└── Interfaces/
    ├── IGrid.cs            # Контракт сетки
    ├── IGridElement.cs     # Контракт элемента
    ├── IElementFactory.cs  # Контракт фабрики
    ├── ISpawnSystem.cs     # Контракт спауна
    ├── IGravitySystem.cs   # Контракт гравитации
    ├── ISwapSystem.cs      # Контракт обмена
    ├── IMatchDetection.cs  # Контракт детекции матчей
    └── IDestructionSystem.cs # Контракт уничтожения
```

---

## Детальная реализация

### 1.1 ElementType.cs

```csharp
namespace Match3.Core
{
    public enum ElementType
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 4,
        Purple = 5
    }
}
```

**Заметки:**
- `None = 0` — для пустых ячеек и ошибок
- Значения 1-5 — для удобной итерации и Random.Range(1, 6)

---

### 1.2 GameState.cs

```csharp
namespace Match3.Core
{
    public enum GameState
    {
        Initializing,
        WaitingForInput,
        Swapping,
        CheckingMatches,
        Destroying,
        Falling
    }
}
```

**Заметки:**
- Определяет state machine игрового цикла
- `WaitingForInput` — единственное состояние, принимающее ввод

---

### 1.3 IGridElement.cs

```csharp
using UnityEngine;

namespace Match3.Core
{
    public interface IGridElement
    {
        Vector2Int GridPosition { get; set; }
        ElementType Type { get; }
        GameObject GameObject { get; }
    }
}
```

**Заметки:**
- `GridPosition` — read/write для обновления при падении
- `Type` — read-only, тип не меняется после создания
- `GameObject` — доступ к визуалу для анимаций

---

### 1.4 IGrid.cs

```csharp
using System;
using UnityEngine;

namespace Match3.Core
{
    public interface IGrid
    {
        // Properties
        int Width { get; }
        int Height { get; }
        float CellSize { get; }

        // Coordinate conversion
        Vector3 GridToWorld(Vector2Int gridPos);
        Vector2Int WorldToGrid(Vector3 worldPos);
        bool IsValidPosition(Vector2Int pos);

        // Element access
        IGridElement GetElementAt(Vector2Int pos);
        void SetElementAt(Vector2Int pos, IGridElement element);
        void ClearCell(Vector2Int pos);

        // Events
        event Action<Vector2Int, IGridElement> OnElementPlaced;
        event Action<Vector2Int> OnCellCleared;
    }
}
```

**Заметки:**
- Центральный контракт, от которого зависят все системы
- Events для реактивного обновления (UI, эффекты)

---

### 1.5 IElementFactory.cs

```csharp
using UnityEngine;

namespace Match3.Core
{
    public interface IElementFactory
    {
        IGridElement Create(ElementType type, Vector3 worldPosition);
        void Destroy(IGridElement element);
    }
}
```

**Заметки:**
- Абстрагирует создание/уничтожение элементов
- Позже можно добавить pooling без изменения интерфейса

---

### 1.6 ISpawnSystem.cs

```csharp
using System;
using UnityEngine;

namespace Match3.Core
{
    public interface ISpawnSystem
    {
        void FillGrid();
        IGridElement SpawnAt(Vector2Int pos);
        IGridElement SpawnAtTop(int column);

        event Action OnGridFilled;
    }
}
```

**Заметки:**
- `SpawnAtTop` — для гравитации (новые элементы сверху)
- `FillGrid` — начальное заполнение без матчей

---

### 1.7 IMatchDetection.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core
{
    public interface IMatchDetection
    {
        List<Vector2Int> FindAllMatches();
        List<Vector2Int> FindMatchesAt(Vector2Int pos);
        bool HasAnyMatch();
        bool WouldCreateMatch(Vector2Int pos, ElementType type);

        event Action<List<Vector2Int>> OnMatchesFound;
    }
}
```

**Заметки:**
- `WouldCreateMatch` — превентивная проверка для спауна
- Возвращает позиции, не сами элементы (разделение данных и логики)

---

### 1.8 ISwapSystem.cs

```csharp
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Match3.Core
{
    public interface ISwapSystem
    {
        bool CanSwap(Vector2Int pos1, Vector2Int pos2);
        Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2);
        Task SwapBack(Vector2Int pos1, Vector2Int pos2);

        event Action<Vector2Int, Vector2Int> OnSwapStarted;
        event Action<Vector2Int, Vector2Int> OnSwapCompleted;
    }
}
```

**Заметки:**
- `Task` для async/await с DOTween анимациями
- `SwapBack` — откат при отсутствии матча

---

### 1.9 IDestructionSystem.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Match3.Core
{
    public interface IDestructionSystem
    {
        Task DestroyElements(List<Vector2Int> positions);
        Task DestroyElement(Vector2Int pos);

        event Action<List<Vector2Int>> OnDestructionStarted;
        event Action<List<Vector2Int>> OnDestructionCompleted;
    }
}
```

**Заметки:**
- `Task` для async/await анимаций уничтожения
- Batch-операция для параллельного уничтожения

---

### 1.10 IGravitySystem.cs

```csharp
using System;
using System.Threading.Tasks;

namespace Match3.Core
{
    public interface IGravitySystem
    {
        Task ApplyGravity();
        bool HasEmptyCells();

        event Action OnGravityStarted;
        event Action OnGravityCompleted;
    }
}
```

**Заметки:**
- `ApplyGravity` — обрабатывает падение + спаун новых
- `HasEmptyCells` — проверка для цикла каскадов

---

## Чеклист реализации

- [ ] Создать папку `Assets/Scripts/Core/`
- [ ] Создать папку `Assets/Scripts/Core/Interfaces/`
- [ ] Создать `ElementType.cs`
- [ ] Создать `GameState.cs`
- [ ] Создать `IGridElement.cs`
- [ ] Создать `IGrid.cs`
- [ ] Создать `IElementFactory.cs`
- [ ] Создать `ISpawnSystem.cs`
- [ ] Создать `IMatchDetection.cs`
- [ ] Создать `ISwapSystem.cs`
- [ ] Создать `IDestructionSystem.cs`
- [ ] Создать `IGravitySystem.cs`
- [ ] Проверить компиляцию в Unity

---

## Валидация

После реализации проверить:

1. **Компиляция** — проект собирается без ошибок
2. **Namespace** — все файлы в `Match3.Core`
3. **Зависимости** — интерфейсы не ссылаются на конкретные реализации
4. **Events** — все асинхронные операции имеют пару Started/Completed

---

## Критические заметки

1. **Не добавлять MonoBehaviour** — это чистые контракты
2. **Не создавать реализации** — это задача других модулей
3. **Namespace обязателен** — избежать конфликтов имён
4. **using System.Threading.Tasks** — для async/await паттерна

---

## Время выполнения

~15-20 минут (создание файлов и проверка компиляции)
