# Этап 7: Match Detection - Детальный План Реализации

## Статус: ЗАВЕРШЁН ✅

---

## Реализация (итоговая)

### Созданные файлы

| Файл | Строк | Описание |
|------|-------|----------|
| `Assets/Scripts/Match/Match.cs` | 38 | readonly struct данных матча |
| `Assets/Scripts/Match/MatchFinder.cs` | 201 | Алгоритм поиска матчей |
| `Assets/Scripts/Match/MatchHighlighter.cs` | 68 | Debug визуализация через Gizmos |
| `Assets/Scripts/Editor/MatchSystemSetup.cs` | 56 | Editor menu setup |

### Изменённые файлы

| Файл | Изменение |
|------|-----------|
| `Assets/Scripts/Swap/SwapHandler.cs` | +`using Match3.Matching`, +`_matchFinder` field, заменён `CheckForMatch()` |

### Диаграмма компонентов

```
GameManager (GameObject)
├── GridComponent          [Stage 1]
├── BoardComponent         [Stage 4]
├── ElementPool            [Stage 3]
├── ElementFactory         [Stage 3]
├── InitialBoardSpawner    [Stage 3]
├── InputBlocker           [Stage 5]
├── InputDetector          [Stage 5]
├── SelectionHighlighter   [Stage 5]
├── SwapAnimator           [Stage 6]
├── SwapHandler            [Stage 6] ← _matchFinder добавлен
├── MatchFinder            [Stage 7] ← NEW
└── MatchHighlighter       [Stage 7] ← NEW (debug)
```

### Поток данных

```
SwapHandler.CheckForMatch(posA, posB)
         │
         ▼
MatchFinder.WouldCreateMatch(posA, posB)
         │
         ├── FindMatchesAt(posA) ──► horizontal + vertical scan
         │
         └── FindMatchesAt(posB) ──► horizontal + vertical scan
         │
         ▼
    bool hasMatch
         │
    ┌────┴────┐
   true      false
    │         │
    ▼         ▼
 Complete   Revert
```

---

## Обзор

Match Detection находит совпадения 3+ элементов одного типа по горизонтали или вертикали. Интегрируется со SwapHandler для валидации ходов.

### Связь с другими системами

```
SwapHandler.CheckForMatch()
         │
         ▼
┌─────────────────────┐
│     MatchFinder     │
│                     │
│ 1. FindMatchesAt()  │ ← Проверка конкретных позиций
│ 2. FindAllMatches() │ ← Полный скан доски (для cascade)
│                     │
│ Горизонтальный скан │
│ Вертикальный скан   │
│ Объединение матчей  │
└─────────┬───────────┘
          │
          ▼
    List<Match>
          │
          ▼
   [Этап 8: DestroyHandler]
```

### Зависимости

- `BoardComponent` - получение типов элементов через `GetElementType()`
- `SwapHandler` - интеграция для валидации свапов
- `GridComponent` - размеры сетки (через BoardComponent)

---

## Архитектура

### Компоненты

| Компонент | Ответственность |
|-----------|-----------------|
| `Match` | Структура данных матча (позиции, тип) |
| `MatchFinder` | Алгоритм поиска матчей |
| `MatchHighlighter` | Debug-визуализация найденных матчей |

### Почему MatchFinder — MonoBehaviour?

1. Видимость в Inspector для дебага
2. Возможность добавить SerializeField настройки (min match length)
3. Консистентность с остальной архитектурой
4. Легко интегрировать через Editor Setup

---

## Файлы для создания

```
Assets/Scripts/Match/
├── Match.cs            # Структура данных
├── MatchFinder.cs      # Алгоритм поиска
└── MatchHighlighter.cs # Debug визуализация

Assets/Scripts/Editor/
└── MatchSystemSetup.cs # Editor setup
```

---

## 7.1 Match.cs

### Назначение

Структура данных для хранения информации о найденном совпадении.

### Дизайн

```
Match {
    ElementType Type      - тип элементов в матче
    List<Vector2Int> Positions - позиции всех элементов
    MatchOrientation Orientation - горизонтальный/вертикальный/крест
}
```

### Реализованный код

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Elements;

namespace Match3.Matching
{
    public enum MatchOrientation
    {
        Horizontal,
        Vertical,
        Cross
    }

    public readonly struct Match
    {
        public readonly ElementType Type;
        public readonly IReadOnlyList<Vector2Int> Positions;
        public readonly MatchOrientation Orientation;

        public int Count => Positions.Count;
        public bool IsValid => Positions != null && Positions.Count >= 3;

        public Match(ElementType type, List<Vector2Int> positions, MatchOrientation orientation)
        {
            Type = type;
            Positions = positions;
            Orientation = orientation;
        }

        public static Match Merge(Match a, Match b)
        {
            var positions = new HashSet<Vector2Int>(a.Positions);
            foreach (var pos in b.Positions)
                positions.Add(pos);

            return new Match(a.Type, new List<Vector2Int>(positions), MatchOrientation.Cross);
        }

        public override string ToString() => $"Match({Type}, {Count} elements, {Orientation})";
    }
}
```

### Почему readonly struct?

1. Immutable — после создания не меняется
2. Value-type — меньше аллокаций на GC
3. Безопасный для многопоточности
4. `IReadOnlyList` — защита от модификации извне

---

## 7.2 MatchFinder.cs

### Назначение

Находит все матчи на доске или в конкретных позициях.

### Алгоритм поиска

```
Горизонтальный проход (для каждой строки):
┌─────────────────────────────────────┐
│ R R R B G G G G Y                   │
│ ←───→   ←─────→                     │
│  3      4 (Match!)                  │
└─────────────────────────────────────┘

Вертикальный проход (для каждого столбца):
┌───┐
│ R │
│ R │ ← 3 (Match!)
│ R │
│ B │
│ G │
└───┘

Объединение пересекающихся:
    R
    R
R R R ← Горизонталь + Вертикаль = Cross
    R
```

### Реализованный код

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Elements;

namespace Match3.Matching
{
    public class MatchFinder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _minMatchLength = 3;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;

        private readonly List<Match> _matchBuffer = new();
        private readonly List<Vector2Int> _lineBuffer = new();

        public List<Match> FindAllMatches()
        {
            _matchBuffer.Clear();

            for (int y = 0; y < _board.Height; y++)
                FindHorizontalMatchesInRow(y, _matchBuffer);

            for (int x = 0; x < _board.Width; x++)
                FindVerticalMatchesInColumn(x, _matchBuffer);

            return MergeIntersectingMatches(_matchBuffer);
        }

        public List<Match> FindMatchesAt(Vector2Int position)
        {
            _matchBuffer.Clear();

            var horizontal = FindHorizontalMatchAt(position);
            if (horizontal.IsValid)
                _matchBuffer.Add(horizontal);

            var vertical = FindVerticalMatchAt(position);
            if (vertical.IsValid)
                _matchBuffer.Add(vertical);

            return MergeIntersectingMatches(_matchBuffer);
        }

        public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
        {
            return FindMatchesAt(posA).Count > 0 || FindMatchesAt(posB).Count > 0;
        }

        private void FindHorizontalMatchesInRow(int y, List<Match> results)
        {
            int x = 0;
            while (x < _board.Width)
            {
                var type = _board.GetElementType(new Vector2Int(x, y));
                if (type == null || type == ElementType.None) { x++; continue; }

                _lineBuffer.Clear();
                _lineBuffer.Add(new Vector2Int(x, y));

                int nextX = x + 1;
                while (nextX < _board.Width && _board.GetElementType(new Vector2Int(nextX, y)) == type)
                {
                    _lineBuffer.Add(new Vector2Int(nextX, y));
                    nextX++;
                }

                if (_lineBuffer.Count >= _minMatchLength)
                    results.Add(new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Horizontal));

                x = nextX;
            }
        }

        private void FindVerticalMatchesInColumn(int x, List<Match> results)
        {
            int y = 0;
            while (y < _board.Height)
            {
                var type = _board.GetElementType(new Vector2Int(x, y));
                if (type == null || type == ElementType.None) { y++; continue; }

                _lineBuffer.Clear();
                _lineBuffer.Add(new Vector2Int(x, y));

                int nextY = y + 1;
                while (nextY < _board.Height && _board.GetElementType(new Vector2Int(x, nextY)) == type)
                {
                    _lineBuffer.Add(new Vector2Int(x, nextY));
                    nextY++;
                }

                if (_lineBuffer.Count >= _minMatchLength)
                    results.Add(new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Vertical));

                y = nextY;
            }
        }

        private Match FindHorizontalMatchAt(Vector2Int pos)
        {
            var type = _board.GetElementType(pos);
            if (type == null || type == ElementType.None) return default;

            _lineBuffer.Clear();
            _lineBuffer.Add(pos);

            for (int left = pos.x - 1; left >= 0; left--)
            {
                if (_board.GetElementType(new Vector2Int(left, pos.y)) != type) break;
                _lineBuffer.Insert(0, new Vector2Int(left, pos.y));
            }

            for (int right = pos.x + 1; right < _board.Width; right++)
            {
                if (_board.GetElementType(new Vector2Int(right, pos.y)) != type) break;
                _lineBuffer.Add(new Vector2Int(right, pos.y));
            }

            return _lineBuffer.Count >= _minMatchLength
                ? new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Horizontal)
                : default;
        }

        private Match FindVerticalMatchAt(Vector2Int pos)
        {
            var type = _board.GetElementType(pos);
            if (type == null || type == ElementType.None) return default;

            _lineBuffer.Clear();
            _lineBuffer.Add(pos);

            for (int down = pos.y - 1; down >= 0; down--)
            {
                if (_board.GetElementType(new Vector2Int(pos.x, down)) != type) break;
                _lineBuffer.Insert(0, new Vector2Int(pos.x, down));
            }

            for (int up = pos.y + 1; up < _board.Height; up++)
            {
                if (_board.GetElementType(new Vector2Int(pos.x, up)) != type) break;
                _lineBuffer.Add(new Vector2Int(pos.x, up));
            }

            return _lineBuffer.Count >= _minMatchLength
                ? new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Vertical)
                : default;
        }

        private List<Match> MergeIntersectingMatches(List<Match> matches)
        {
            if (matches.Count <= 1) return new List<Match>(matches);

            var result = new List<Match>();
            var merged = new bool[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (merged[i]) continue;
                var current = matches[i];

                for (int j = i + 1; j < matches.Count; j++)
                {
                    if (merged[j] || matches[i].Type != matches[j].Type) continue;
                    if (MatchesIntersect(current, matches[j]))
                    {
                        current = Match.Merge(current, matches[j]);
                        merged[j] = true;
                    }
                }
                result.Add(current);
            }
            return result;
        }

        private bool MatchesIntersect(Match a, Match b)
        {
            foreach (var posA in a.Positions)
                foreach (var posB in b.Positions)
                    if (posA == posB) return true;
            return false;
        }
    }
}
```

### Оптимизации

| Оптимизация | Описание |
|-------------|----------|
| Reusable buffers | `_matchBuffer`, `_lineBuffer` переиспользуются |
| Early exit | `FindMatchesAt()` для быстрой проверки свапа |
| Linear scan | O(n) проход вместо O(n²) для каждой линии |

### Сложность алгоритма

- `FindAllMatches()`: O(W*H) — линейный проход по всем ячейкам
- `FindMatchesAt()`: O(W+H) — только горизонталь и вертикаль от точки
- `MergeIntersectingMatches()`: O(M²) где M — кол-во матчей (обычно < 10)

---

## 7.3 MatchHighlighter.cs

### Назначение

Debug-компонент для визуализации найденных матчей. Полезен для тестирования алгоритма.

### Реализованный код

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;

namespace Match3.Matching
{
    public class MatchHighlighter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private float _highlightDuration = 1f;
        [SerializeField] private Color _horizontalColor = new(1f, 0.5f, 0f, 0.7f);
        [SerializeField] private Color _verticalColor = new(0f, 0.5f, 1f, 0.7f);
        [SerializeField] private Color _crossColor = new(1f, 0f, 1f, 0.7f);

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private MatchFinder _matchFinder;

        private List<Match> _currentMatches = new();
        private float _highlightTimer;

        public void HighlightMatches(List<Match> matches)
        {
            _currentMatches = matches;
            _highlightTimer = _highlightDuration;
        }

        [ContextMenu("Find And Highlight All Matches")]
        public void FindAndHighlightAll()
        {
            if (_matchFinder == null) return;

            var matches = _matchFinder.FindAllMatches();
            HighlightMatches(matches);

            Debug.Log($"[MatchHighlighter] Found {matches.Count} matches:");
            foreach (var match in matches)
                Debug.Log($"  {match}");
        }

        private void Update()
        {
            if (_highlightTimer > 0)
            {
                _highlightTimer -= Time.deltaTime;
                if (_highlightTimer <= 0)
                    _currentMatches.Clear();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _grid == null || _currentMatches.Count == 0)
                return;

            foreach (var match in _currentMatches)
            {
                Gizmos.color = match.Orientation switch
                {
                    MatchOrientation.Horizontal => _horizontalColor,
                    MatchOrientation.Vertical => _verticalColor,
                    MatchOrientation.Cross => _crossColor,
                    _ => Color.white
                };

                foreach (var pos in match.Positions)
                {
                    var worldPos = _grid.GridToWorld(pos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.3f);
                }
            }
        }
    }
}
```

### Использование

1. В Play Mode → ПКМ на компоненте → "Find And Highlight All Matches"
2. Gizmos покажут все найденные матчи разными цветами
3. Автоматически исчезнут через `_highlightDuration`

---

## 7.4 Интеграция в SwapHandler

### Изменения в SwapHandler.cs (реализовано)

```csharp
// Добавлен using
using Match3.Matching;

// Добавлена зависимость
[Header("Dependencies")]
[SerializeField] private BoardComponent _board;
[SerializeField] private GridComponent _grid;
[SerializeField] private InputDetector _inputDetector;
[SerializeField] private InputBlocker _inputBlocker;
[SerializeField] private SwapAnimator _swapAnimator;
[SerializeField] private MatchFinder _matchFinder;  // ← NEW

// Заменён метод CheckForMatch
private bool CheckForMatch(Vector2Int posA, Vector2Int posB)
{
    return _matchFinder.WouldCreateMatch(posA, posB);
}
```

### После интеграции

```
OnSwapRequested(posA, posB)
         │
         ▼
   Animate Swap
         │
         ▼
   Update Board Data
         │
         ▼
   MatchFinder.WouldCreateMatch() ← ИНТЕГРАЦИЯ
         │
    ┌────┴────┐
   YES        NO
    │         │
    ▼         ▼
  Done    Revert Swap
```

---

## 7.5 MatchSystemSetup.cs (Editor)

### Реализованный код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Matching;
using Match3.Grid;
using Match3.Board;
using Match3.Swap;

namespace Match3.Editor
{
    public static class MatchSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 7 - Match System")]
        public static void SetupMatchSystem()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            var board = grid.GetComponent<BoardComponent>();
            if (board == null)
            {
                Debug.LogError("[Match3] BoardComponent not found. Run Stage 4 setup first.");
                return;
            }

            var swapHandler = grid.GetComponent<SwapHandler>();
            if (swapHandler == null)
            {
                Debug.LogError("[Match3] SwapHandler not found. Run Stage 6 setup first.");
                return;
            }

            var go = grid.gameObject;

            // MatchFinder
            var matchFinder = go.GetComponent<MatchFinder>();
            if (matchFinder == null)
                matchFinder = Undo.AddComponent<MatchFinder>(go);

            SetField(matchFinder, "_board", board);

            // MatchHighlighter
            var matchHighlighter = go.GetComponent<MatchHighlighter>();
            if (matchHighlighter == null)
                matchHighlighter = Undo.AddComponent<MatchHighlighter>(go);

            SetField(matchHighlighter, "_grid", grid);
            SetField(matchHighlighter, "_matchFinder", matchFinder);

            // Wire SwapHandler
            SetField(swapHandler, "_matchFinder", matchFinder);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Match System setup complete!");
        }

        private static void SetField<T>(Component component, string fieldName, T value) where T : Object
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `Match.cs` | — | Компиляция |
| 2 | `MatchFinder.cs` | BoardComponent | Unit test / Console log |
| 3 | `MatchHighlighter.cs` | Grid, MatchFinder | Gizmos в Scene view |
| 4 | `MatchSystemSetup.cs` | Все выше | Меню создаёт компоненты |
| 5 | Интеграция в SwapHandler | MatchFinder | Свап с невалидным ходом реверсится |

---

## Тестирование

### Тест 1: Ручная проверка матчей

1. Запустить Play Mode
2. В Inspector на MatchHighlighter → ПКМ → "Find And Highlight All Matches"
3. Проверить, что начальная доска не имеет матчей (по дизайну InitialBoardSpawner)
4. В консоли: "Found 0 matches"

### Тест 2: Проверка свапа с матчем

1. Найти два соседних элемента, где свап создаст линию из 3
2. Сделать свап
3. Элементы должны остаться на новых позициях
4. В консоли: SwapCompleted

### Тест 3: Проверка свапа без матча

1. Найти два соседних элемента, где свап НЕ создаст матч
2. Сделать свап
3. Элементы должны вернуться на исходные позиции
4. В консоли: SwapReverted

### Debug через Console

```csharp
// Временно добавить в MatchFinder.WouldCreateMatch():
Debug.Log($"WouldCreateMatch({posA}, {posB}): checking...");
var result = /* original logic */;
Debug.Log($"  -> {result}");
return result;
```

---

## Визуализация алгоритма

### Пример 1: Простой горизонтальный матч

```
Доска:          После FindHorizontalMatchesInRow(2):

y=3: B G Y R P
y=2: R R R G B  → Match(Red, [(0,2),(1,2),(2,2)], Horizontal)
y=1: G Y B P R
y=0: P B G Y R
     0 1 2 3 4
```

### Пример 2: L-образный матч (merge)

```
Доска:             Найденные матчи:

y=3: B R Y G P     1. Horizontal: R R R (y=2)
y=2: R R R G B     2. Vertical: R R R (x=0)
y=1: R Y B P R
y=0: R B G Y R     После merge:
     0 1 2 3 4     Cross: [(0,0),(0,1),(0,2),(1,2),(2,2)]
```

### Пример 3: FindMatchesAt для свапа

```
Свап (2,1) ↔ (2,2):

До свапа:           После свапа:
y=2: R R G G B      y=2: R R B G B
y=1: G Y B P R      y=1: G Y G P R   ← G теперь в (2,1)
     0 1 2 3 4           0 1 2 3 4

FindMatchesAt(2,2) → No match (B B не формирует 3)
FindMatchesAt(2,1) → No match (G Y G не подряд)

WouldCreateMatch = false → REVERT
```

---

## Диаграмма компонентов на GameObject

После Stage 7 на GameManager объекте:

```
GameManager (GameObject)
├── GridComponent          [Stage 1]
├── BoardComponent         [Stage 4]
├── ElementPool            [Stage 3]
├── ElementFactory         [Stage 3]
├── InitialBoardSpawner    [Stage 3]
├── InputBlocker           [Stage 5]
├── InputDetector          [Stage 5]
├── SelectionHighlighter   [Stage 5]
├── SwapAnimator           [Stage 6]
├── SwapHandler            [Stage 6] ← теперь с _matchFinder
├── MatchFinder            [Stage 7] ← NEW
└── MatchHighlighter       [Stage 7] ← NEW (debug)
```

---

## Известные ограничения

### 1. Cascade не реализован

На этом этапе после успешного свапа матчи находятся, но не уничтожаются. Это будет в Этапе 8 (Destroy System).

### 2. MatchHighlighter — только для debug

В production-билде можно отключить или удалить. Использует Gizmos которые не рисуются в билде.

### 3. Нет подсветки возможных ходов

Hint system (подсветка куда можно сходить) — отдельная фича, не входит в базовый Match Detection.

---

## Возможные улучшения (будущее)

1. **Match Patterns** — T-shape, L-shape дают бонусы
2. **4-match / 5-match** — создание special элементов
3. **Hint System** — подсветка возможных ходов
4. **Cascading Multiplier** — комбо за chain matches

---

## Чеклист

### Код (завершено)
- [x] Создать папку `Assets/Scripts/Match/`
- [x] `Match.cs` создан (38 строк)
- [x] `MatchFinder.cs` создан (201 строка)
- [x] `MatchHighlighter.cs` создан (68 строк)
- [x] `MatchSystemSetup.cs` создан (56 строк)
- [x] SwapHandler интегрирован с MatchFinder

### Тестирование в Unity
- [ ] Меню `Match3 → Setup Scene → Stage 7 - Match System` работает
- [ ] MatchFinder находит горизонтальные матчи (3+ в ряд)
- [ ] MatchFinder находит вертикальные матчи (3+ в столбец)
- [ ] L/T матчи объединяются в Cross
- [ ] Невалидный свап реверсится (возврат на место)
- [ ] Валидный свап не реверсится (остаётся)
- [ ] MatchHighlighter показывает Gizmos (ПКМ → Find And Highlight)

---

## FAQ

### Q: Почему не использовать flood fill?

A: Line scan проще и эффективнее для Match-3. Flood fill нужен для игр типа "Puzzle Bobble" где важна связность, а не линии.

### Q: Почему матчи объединяются?

A: Для корректного подсчёта в будущем (5-match = bomb, L-shape = lightning). Также визуально правильнее уничтожать как единое целое.

### Q: Можно ли сделать 4-match/5-match?

A: Да, `_minMatchLength` настраивается. Для special элементов нужно проверять `match.Count >= 4` и т.д. в Этапе 8.
