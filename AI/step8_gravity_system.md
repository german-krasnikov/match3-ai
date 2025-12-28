# Step 8: GRAVITY SYSTEM — Падение элементов

> **Статус:** План реализации
> **Зависимости:** Grid (step 2), Elements (step 3), Spawn (step 4)
> **Выход:** Система гравитации с анимацией падения

---

## Обзор

Gravity System отвечает за:
1. Обнаружение пустых ячеек после уничтожения элементов
2. Перемещение существующих элементов вниз (падение)
3. Запрос новых элементов у SpawnSystem для заполнения верхних пустот
4. Анимацию падения через DOTween

---

## Файловая структура

```
Assets/Scripts/Gravity/
└── GravityComponent.cs
```

---

## Интерфейс IGravitySystem (из Core, step 1)

```csharp
public interface IGravitySystem
{
    Task ApplyGravity();
    bool HasEmptyCells();

    event Action OnGravityStarted;
    event Action OnGravityCompleted;
}
```

---

## GravityComponent.cs

### Полная реализация

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace Match3.Gravity
{
    /// <summary>
    /// Применяет гравитацию к элементам сетки.
    /// После уничтожения матчей элементы падают вниз,
    /// пустые места заполняются новыми элементами сверху.
    /// </summary>
    public class GravityComponent : MonoBehaviour, IGravitySystem
    {
        // === СОБЫТИЯ ===
        public event Action OnGravityStarted;
        public event Action OnGravityCompleted;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SpawnComponent _spawn;

        // === НАСТРОЙКИ ===
        [Header("Settings")]
        [SerializeField] private float _fallDuration = 0.3f;
        [SerializeField] private Ease _fallEase = Ease.OutBounce;

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Проверяет наличие пустых ячеек в сетке
        /// </summary>
        public bool HasEmptyCells()
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    if (_grid.GetElementAt(new Vector2Int(x, y)) == null)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Применяет гравитацию ко всем колонкам параллельно
        /// </summary>
        public async Task ApplyGravity()
        {
            OnGravityStarted?.Invoke();

            // Обрабатываем все колонки параллельно
            var columnTasks = new List<Task>();
            for (int x = 0; x < _grid.Width; x++)
            {
                columnTasks.Add(ProcessColumn(x));
            }

            await Task.WhenAll(columnTasks);

            OnGravityCompleted?.Invoke();
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Обрабатывает одну колонку: падение + спаун новых
        /// </summary>
        private async Task ProcessColumn(int column)
        {
            // Фаза 1: Падение существующих элементов
            await FallExistingElements(column);

            // Фаза 2: Спаун новых элементов сверху
            await SpawnNewElements(column);
        }

        /// <summary>
        /// Опускает существующие элементы на пустые места
        /// </summary>
        private async Task FallExistingElements(int column)
        {
            var animations = new List<Task>();

            // Проходим снизу вверх
            for (int y = 0; y < _grid.Height; y++)
            {
                var pos = new Vector2Int(column, y);

                // Если ячейка не пустая - пропускаем
                if (_grid.GetElementAt(pos) != null)
                    continue;

                // Ищем первый элемент выше
                for (int above = y + 1; above < _grid.Height; above++)
                {
                    var abovePos = new Vector2Int(column, above);
                    var element = _grid.GetElementAt(abovePos);

                    if (element != null)
                    {
                        // Перемещаем элемент в сетке
                        _grid.ClearCell(abovePos);
                        _grid.SetElementAt(pos, element);

                        // Анимируем падение
                        var task = AnimateFall(
                            (ElementComponent)element,
                            abovePos,
                            pos
                        );
                        animations.Add(task);
                        break;
                    }
                }
            }

            await Task.WhenAll(animations);
        }

        /// <summary>
        /// Спавнит новые элементы для заполнения пустот сверху
        /// </summary>
        private async Task SpawnNewElements(int column)
        {
            var animations = new List<Task>();

            // Считаем сколько пустых ячеек
            int emptyCount = 0;
            for (int y = _grid.Height - 1; y >= 0; y--)
            {
                var pos = new Vector2Int(column, y);
                if (_grid.GetElementAt(pos) == null)
                    emptyCount++;
                else
                    break; // Пустоты только сверху после падения
            }

            // Спавним новые элементы
            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = _grid.Height - emptyCount + i;
                var targetPos = new Vector2Int(column, targetY);

                // Начальная позиция над сеткой
                var startPos = new Vector2Int(column, _grid.Height + i);

                // Спавним элемент
                var element = _spawn.SpawnAtTop(column);
                _grid.SetElementAt(targetPos, element);

                // Позиционируем над сеткой
                element.transform.position = _grid.GridToWorld(startPos);

                // Анимируем падение
                var task = AnimateFall(element, startPos, targetPos);
                animations.Add(task);
            }

            await Task.WhenAll(animations);
        }

        /// <summary>
        /// Анимирует падение элемента из одной позиции в другую
        /// </summary>
        private async Task AnimateFall(ElementComponent element, Vector2Int from, Vector2Int to)
        {
            var targetWorldPos = _grid.GridToWorld(to);

            // Обновляем grid position в элементе
            element.GridPosition = to;

            // Анимация падения
            var tween = element.transform
                .DOMove(targetWorldPos, _fallDuration)
                .SetEase(_fallEase);

            await tween.AsyncWaitForCompletion();
        }
    }
}
```

---

## Алгоритм гравитации (детально)

### Фаза 1: Падение существующих элементов

```
Для каждой колонки (0 → Width):
  Для каждой строки снизу вверх (0 → Height):
    Если ячейка пустая:
      Найти первый элемент выше
      Если найден:
        1. Очистить верхнюю ячейку в Grid
        2. Установить элемент в нижнюю ячейку
        3. Запустить анимацию падения
```

**Пример:**
```
До:        После падения:
[A]        [A]
[ ]   →    [B]
[B]        [C]
[C]        [ ] ← пустота теперь сверху
```

### Фаза 2: Спаун новых элементов

```
Для каждой колонки:
  Посчитать пустые ячейки сверху
  Для каждой пустоты:
    1. SpawnAtTop() создаёт элемент над сеткой
    2. Установить в Grid на целевую позицию
    3. Позиционировать визуально над сеткой
    4. Анимировать падение на место
```

**Пример:**
```
Над сеткой:  [N1][N2]  (новые элементы)
             ─────────
Сетка:       [A]       → После: [N1]
             [B]                [N2]
             [ ]                [A]
             [ ]                [B]
```

---

## Диаграмма потока

```
ApplyGravity()
     │
     ▼
OnGravityStarted
     │
     ▼
┌─────────────────────────────────────┐
│  Параллельно для всех колонок:      │
│  ┌───────────────────────────────┐  │
│  │  ProcessColumn(x)             │  │
│  │       │                       │  │
│  │       ▼                       │  │
│  │  FallExistingElements()       │  │
│  │       │                       │  │
│  │       ▼                       │  │
│  │  SpawnNewElements()           │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
     │
     ▼
Task.WhenAll(columnTasks)
     │
     ▼
OnGravityCompleted
```

---

## STUB-ы для тестирования

### Без Grid и Spawn

```csharp
// В тестовом скрипте:
public class GravityTestStub : MonoBehaviour
{
    // STUB Grid
    private Dictionary<Vector2Int, IGridElement> _stubGrid = new();

    public IGridElement StubGetElementAt(Vector2Int pos)
    {
        _stubGrid.TryGetValue(pos, out var element);
        return element;
    }

    public void StubSetElementAt(Vector2Int pos, IGridElement element)
    {
        _stubGrid[pos] = element;
    }

    public void StubClearCell(Vector2Int pos)
    {
        _stubGrid.Remove(pos);
    }

    // STUB Spawn
    public ElementComponent StubSpawnAtTop(int column)
    {
        // Создать простой GameObject для теста
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var element = go.AddComponent<ElementComponent>();
        return element;
    }
}
```

---

## Интеграция с GameLoop (step 9)

```csharp
// В GameLoopComponent:
private async Task ProcessCascade()
{
    while (true)
    {
        // Проверяем матчи
        var matches = _matchDetection.FindAllMatches();
        if (matches.Count == 0)
            break;

        // Уничтожаем
        await _destruction.DestroyElements(matches);

        // Применяем гравитацию ← ЗДЕСЬ
        await _gravity.ApplyGravity();
    }
}
```

---

## Настройки анимации

| Параметр | Значение | Описание |
|----------|----------|----------|
| `_fallDuration` | 0.3f | Время падения на одну ячейку |
| `_fallEase` | OutBounce | Эффект отскока при приземлении |

### Альтернативные Ease варианты:
- `Ease.OutQuad` — плавное замедление
- `Ease.InQuad` — ускорение (как реальное падение)
- `Ease.OutBack` — небольшой перелёт и возврат

---

## Edge Cases

### 1. Пустая колонка
```
[ ]    После:   [N1]
[ ]     →       [N2]
[ ]             [N3]
```
Все элементы — новые, все падают сверху.

### 2. Полная колонка
```
[A]    После:   [A]
[B]     →       [B]
[C]             [C]
```
Ничего не падает, ничего не спавнится.

### 3. Дыра в середине
```
[A]    После:   [N1]
[ ]     →       [A]
[B]             [B]
```
Сначала [A] падает вниз, потом [N1] спавнится сверху.

### 4. Множественные дыры
```
[A]    После:   [N1]
[ ]     →       [N2]
[ ]             [A]
[B]             [B]
```
Элементы заполняют все пустоты снизу вверх.

---

## Чеклист реализации

- [x] Создать папку `Assets/Scripts/Gravity/`
- [x] Создать `GravityComponent.cs`
- [x] Реализовать `HasEmptyCells()`
- [x] Реализовать `ApplyGravity()` с параллельной обработкой колонок
- [x] Реализовать `FallExistingElements()` — падение существующих
- [x] Реализовать `SpawnNewElements()` — спаун новых сверху
- [x] Реализовать `AnimateFall()` с DOTween
- [x] Добавить события `OnGravityStarted` / `OnGravityCompleted`
- [x] Создать `GravityTester.cs` для тестирования
- [x] Создать `GravitySceneSetup.cs` Editor скрипт
- [ ] Протестировать с ручным удалением элементов
- [ ] Проверить edge cases (пустая колонка, множественные дыры)

---

## Зависимости (ожидаемые из других шагов)

### От Grid (step 2):
```csharp
int Width { get; }
int Height { get; }
IGridElement GetElementAt(Vector2Int pos);
void SetElementAt(Vector2Int pos, IGridElement element);
void ClearCell(Vector2Int pos);
Vector3 GridToWorld(Vector2Int gridPos);
```

### От Spawn (step 4):
```csharp
ElementComponent SpawnAtTop(int column);
```

### От Elements (step 3):
```csharp
Vector2Int GridPosition { get; set; }
Transform transform;
```

---

## Примечания

1. **Параллельность**: Колонки обрабатываются параллельно для производительности. Внутри колонки — последовательно.

2. **Порядок операций**: Сначала падают существующие элементы, потом спавнятся новые. Это важно, иначе новые элементы могут столкнуться с падающими.

3. **Grid sync**: Обновление Grid происходит ДО анимации. Визуал догоняет логику.

4. **DOTween**: Используем `AsyncWaitForCompletion()` для async/await паттерна.
