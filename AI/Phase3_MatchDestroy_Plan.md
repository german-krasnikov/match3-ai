# Phase 3: Match & Destroy — План реализации

## Обзор

Система поиска совпадений и уничтожения тайлов.

**Зависимости:** Phase 1 (Board), Phase 2 (Input & Swap)
**Технологии:** DOTween для анимаций уничтожения

---

## Архитектура компонентов

```
┌─────────────────────────────────────────────────────────────┐
│                      BoardController                         │
│              (расширяется обработкой матчей)                │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ события
┌─────────────────┐    ┌──────┴──────┐    ┌─────────────────┐
│  SwapController │───▶│MatchDetector │───▶│ DestroyHandler  │
│   (из Phase 2)  │    │ (поиск)      │    │  (уничтожение)  │
└─────────────────┘    └──────────────┘    └─────────────────┘
                              │                    │
                              ▼                    ▼
                       ┌──────────────┐    ┌──────────────┐
                       │  MatchData   │    │DestroyAnimator│
                       │  (данные)    │    │  (анимация)   │
                       └──────────────┘    └──────────────┘
```

---

## 1. Data Structures

### 1.1 MatchType Enum

**Файл:** `Scripts/Data/MatchType.cs`

```csharp
public enum MatchType
{
    None = 0,
    Line3 = 1,          // Обычная линия 3
    Line4 = 2,          // Линия 4 → Striped
    Line5 = 3,          // Линия 5 → ColorBomb
    LShape = 4,         // L-образное → Wrapped
    TShape = 5,         // T-образное → Wrapped
    Cross = 6,          // Крест → Wrapped
    Square = 7          // Квадрат 2x2 (опционально)
}
```

### 1.2 MatchData

**Файл:** `Scripts/Data/MatchData.cs`

```csharp
public class MatchData
{
    public List<Vector2Int> Positions { get; } = new();
    public MatchType Type { get; set; } = MatchType.None;
    public TileType TileType { get; set; }
    public Vector2Int Center { get; set; }  // Для спавна спецтайла

    public int Count => Positions.Count;

    public void AddPosition(Vector2Int pos)
    {
        if (!Positions.Contains(pos))
            Positions.Add(pos);
    }

    public void Merge(MatchData other)
    {
        foreach (var pos in other.Positions)
            AddPosition(pos);
    }

    public bool Intersects(MatchData other)
    {
        foreach (var pos in Positions)
        {
            if (other.Positions.Contains(pos))
                return true;
        }
        return false;
    }
}
```

### 1.3 MatchResult

**Файл:** `Scripts/Data/MatchResult.cs`

```csharp
public class MatchResult
{
    public List<MatchData> Matches { get; } = new();
    public HashSet<Vector2Int> AllPositions { get; } = new();
    public int TotalScore { get; set; }

    public bool HasMatches => Matches.Count > 0;

    public void AddMatch(MatchData match)
    {
        Matches.Add(match);
        foreach (var pos in match.Positions)
            AllPositions.Add(pos);
    }
}
```

---

## 2. MatchDetector

**Файл:** `Scripts/Core/MatchDetector.cs`
**Ответственность:** Поиск всех совпадений на поле.

### 2.1 Основная структура

```csharp
public class MatchDetector : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<MatchResult> OnMatchesFound;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private int _minMatchLength = 3;
    [SerializeField] private bool _detectLShapes = true;
    [SerializeField] private bool _detectTShapes = true;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

    /// <summary>
    /// Поиск матчей во всём поле
    /// </summary>
    public MatchResult FindAllMatches()
    {
        var result = new MatchResult();

        // 1. Найти горизонтальные линии
        var horizontal = FindHorizontalMatches();

        // 2. Найти вертикальные линии
        var vertical = FindVerticalMatches();

        // 3. Объединить пересекающиеся матчи
        var merged = MergeMatches(horizontal, vertical);

        // 4. Определить тип каждого матча
        foreach (var match in merged)
        {
            match.Type = DetermineMatchType(match);
            match.Center = CalculateCenter(match);
            result.AddMatch(match);
        }

        if (result.HasMatches)
            OnMatchesFound?.Invoke(result);

        return result;
    }

    /// <summary>
    /// Поиск матчей только в указанных позициях (после свапа)
    /// </summary>
    public MatchResult FindMatchesAt(params Vector2Int[] positions)
    {
        var result = new MatchResult();
        var processed = new HashSet<Vector2Int>();

        foreach (var pos in positions)
        {
            if (processed.Contains(pos)) continue;

            // Проверяем горизонталь и вертикаль от этой точки
            var hMatch = FindLineFromPoint(pos, Vector2Int.right);
            var vMatch = FindLineFromPoint(pos, Vector2Int.up);

            if (hMatch != null)
            {
                foreach (var p in hMatch.Positions) processed.Add(p);
            }
            if (vMatch != null)
            {
                foreach (var p in vMatch.Positions) processed.Add(p);
            }

            // Объединяем если пересекаются
            var merged = MergeIfIntersect(hMatch, vMatch);
            foreach (var m in merged)
            {
                m.Type = DetermineMatchType(m);
                m.Center = CalculateCenter(m);
                result.AddMatch(m);
            }
        }

        if (result.HasMatches)
            OnMatchesFound?.Invoke(result);

        return result;
    }

    /// <summary>
    /// Проверка: создаст ли обмен матч (для SwapValidator)
    /// </summary>
    public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
    {
        var tileA = GetTileType(posA);
        var tileB = GetTileType(posB);

        // Проверяем как будто тайлы уже поменялись
        return CheckMatchAtPosition(posA, tileB) ||
               CheckMatchAtPosition(posB, tileA);
    }
}
```

### 2.2 Алгоритмы поиска

```csharp
// === ПРИВАТНЫЕ МЕТОДЫ ===

private List<MatchData> FindHorizontalMatches()
{
    var matches = new List<MatchData>();

    for (int y = 0; y < _grid.Height; y++)
    {
        int x = 0;
        while (x < _grid.Width)
        {
            var startType = GetTileType(x, y);
            if (startType == TileType.None)
            {
                x++;
                continue;
            }

            // Считаем длину линии
            int length = 1;
            while (x + length < _grid.Width &&
                   GetTileType(x + length, y) == startType)
            {
                length++;
            }

            if (length >= _minMatchLength)
            {
                var match = new MatchData { TileType = startType };
                for (int i = 0; i < length; i++)
                    match.AddPosition(new Vector2Int(x + i, y));
                matches.Add(match);
            }

            x += length;
        }
    }

    return matches;
}

private List<MatchData> FindVerticalMatches()
{
    var matches = new List<MatchData>();

    for (int x = 0; x < _grid.Width; x++)
    {
        int y = 0;
        while (y < _grid.Height)
        {
            var startType = GetTileType(x, y);
            if (startType == TileType.None)
            {
                y++;
                continue;
            }

            int length = 1;
            while (y + length < _grid.Height &&
                   GetTileType(x, y + length) == startType)
            {
                length++;
            }

            if (length >= _minMatchLength)
            {
                var match = new MatchData { TileType = startType };
                for (int i = 0; i < length; i++)
                    match.AddPosition(new Vector2Int(x, y + i));
                matches.Add(match);
            }

            y += length;
        }
    }

    return matches;
}

private MatchData FindLineFromPoint(Vector2Int start, Vector2Int direction)
{
    var type = GetTileType(start);
    if (type == TileType.None) return null;

    var positions = new List<Vector2Int> { start };

    // В обе стороны от точки
    var pos = start + direction;
    while (_grid.IsValidPosition(pos) && GetTileType(pos) == type)
    {
        positions.Add(pos);
        pos += direction;
    }

    pos = start - direction;
    while (_grid.IsValidPosition(pos) && GetTileType(pos) == type)
    {
        positions.Add(pos);
        pos -= direction;
    }

    if (positions.Count < _minMatchLength)
        return null;

    var match = new MatchData { TileType = type };
    foreach (var p in positions)
        match.AddPosition(p);

    return match;
}

private List<MatchData> MergeMatches(List<MatchData> horizontal, List<MatchData> vertical)
{
    var all = new List<MatchData>();
    all.AddRange(horizontal);
    all.AddRange(vertical);

    // Объединяем пересекающиеся матчи одного типа
    var merged = new List<MatchData>();
    var used = new bool[all.Count];

    for (int i = 0; i < all.Count; i++)
    {
        if (used[i]) continue;

        var current = all[i];
        used[i] = true;

        // Ищем все пересекающиеся
        bool foundMerge;
        do
        {
            foundMerge = false;
            for (int j = 0; j < all.Count; j++)
            {
                if (used[j]) continue;
                if (all[j].TileType != current.TileType) continue;

                if (current.Intersects(all[j]))
                {
                    current.Merge(all[j]);
                    used[j] = true;
                    foundMerge = true;
                }
            }
        } while (foundMerge);

        merged.Add(current);
    }

    return merged;
}

private List<MatchData> MergeIfIntersect(MatchData a, MatchData b)
{
    var result = new List<MatchData>();

    if (a == null && b == null) return result;
    if (a == null) { result.Add(b); return result; }
    if (b == null) { result.Add(a); return result; }

    if (a.TileType == b.TileType && a.Intersects(b))
    {
        a.Merge(b);
        result.Add(a);
    }
    else
    {
        result.Add(a);
        result.Add(b);
    }

    return result;
}
```

### 2.3 Определение типа матча

```csharp
private MatchType DetermineMatchType(MatchData match)
{
    int count = match.Count;

    // Простые линии
    if (IsSimpleLine(match))
    {
        return count switch
        {
            3 => MatchType.Line3,
            4 => MatchType.Line4,
            >= 5 => MatchType.Line5,
            _ => MatchType.None
        };
    }

    // Сложные формы
    if (_detectLShapes && IsLShape(match)) return MatchType.LShape;
    if (_detectTShapes && IsTShape(match)) return MatchType.TShape;
    if (IsCross(match)) return MatchType.Cross;

    // Fallback по количеству
    return count >= 5 ? MatchType.Line5 :
           count >= 4 ? MatchType.Line4 :
           MatchType.Line3;
}

private bool IsSimpleLine(MatchData match)
{
    if (match.Count < 3) return false;

    var positions = match.Positions.OrderBy(p => p.x).ThenBy(p => p.y).ToList();

    // Все в одном ряду?
    if (positions.All(p => p.y == positions[0].y))
        return true;

    // Все в одной колонке?
    if (positions.All(p => p.x == positions[0].x))
        return true;

    return false;
}

private bool IsLShape(MatchData match)
{
    if (match.Count < 5) return false;

    // L = горизонталь + вертикаль с одной общей точкой (угол)
    var positions = match.Positions;

    // Находим угловую точку
    foreach (var pos in positions)
    {
        int horizontal = positions.Count(p => p.y == pos.y);
        int vertical = positions.Count(p => p.x == pos.x);

        // Угол: минимум 3 в горизонтали И минимум 3 в вертикали
        if (horizontal >= 3 && vertical >= 3)
            return true;
    }

    return false;
}

private bool IsTShape(MatchData match)
{
    if (match.Count < 5) return false;

    var positions = match.Positions;

    // T = центральная точка с 3+ соседями в одном направлении и 2+ в другом
    foreach (var pos in positions)
    {
        int horizontal = positions.Count(p => p.y == pos.y);
        int vertical = positions.Count(p => p.x == pos.x);

        // Центр T: ровно в середине одной линии и конец другой
        if (horizontal >= 3 && vertical >= 2)
        {
            // Проверяем что pos в центре горизонтали
            var hLine = positions.Where(p => p.y == pos.y).OrderBy(p => p.x).ToList();
            int posIndex = hLine.IndexOf(pos);
            if (posIndex > 0 && posIndex < hLine.Count - 1)
                return true;
        }

        if (vertical >= 3 && horizontal >= 2)
        {
            var vLine = positions.Where(p => p.x == pos.x).OrderBy(p => p.y).ToList();
            int posIndex = vLine.IndexOf(pos);
            if (posIndex > 0 && posIndex < vLine.Count - 1)
                return true;
        }
    }

    return false;
}

private bool IsCross(MatchData match)
{
    if (match.Count < 5) return false;

    var positions = match.Positions;

    // Крест = центральная точка с соседями во все 4 стороны
    foreach (var pos in positions)
    {
        bool hasUp = positions.Contains(pos + Vector2Int.up);
        bool hasDown = positions.Contains(pos + Vector2Int.down);
        bool hasLeft = positions.Contains(pos + Vector2Int.left);
        bool hasRight = positions.Contains(pos + Vector2Int.right);

        if (hasUp && hasDown && hasLeft && hasRight)
            return true;
    }

    return false;
}

private Vector2Int CalculateCenter(MatchData match)
{
    // Для L/T/Cross — точка пересечения
    // Для линий — средний элемент
    var positions = match.Positions;

    // Ищем точку с максимальным количеством соседей
    Vector2Int best = positions[0];
    int maxNeighbors = 0;

    foreach (var pos in positions)
    {
        int neighbors = 0;
        if (positions.Contains(pos + Vector2Int.up)) neighbors++;
        if (positions.Contains(pos + Vector2Int.down)) neighbors++;
        if (positions.Contains(pos + Vector2Int.left)) neighbors++;
        if (positions.Contains(pos + Vector2Int.right)) neighbors++;

        if (neighbors > maxNeighbors)
        {
            maxNeighbors = neighbors;
            best = pos;
        }
    }

    return best;
}

private TileType GetTileType(Vector2Int pos) => GetTileType(pos.x, pos.y);

private TileType GetTileType(int x, int y)
{
    var cell = _grid.GetCell(x, y);
    return cell?.CurrentTile?.Type ?? TileType.None;
}

private bool CheckMatchAtPosition(Vector2Int pos, TileType type)
{
    if (type == TileType.None) return false;

    // Горизонталь
    int hCount = 1;
    var p = pos + Vector2Int.left;
    while (_grid.IsValidPosition(p) && GetTileTypeOrSwapped(p, pos, type) == type)
    {
        hCount++;
        p += Vector2Int.left;
    }
    p = pos + Vector2Int.right;
    while (_grid.IsValidPosition(p) && GetTileTypeOrSwapped(p, pos, type) == type)
    {
        hCount++;
        p += Vector2Int.right;
    }

    if (hCount >= _minMatchLength) return true;

    // Вертикаль
    int vCount = 1;
    p = pos + Vector2Int.down;
    while (_grid.IsValidPosition(p) && GetTileTypeOrSwapped(p, pos, type) == type)
    {
        vCount++;
        p += Vector2Int.down;
    }
    p = pos + Vector2Int.up;
    while (_grid.IsValidPosition(p) && GetTileTypeOrSwapped(p, pos, type) == type)
    {
        vCount++;
        p += Vector2Int.up;
    }

    return vCount >= _minMatchLength;
}

private TileType GetTileTypeOrSwapped(Vector2Int checkPos, Vector2Int swappedPos, TileType swappedType)
{
    if (checkPos == swappedPos) return swappedType;
    return GetTileType(checkPos);
}
```

---

## 3. DestroyAnimator

**Файл:** `Scripts/Components/Animation/DestroyAnimator.cs`
**Ответственность:** Анимация уничтожения тайлов.

```csharp
public class DestroyAnimator : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnDestroyAnimationComplete;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _destroyDuration = 0.2f;
    [SerializeField] private float _delayBetweenTiles = 0.03f;
    [SerializeField] private Ease _destroyEase = Ease.InBack;

    [Header("Effects")]
    [SerializeField] private bool _scaleDown = true;
    [SerializeField] private bool _fadeOut = true;
    [SerializeField] private float _punchScale = 1.2f;

    /// <summary>
    /// Анимация уничтожения группы тайлов
    /// </summary>
    public void AnimateDestroy(List<TileComponent> tiles, Action onComplete = null)
    {
        if (tiles == null || tiles.Count == 0)
        {
            onComplete?.Invoke();
            OnDestroyAnimationComplete?.Invoke();
            return;
        }

        var sequence = DOTween.Sequence();

        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            if (tile == null) continue;

            float delay = i * _delayBetweenTiles;

            // Punch эффект
            sequence.Insert(delay, tile.transform
                .DOPunchScale(Vector3.one * _punchScale * 0.2f, _destroyDuration * 0.3f));

            // Уменьшение
            if (_scaleDown)
            {
                sequence.Insert(delay + _destroyDuration * 0.3f, tile.transform
                    .DOScale(Vector3.zero, _destroyDuration * 0.7f)
                    .SetEase(_destroyEase));
            }

            // Затухание
            if (_fadeOut)
            {
                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sequence.Insert(delay, sr
                        .DOFade(0f, _destroyDuration)
                        .SetEase(Ease.Linear));
                }
            }
        }

        sequence.OnComplete(() =>
        {
            onComplete?.Invoke();
            OnDestroyAnimationComplete?.Invoke();
        });
    }

    /// <summary>
    /// Анимация уничтожения одного тайла
    /// </summary>
    public void AnimateDestroySingle(TileComponent tile, Action onComplete = null)
    {
        AnimateDestroy(new List<TileComponent> { tile }, onComplete);
    }
}
```

---

## 4. DestroyHandler

**Файл:** `Scripts/Core/DestroyHandler.cs`
**Ответственность:** Управление процессом уничтожения тайлов.

```csharp
public class DestroyHandler : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<MatchResult> OnDestroyStarted;
    public event Action<List<Vector2Int>> OnTilesDestroyed; // Освободившиеся позиции
    public event Action OnDestroyComplete;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private GridComponent _grid;
    [SerializeField] private DestroyAnimator _animator;
    [SerializeField] private ScoreCalculator _scoreCalculator; // опционально

    // === СОСТОЯНИЕ ===
    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;

    /// <summary>
    /// Уничтожить тайлы по результату матча
    /// </summary>
    public void DestroyMatches(MatchResult result)
    {
        if (_isProcessing || !result.HasMatches) return;

        _isProcessing = true;
        OnDestroyStarted?.Invoke(result);

        // Собираем все тайлы для уничтожения
        var tilesToDestroy = new List<TileComponent>();
        var positions = new List<Vector2Int>();

        foreach (var pos in result.AllPositions)
        {
            var cell = _grid.GetCell(pos);
            if (cell?.CurrentTile != null)
            {
                tilesToDestroy.Add(cell.CurrentTile);
                positions.Add(pos);
            }
        }

        // Помечаем тайлы
        foreach (var tile in tilesToDestroy)
        {
            tile.IsMatched = true;
        }

        // Анимация
        _animator.AnimateDestroy(tilesToDestroy, () =>
        {
            // Удаляем тайлы из ячеек
            foreach (var pos in positions)
            {
                var cell = _grid.GetCell(pos);
                if (cell != null)
                {
                    var tile = cell.RemoveTile();
                    if (tile != null)
                    {
                        // Возврат в пул или уничтожение
                        Destroy(tile.gameObject);
                    }
                }
            }

            _isProcessing = false;
            OnTilesDestroyed?.Invoke(positions);
            OnDestroyComplete?.Invoke();
        });
    }

    /// <summary>
    /// Уничтожить один тайл (для бустеров)
    /// </summary>
    public void DestroySingle(Vector2Int position)
    {
        var cell = _grid.GetCell(position);
        if (cell?.CurrentTile == null) return;

        var tile = cell.CurrentTile;
        tile.IsMatched = true;

        _animator.AnimateDestroySingle(tile, () =>
        {
            cell.RemoveTile();
            Destroy(tile.gameObject);
            OnTilesDestroyed?.Invoke(new List<Vector2Int> { position });
        });
    }
}
```

---

## 5. ScoreCalculator

**Файл:** `Scripts/Core/ScoreCalculator.cs`
**Ответственность:** Подсчёт очков за матчи.

```csharp
public class ScoreCalculator : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<int> OnScoreCalculated;

    // === НАСТРОЙКИ ===
    [Header("Base Points")]
    [SerializeField] private int _line3Points = 50;
    [SerializeField] private int _line4Points = 100;
    [SerializeField] private int _line5Points = 200;
    [SerializeField] private int _lShapePoints = 150;
    [SerializeField] private int _tShapePoints = 150;
    [SerializeField] private int _crossPoints = 200;

    [Header("Multipliers")]
    [SerializeField] private float _cascadeMultiplier = 1.5f;

    // === СОСТОЯНИЕ ===
    private int _cascadeLevel;

    public void ResetCascade() => _cascadeLevel = 0;
    public void IncrementCascade() => _cascadeLevel++;

    public int CalculateScore(MatchResult result)
    {
        int baseScore = 0;

        foreach (var match in result.Matches)
        {
            baseScore += GetBasePoints(match.Type);
        }

        // Множитель каскада
        float multiplier = Mathf.Pow(_cascadeMultiplier, _cascadeLevel);
        int finalScore = Mathf.RoundToInt(baseScore * multiplier);

        OnScoreCalculated?.Invoke(finalScore);
        return finalScore;
    }

    private int GetBasePoints(MatchType type)
    {
        return type switch
        {
            MatchType.Line3 => _line3Points,
            MatchType.Line4 => _line4Points,
            MatchType.Line5 => _line5Points,
            MatchType.LShape => _lShapePoints,
            MatchType.TShape => _tShapePoints,
            MatchType.Cross => _crossPoints,
            _ => _line3Points
        };
    }
}
```

---

## 6. MatchController (Оркестратор)

**Файл:** `Scripts/Core/MatchController.cs`
**Ответственность:** Координация поиска и уничтожения матчей.

```csharp
public class MatchController : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnMatchProcessingStarted;
    public event Action OnMatchProcessingComplete;
    public event Action<int> OnScoreAdded;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private MatchDetector _detector;
    [SerializeField] private DestroyHandler _destroyHandler;
    [SerializeField] private ScoreCalculator _scoreCalculator;

    // === СОСТОЯНИЕ ===
    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;

    private void OnEnable()
    {
        _destroyHandler.OnDestroyComplete += OnDestroyComplete;
    }

    private void OnDisable()
    {
        _destroyHandler.OnDestroyComplete -= OnDestroyComplete;
    }

    /// <summary>
    /// Проверить и обработать матчи после свапа
    /// </summary>
    public void ProcessMatchesAt(Vector2Int posA, Vector2Int posB)
    {
        if (_isProcessing) return;

        var result = _detector.FindMatchesAt(posA, posB);

        if (result.HasMatches)
        {
            ProcessMatches(result);
        }
    }

    /// <summary>
    /// Проверить всё поле на матчи (после падения)
    /// </summary>
    public void ProcessAllMatches()
    {
        if (_isProcessing) return;

        var result = _detector.FindAllMatches();

        if (result.HasMatches)
        {
            ProcessMatches(result);
        }
        else
        {
            // Каскад закончен
            _scoreCalculator.ResetCascade();
            OnMatchProcessingComplete?.Invoke();
        }
    }

    /// <summary>
    /// Проверка для SwapValidator
    /// </summary>
    public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
    {
        return _detector.WouldCreateMatch(posA, posB);
    }

    private void ProcessMatches(MatchResult result)
    {
        _isProcessing = true;
        OnMatchProcessingStarted?.Invoke();

        // Подсчёт очков
        int score = _scoreCalculator.CalculateScore(result);
        OnScoreAdded?.Invoke(score);

        // Уничтожение
        _destroyHandler.DestroyMatches(result);
    }

    private void OnDestroyComplete()
    {
        _isProcessing = false;
        _scoreCalculator.IncrementCascade();

        // Сигнал для GravityController (Phase 4)
        // Пока просто завершаем
        OnMatchProcessingComplete?.Invoke();
    }
}
```

---

## 7. Интеграция с Phase 2

### 7.1 Обновить SwapValidator

```csharp
// В SwapValidator добавить:
[SerializeField] private MatchController _matchController;

public bool WillCreateMatch(Vector2Int posA, Vector2Int posB)
{
    return _matchController.WouldCreateMatch(posA, posB);
}
```

### 7.2 Обновить BoardInputHandler

```csharp
// Добавить в BoardInputHandler:
[SerializeField] private MatchController _matchController;

private void OnEnable()
{
    // ... существующие подписки ...
    _matchController.OnMatchProcessingComplete += OnMatchProcessingComplete;
}

private void OnSwapCompleted(Vector2Int posA, Vector2Int posB)
{
    _matchController.ProcessMatchesAt(posA, posB);
}

private void OnMatchProcessingComplete()
{
    // Здесь будет вызов Gravity (Phase 4)
    Debug.Log("Match processing complete, ready for gravity");
}
```

---

## 8. Структура файлов

```
Assets/Scripts/
├── Data/
│   ├── MatchType.cs           ← NEW
│   ├── MatchData.cs           ← NEW
│   └── MatchResult.cs         ← NEW
├── Core/
│   ├── MatchDetector.cs       ← NEW
│   ├── MatchController.cs     ← NEW
│   ├── DestroyHandler.cs      ← NEW
│   ├── ScoreCalculator.cs     ← NEW
│   ├── SwapValidator.cs       ← MODIFY (добавить _matchController)
│   └── BoardInputHandler.cs   ← MODIFY (добавить обработку матчей)
└── Components/
    └── Animation/
        └── DestroyAnimator.cs ← NEW
```

---

## 9. Порядок реализации

### Step 1: Data Structures (15 мин)
1. `MatchType.cs`
2. `MatchData.cs`
3. `MatchResult.cs`

### Step 2: MatchDetector — базовые линии (45 мин)
1. `MatchDetector.cs` — только `FindHorizontalMatches`, `FindVerticalMatches`
2. Тест: поиск простых линий 3+

### Step 3: MatchDetector — объединение и формы (30 мин)
1. `MergeMatches`
2. `DetermineMatchType` — Line3/4/5
3. Тест: поиск линий 4 и 5

### Step 4: MatchDetector — сложные формы (30 мин)
1. `IsLShape`, `IsTShape`, `IsCross`
2. `CalculateCenter`
3. Тест: L/T формы

### Step 5: DestroyAnimator (20 мин)
1. `DestroyAnimator.cs`
2. Тест: анимация уничтожения вручную

### Step 6: DestroyHandler (20 мин)
1. `DestroyHandler.cs`
2. Тест: уничтожение группы тайлов

### Step 7: ScoreCalculator (15 мин)
1. `ScoreCalculator.cs`
2. Тест: подсчёт очков

### Step 8: MatchController (20 мин)
1. `MatchController.cs`
2. Связать все компоненты

### Step 9: Интеграция с Phase 2 (20 мин)
1. Обновить `SwapValidator`
2. Обновить `BoardInputHandler`
3. Полный тест flow: свап → матч → уничтожение

### Step 10: Проверка WouldCreateMatch (15 мин)
1. Реализовать `WouldCreateMatch` в `MatchDetector`
2. Тест: откат свапа без матча работает корректно

---

## 10. Тестовый чеклист

```
□ Горизонтальные линии 3+ находятся
□ Вертикальные линии 3+ находятся
□ Пересекающиеся матчи объединяются
□ Line3/Line4/Line5 определяются корректно
□ L-shape определяется (5+ тайлов, угол)
□ T-shape определяется (5+ тайлов, центр)
□ Cross определяется (5+ тайлов, крест)
□ Center вычисляется правильно
□ WouldCreateMatch работает для SwapValidator
□ Анимация уничтожения проигрывается
□ Тайлы удаляются из Grid после анимации
□ Очки считаются правильно
□ Каскадный множитель работает
□ Полный flow: свап → матч → уничтожение
□ Откат свапа при отсутствии матча
```

---

## 11. Связь с Phase 4 (Cascade/Gravity)

После Phase 3:

1. **DestroyHandler.OnTilesDestroyed** → вызов `GravityController.ApplyGravity(positions)`
2. **GravityController.OnGravityComplete** → вызов `TileSpawner.FillEmptyCells()`
3. **TileSpawner.OnFillComplete** → вызов `MatchController.ProcessAllMatches()` (каскад)
4. Цикл повторяется пока есть матчи

```csharp
// Будущий flow в BoardController/GameManager:
_destroyHandler.OnTilesDestroyed += (positions) =>
{
    _gravityController.ApplyGravity(positions);
};

_gravityController.OnGravityComplete += () =>
{
    _spawner.FillEmptyCells();
};

_spawner.OnFillComplete += () =>
{
    _matchController.ProcessAllMatches(); // Каскад
};
```
