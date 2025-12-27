# Step 1: Core/Interfaces - План реализации

## Цель
Создать фундамент системы: все интерфейсы, структуры данных и enum-ы, которые обеспечат независимость модулей.

---

## Структура файлов

```
Assets/Scripts/
└── Core/
    ├── GridPosition.cs          # Struct координат
    ├── PieceType.cs              # Enum типов элементов
    └── Interfaces/
        ├── IPiece.cs             # Интерфейс элемента
        ├── IGrid.cs              # Интерфейс сетки
        ├── IBoardState.cs        # Состояние доски
        ├── IMatchChecker.cs      # Проверка матча при спауне
        ├── IMatchDetector.cs     # Детектор матчей
        ├── ISpawner.cs           # Спаунер элементов
        ├── ISwappable.cs         # Свапаемый элемент
        ├── IScoreHandler.cs      # Обработчик очков
        └── IGameEvents.cs        # Глобальные события
```

---

## 1. GridPosition.cs

**Назначение:** Immutable struct для координат в сетке. Используется везде для адресации ячеек.

```csharp
using System;

namespace Match3.Core
{
    /// <summary>
    /// Координаты ячейки в сетке (immutable)
    /// </summary>
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Соседние позиции
        public GridPosition Up => new(X, Y + 1);
        public GridPosition Down => new(X, Y - 1);
        public GridPosition Left => new(X - 1, Y);
        public GridPosition Right => new(X + 1, Y);

        // Проверка соседства (только горизонталь/вертикаль)
        public bool IsAdjacentTo(GridPosition other)
        {
            int dx = Math.Abs(X - other.X);
            int dy = Math.Abs(Y - other.Y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        // IEquatable
        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);

        // Operators
        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Y})";
    }
}
```

**Ключевые решения:**
- `readonly struct` для производительности и immutability
- `IEquatable<T>` для корректного сравнения в коллекциях
- Свойства `Up/Down/Left/Right` для удобной навигации
- `IsAdjacentTo` для проверки соседства при свапе

---

## 2. PieceType.cs

**Назначение:** Enum типов элементов с расширяющими методами.

```csharp
namespace Match3.Core
{
    /// <summary>
    /// Типы элементов Match3
    /// </summary>
    public enum PieceType
    {
        None = 0,    // Пустая ячейка / отсутствие типа
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6
    }

    public static class PieceTypeExtensions
    {
        /// <summary>
        /// Количество игровых типов (без None)
        /// </summary>
        public const int PlayableCount = 6;

        /// <summary>
        /// Проверяет, является ли тип игровым (не None)
        /// </summary>
        public static bool IsPlayable(this PieceType type) => type != PieceType.None;
    }
}
```

**Ключевые решения:**
- `None = 0` для обозначения пустых ячеек
- Extension методы для удобства
- Константа `PlayableCount` для генерации случайных типов

---

## 3. IPiece.cs

**Назначение:** Контракт для игрового элемента. Минимальный интерфейс.

```csharp
using System;
using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// Интерфейс игрового элемента
    /// </summary>
    public interface IPiece
    {
        /// <summary>
        /// Тип элемента
        /// </summary>
        PieceType Type { get; }

        /// <summary>
        /// Позиция в сетке
        /// </summary>
        GridPosition Position { get; set; }

        /// <summary>
        /// GameObject элемента
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Устанавливает мировую позицию (для анимаций)
        /// </summary>
        void SetWorldPosition(Vector3 position);

        /// <summary>
        /// Элемент уничтожен
        /// </summary>
        event Action<IPiece> OnDestroyed;
    }
}
```

**Ключевые решения:**
- Минимальный набор: Type, Position, GameObject
- `SetWorldPosition` отделён от Position для анимаций
- Event `OnDestroyed` для уведомления системы

---

## 4. IGrid.cs

**Назначение:** Контракт для сетки. Конвертация координат.

```csharp
using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// Интерфейс сетки
    /// </summary>
    public interface IGrid
    {
        /// <summary>
        /// Ширина сетки
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Высота сетки
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Конвертирует позицию сетки в мировые координаты
        /// </summary>
        Vector3 GridToWorld(GridPosition position);

        /// <summary>
        /// Конвертирует мировые координаты в позицию сетки
        /// </summary>
        GridPosition WorldToGrid(Vector3 worldPosition);

        /// <summary>
        /// Проверяет валидность позиции
        /// </summary>
        bool IsValidPosition(GridPosition position);
    }
}
```

---

## 5. IBoardState.cs

**Назначение:** Состояние доски. Чтение/запись элементов в ячейки.

```csharp
using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Состояние игровой доски
    /// </summary>
    public interface IBoardState
    {
        /// <summary>
        /// Ширина доски
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Высота доски
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Получить элемент в позиции
        /// </summary>
        IPiece GetPieceAt(GridPosition position);

        /// <summary>
        /// Установить элемент в позицию
        /// </summary>
        void SetPieceAt(GridPosition position, IPiece piece);

        /// <summary>
        /// Очистить ячейку
        /// </summary>
        void ClearCell(GridPosition position);

        /// <summary>
        /// Проверить, пуста ли ячейка
        /// </summary>
        bool IsEmpty(GridPosition position);

        /// <summary>
        /// Получить все позиции на доске
        /// </summary>
        IEnumerable<GridPosition> AllPositions { get; }

        /// <summary>
        /// Событие изменения доски
        /// </summary>
        event Action OnBoardChanged;
    }
}
```

---

## 6. IMatchChecker.cs

**Назначение:** Быстрая проверка - создаст ли элемент матч в позиции. Используется при спауне.

```csharp
namespace Match3.Core
{
    /// <summary>
    /// Проверка потенциального матча (для спауна без матчей)
    /// </summary>
    public interface IMatchChecker
    {
        /// <summary>
        /// Проверяет, создаст ли тип элемента матч в данной позиции
        /// </summary>
        bool WouldCreateMatch(GridPosition position, PieceType type);
    }
}
```

---

## 7. IMatchDetector.cs

**Назначение:** Полный поиск матчей на доске.

```csharp
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Результат матча
    /// </summary>
    public readonly struct MatchResult
    {
        public readonly IReadOnlyList<GridPosition> Positions;
        public readonly PieceType Type;

        public MatchResult(IReadOnlyList<GridPosition> positions, PieceType type)
        {
            Positions = positions;
            Type = type;
        }

        public int Count => Positions?.Count ?? 0;
    }

    /// <summary>
    /// Детектор матчей
    /// </summary>
    public interface IMatchDetector
    {
        /// <summary>
        /// Найти все матчи на доске
        /// </summary>
        IReadOnlyList<MatchResult> FindAllMatches();

        /// <summary>
        /// Проверить наличие матчей после свапа
        /// </summary>
        bool HasMatchAt(GridPosition position);

        /// <summary>
        /// Найти матч, включающий позицию
        /// </summary>
        MatchResult? FindMatchAt(GridPosition position);
    }
}
```

**Ключевые решения:**
- `MatchResult` struct содержит позиции и тип матча
- `FindAllMatches` для каскадов
- `HasMatchAt` для быстрой проверки после свапа

---

## 8. ISpawner.cs

**Назначение:** Создание новых элементов.

```csharp
using System;

namespace Match3.Core
{
    /// <summary>
    /// Спаунер элементов
    /// </summary>
    public interface ISpawner
    {
        /// <summary>
        /// Создать элемент указанного типа в позиции
        /// </summary>
        IPiece Spawn(PieceType type, GridPosition position);

        /// <summary>
        /// Создать случайный элемент в позиции
        /// </summary>
        IPiece SpawnRandom(GridPosition position);

        /// <summary>
        /// Создать случайный элемент, который не создаст матч
        /// </summary>
        IPiece SpawnRandomNoMatch(GridPosition position, IMatchChecker checker);

        /// <summary>
        /// Вернуть элемент в пул
        /// </summary>
        void Despawn(IPiece piece);

        /// <summary>
        /// Событие спауна
        /// </summary>
        event Action<IPiece> OnPieceSpawned;
    }
}
```

---

## 9. ISwappable.cs

**Назначение:** Маркер-интерфейс для элементов, которые можно свапать.

```csharp
namespace Match3.Core
{
    /// <summary>
    /// Элемент, который можно менять местами
    /// </summary>
    public interface ISwappable
    {
        /// <summary>
        /// Можно ли сейчас свапать этот элемент
        /// </summary>
        bool CanSwap { get; }

        /// <summary>
        /// Позиция элемента
        /// </summary>
        GridPosition Position { get; }
    }
}
```

---

## 10. IScoreHandler.cs

**Назначение:** Обработка очков.

```csharp
using System;

namespace Match3.Core
{
    /// <summary>
    /// Обработчик очков
    /// </summary>
    public interface IScoreHandler
    {
        /// <summary>
        /// Текущий счёт
        /// </summary>
        int CurrentScore { get; }

        /// <summary>
        /// Добавить очки за матч
        /// </summary>
        void AddScore(int matchCount, PieceType type);

        /// <summary>
        /// Сбросить счёт
        /// </summary>
        void Reset();

        /// <summary>
        /// Событие изменения счёта
        /// </summary>
        event Action<int> OnScoreChanged;
    }
}
```

---

## 11. IGameEvents.cs

**Назначение:** Глобальные события игры.

```csharp
using System;

namespace Match3.Core
{
    /// <summary>
    /// Глобальные игровые события
    /// </summary>
    public interface IGameEvents
    {
        event Action OnGameStarted;
        event Action OnGameEnded;
        event Action OnTurnStarted;
        event Action OnTurnEnded;
        event Action<int> OnCascade; // уровень каскада
    }
}
```

---

## Чеклист реализации

- [ ] Создать папку `Assets/Scripts/Core/`
- [ ] Создать папку `Assets/Scripts/Core/Interfaces/`
- [ ] Реализовать `GridPosition.cs`
- [ ] Реализовать `PieceType.cs`
- [ ] Реализовать `IPiece.cs`
- [ ] Реализовать `IGrid.cs`
- [ ] Реализовать `IBoardState.cs`
- [ ] Реализовать `IMatchChecker.cs`
- [ ] Реализовать `IMatchDetector.cs`
- [ ] Реализовать `ISpawner.cs`
- [ ] Реализовать `ISwappable.cs`
- [ ] Реализовать `IScoreHandler.cs`
- [ ] Реализовать `IGameEvents.cs`
- [ ] Проверить компиляцию в Unity

---

## Зависимости

Этот шаг **не имеет зависимостей** - он первый.

## Используется в

- **Step 2 (Grid):** IGrid, GridPosition
- **Step 3 (Pieces):** IPiece, PieceType, ISwappable
- **Step 4 (Spawner):** ISpawner, IMatchChecker
- **Step 5 (Match):** IMatchDetector, IMatchChecker, MatchResult
- **Step 6 (Destruction):** IScoreHandler
- **Step 7 (Gravity):** IBoardState, ISpawner
- **Step 8 (Swap):** ISwappable
- **Step 9 (Game Loop):** IGameEvents, IBoardState

---

## Время выполнения

~15-20 минут (чистый код без логики, только контракты)
