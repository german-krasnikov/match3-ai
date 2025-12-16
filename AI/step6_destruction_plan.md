# Этап 6: Destruction — Детальный план реализации

## Обзор

Система уничтожения матчей. Получает `List<MatchData>`, анимирует и удаляет элементы, очищает ячейки в Grid.

---

## Файловая структура

```
Assets/Scripts/Destroy/
├── DestroyConfig.cs           # ScriptableObject - настройки анимации
├── DestroyAnimationComponent.cs  # Анимация уничтожения (DOTween)
└── DestroyComponent.cs        # Логика уничтожения матчей

Assets/Configs/
└── DestroyConfig.asset        # Инстанс конфигурации
```

---

## 6.1 DestroyConfig (ScriptableObject)

**Файл:** `Assets/Scripts/Destroy/DestroyConfig.cs`

```csharp
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "DestroyConfig", menuName = "Match3/DestroyConfig")]
public class DestroyConfig : ScriptableObject
{
    [Header("Animation")]
    [SerializeField, Range(0.05f, 0.5f)] private float _duration = 0.2f;
    [SerializeField] private Ease _scaleEase = Ease.InBack;
    [SerializeField] private Ease _fadeEase = Ease.Linear;

    [Header("Scale")]
    [SerializeField] private Vector3 _targetScale = Vector3.zero;

    [Header("Timing")]
    [SerializeField, Range(0f, 0.1f)] private float _staggerDelay = 0.02f;

    public float Duration => _duration;
    public Ease ScaleEase => _scaleEase;
    public Ease FadeEase => _fadeEase;
    public Vector3 TargetScale => _targetScale;
    public float StaggerDelay => _staggerDelay;
}
```

### Параметры:
| Параметр | Тип | Default | Описание |
|----------|-----|---------|----------|
| Duration | float | 0.2 | Длительность анимации |
| ScaleEase | Ease | InBack | Easing для scale |
| FadeEase | Ease | Linear | Easing для fade |
| TargetScale | Vector3 | (0,0,0) | Конечный scale |
| StaggerDelay | float | 0.02 | Задержка между элементами |

---

## 6.2 DestroyAnimationComponent (MonoBehaviour)

**Файл:** `Assets/Scripts/Destroy/DestroyAnimationComponent.cs`

### Зависимости (SerializeField):
- `DestroyConfig _config`

### Публичные методы:

```csharp
// Анимирует уничтожение одного элемента
public Tween AnimateDestroy(ElementComponent element)

// Анимирует группу элементов с callback по завершении всех
public void AnimateDestroyGroup(List<ElementComponent> elements, Action onComplete)
```

### Реализация:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DestroyAnimationComponent : MonoBehaviour
{
    [SerializeField] private DestroyConfig _config;

    private Sequence _currentSequence;

    public Tween AnimateDestroy(ElementComponent element)
    {
        var spriteRenderer = element.GetComponent<SpriteRenderer>();

        var seq = DOTween.Sequence();
        seq.Join(element.transform.DOScale(_config.TargetScale, _config.Duration)
            .SetEase(_config.ScaleEase));

        if (spriteRenderer != null)
        {
            seq.Join(spriteRenderer.DOFade(0f, _config.Duration)
                .SetEase(_config.FadeEase));
        }

        return seq;
    }

    public void AnimateDestroyGroup(List<ElementComponent> elements, Action onComplete)
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();

        if (elements.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        // Сортируем по расстоянию от центра (от центра к краям)
        var sorted = SortFromCenter(elements);

        for (int i = 0; i < sorted.Count; i++)
        {
            float delay = i * _config.StaggerDelay;
            _currentSequence.Insert(delay, AnimateDestroy(sorted[i]));
        }

        _currentSequence.OnComplete(() => onComplete?.Invoke());
    }

    private List<ElementComponent> SortFromCenter(List<ElementComponent> elements)
    {
        // Находим центр группы
        float centerX = 0f, centerY = 0f;
        foreach (var e in elements)
        {
            centerX += e.X;
            centerY += e.Y;
        }
        centerX /= elements.Count;
        centerY /= elements.Count;

        // Сортируем по расстоянию от центра
        var sorted = new List<ElementComponent>(elements);
        sorted.Sort((a, b) =>
        {
            float distA = (a.X - centerX) * (a.X - centerX) + (a.Y - centerY) * (a.Y - centerY);
            float distB = (b.X - centerX) * (b.X - centerX) + (b.Y - centerY) * (b.Y - centerY);
            return distA.CompareTo(distB);
        });

        return sorted;
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
```

### Ключевые моменты:
- **DOTween.Sequence** для группировки анимаций
- **Insert(delay, tween)** для stagger-эффекта
- **Kill() в OnDestroy** предотвращает утечки
- Scale + Fade параллельно через Join

---

## 6.3 DestroyComponent (MonoBehaviour)

**Файл:** `Assets/Scripts/Destroy/DestroyComponent.cs`

### Зависимости (SerializeField):
- `GridComponent _grid`
- `ElementFactory _elementFactory`
- `DestroyAnimationComponent _animation`

### События:
- `event Action OnDestructionStarted`
- `event Action OnDestructionComplete`

### Публичные методы:

```csharp
// Уничтожает все матчи
public void DestroyMatches(List<MatchData> matches)
```

### Реализация:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class DestroyComponent : MonoBehaviour
{
    public event Action OnDestructionStarted;
    public event Action OnDestructionComplete;

    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactory _elementFactory;
    [SerializeField] private DestroyAnimationComponent _animation;

    public void DestroyMatches(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0)
        {
            OnDestructionComplete?.Invoke();
            return;
        }

        OnDestructionStarted?.Invoke();

        // Собираем уникальные элементы
        var elementsToDestroy = CollectUniqueElements(matches);

        // Очищаем ячейки сразу (логика)
        ClearCells(matches);

        // Запускаем анимацию
        _animation.AnimateDestroyGroup(elementsToDestroy, () =>
        {
            // Уничтожаем GameObject'ы после анимации
            DestroyElements(elementsToDestroy);
            OnDestructionComplete?.Invoke();
        });
    }

    private List<ElementComponent> CollectUniqueElements(List<MatchData> matches)
    {
        var elements = new List<ElementComponent>();
        var processed = new HashSet<Cell>();

        foreach (var match in matches)
        {
            foreach (var cell in match.Cells)
            {
                if (processed.Contains(cell)) continue;
                if (cell.Element != null)
                {
                    elements.Add(cell.Element);
                    processed.Add(cell);
                }
            }
        }

        return elements;
    }

    private void ClearCells(List<MatchData> matches)
    {
        foreach (var match in matches)
        {
            foreach (var cell in match.Cells)
            {
                cell.Clear();
            }
        }
    }

    private void DestroyElements(List<ElementComponent> elements)
    {
        foreach (var element in elements)
        {
            _elementFactory.Destroy(element);
        }
    }
}
```

### Порядок операций:
1. **Собрать элементы** — HashSet предотвращает дубли (пересекающиеся матчи)
2. **Очистить ячейки** — логическое состояние Grid обновляется сразу
3. **Анимировать** — визуальное исчезновение
4. **Уничтожить GameObject'ы** — после завершения анимации

---

## Интеграция в сцену

### Шаг 1: Создать конфигурацию
```
Assets → Create → Match3 → DestroyConfig
```

### Шаг 2: Добавить компоненты на Board GameObject
```
Board (GameObject)
├── DestroyAnimationComponent
│   └── _config → DestroyConfig.asset
└── DestroyComponent
    ├── _grid → GridComponent
    ├── _elementFactory → ElementFactory
    └── _animation → DestroyAnimationComponent
```

### Шаг 3: Подписка на события (для будущего GameLoop)
```csharp
_destroyComponent.OnDestructionComplete += HandleDestructionComplete;
```

---

## Тестирование

### Ручное тестирование:
1. Добавить временный тестовый код в SwapComponent:
```csharp
// После успешного свапа
var matches = _matchDetector.FindMatches(_grid);
if (matches.Count > 0)
    _destroyComponent.DestroyMatches(matches);
```

### Что проверить:
- [ ] Элементы визуально исчезают (scale → 0, fade out)
- [ ] Ячейки Grid очищаются (cell.IsEmpty == true)
- [ ] L/T-образные матчи не создают дублей
- [ ] OnDestructionComplete вызывается после анимации
- [ ] Нет ошибок при пустом списке матчей

---

## Диаграмма потока

```
DestroyMatches(matches)
        │
        ▼
OnDestructionStarted
        │
        ▼
CollectUniqueElements ─── HashSet предотвращает дубли
        │
        ▼
ClearCells ─────────────── Grid.Cells[x,y].Element = null
        │
        ▼
AnimateDestroyGroup
        │
        ├── Scale → 0 ──┐
        │               ├── Параллельно
        └── Fade → 0 ───┘
                │
                ▼ (OnComplete)
        DestroyElements
                │
                ▼
    OnDestructionComplete
```

---

## Возможные улучшения (не в MVP)

1. **Particle эффекты** — спаун партиклов в позиции элемента
2. **Звуки** — AudioSource.PlayOneShot при уничтожении
3. **Scoring** — подсчёт очков на основе match.Length
4. **Combo визуал** — разные эффекты для 4+ матчей

---

## Checklist реализации

- [ ] Создать `DestroyConfig.cs`
- [ ] Создать `DestroyConfig.asset` в `Assets/Configs/`
- [ ] Создать `DestroyAnimationComponent.cs`
- [ ] Создать `DestroyComponent.cs`
- [ ] Добавить компоненты на Board в сцене
- [ ] Связать зависимости в Inspector
- [ ] Протестировать с тестовым кодом
- [ ] Убрать тестовый код после проверки
