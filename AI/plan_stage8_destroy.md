# Этап 8: Destroy System - Детальный План Реализации

## Статус: В ОЖИДАНИИ ⏳

---

## Обзор

Destroy System отвечает за уничтожение элементов после нахождения матчей. Получает `List<Match>` от MatchFinder, извлекает уникальные позиции, удаляет элементы из BoardComponent, анимирует исчезновение и возвращает в пул.

### Связь с другими системами

```
SwapHandler.OnSwapCompleted
         │
         ▼
┌─────────────────────────────────────┐
│  [Этап 11: GameLoopController]      │ ← Orchestrator (будущее)
│                                     │
│  OnSwapCompleted                    │
│         │                           │
│         ▼                           │
│  MatchFinder.FindAllMatches()       │
│         │                           │
│         ▼                           │
│  DestroyHandler.DestroyMatches()  ◄─┼── ЭТАП 8
│         │                           │
│         ▼                           │
│  [Этап 9: FallHandler]              │
│         │                           │
│         ▼                           │
│  [Этап 10: RefillHandler]           │
└─────────────────────────────────────┘
```

### Зависимости

| Зависимость | Использование |
|-------------|---------------|
| `BoardComponent` | `RemoveElement(pos)` — удаление из сетки |
| `GridComponent` | `GridToWorld(pos)` — позиции для VFX |
| `ElementFactory` | `Return(element)` — возврат в пул |
| `Match` struct | Получение позиций для уничтожения |

---

## Архитектура

### Компоненты

| Компонент | Ответственность | События |
|-----------|-----------------|---------|
| `DestroyHandler` | Логика удаления элементов | `OnDestroyStarted`, `OnDestroyCompleted` |
| `DestroyAnimator` | DOTween анимации исчезновения | — |

### Принцип разделения (Unity Way)

```
DestroyHandler              DestroyAnimator
(логика, данные)            (визуал)
      │                          │
      │  1. Collect positions    │
      │  2. Get elements         │
      ├─────────────────────────►│ 3. Animate (scale, fade)
      │                          │
      │◄─────────────────────────┤ 4. OnComplete callback
      │                          │
      │  5. Remove from board    │
      │  6. Return to pool       │
      │  7. Fire event           │
      ▼                          ▼
```

---

## Файлы для создания

```
Assets/Scripts/Destroy/
├── DestroyHandler.cs      # Логика удаления
└── DestroyAnimator.cs     # Анимации

Assets/Scripts/Editor/
└── DestroySystemSetup.cs  # Editor setup
```

---

## 8.1 DestroyHandler.cs

### Назначение

Координирует процесс уничтожения: собирает уникальные позиции из матчей, запускает анимации, затем удаляет из Board и возвращает в пул.

### API

```csharp
public class DestroyHandler : MonoBehaviour
{
    // События
    public event Action OnDestroyStarted;
    public event Action<int> OnDestroyCompleted;  // int = кол-во уничтоженных

    // Публичные методы
    public void DestroyMatches(List<Match> matches);
    public bool IsDestroying { get; }
}
```

### Поток данных

```
DestroyMatches(List<Match> matches)
         │
         ▼
   CollectUniquePositions()  ← HashSet для дедупликации
         │
         ▼
   GetElementsFromBoard()    ← List<ElementComponent>
         │
         ▼
   OnDestroyStarted?.Invoke()
         │
         ▼
   _animator.AnimateDestroy(elements, callback)
         │                              │
         │  ←───── OnComplete ──────────┤
         ▼
   RemoveFromBoard() + ReturnToPool()
         │
         ▼
   OnDestroyCompleted?.Invoke(count)
```

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Elements;
using Match3.Matching;
using Match3.Spawn;

namespace Match3.Destroy
{
    public class DestroyHandler : MonoBehaviour
    {
        public event Action OnDestroyStarted;
        public event Action<int> OnDestroyCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactory _factory;
        [SerializeField] private DestroyAnimator _animator;

        private readonly HashSet<Vector2Int> _positionsBuffer = new();
        private readonly List<ElementComponent> _elementsBuffer = new();

        public bool IsDestroying { get; private set; }

        public void DestroyMatches(List<Match> matches)
        {
            if (matches == null || matches.Count == 0) return;
            if (IsDestroying) return;

            IsDestroying = true;

            CollectUniquePositions(matches);
            CollectElements();

            if (_elementsBuffer.Count == 0)
            {
                FinishDestroy(0);
                return;
            }

            OnDestroyStarted?.Invoke();

            int count = _elementsBuffer.Count;
            _animator.AnimateDestroy(_elementsBuffer, () => OnAnimationComplete(count));
        }

        private void CollectUniquePositions(List<Match> matches)
        {
            _positionsBuffer.Clear();
            foreach (var match in matches)
                foreach (var pos in match.Positions)
                    _positionsBuffer.Add(pos);
        }

        private void CollectElements()
        {
            _elementsBuffer.Clear();
            foreach (var pos in _positionsBuffer)
            {
                var element = _board.GetElement(pos);
                if (element != null)
                    _elementsBuffer.Add(element);
            }
        }

        private void OnAnimationComplete(int count)
        {
            foreach (var pos in _positionsBuffer)
            {
                var element = _board.RemoveElement(pos);
                if (element != null)
                    _factory.Return(element);
            }

            FinishDestroy(count);
        }

        private void FinishDestroy(int count)
        {
            _positionsBuffer.Clear();
            _elementsBuffer.Clear();
            IsDestroying = false;
            OnDestroyCompleted?.Invoke(count);
        }
    }
}
```

### Ключевые решения

| Решение | Обоснование |
|---------|-------------|
| HashSet для позиций | Дедупликация при Cross-матчах |
| Буферизация элементов | Сначала анимация, потом удаление |
| IsDestroying flag | Защита от повторного вызова |
| Callback анимации | Гарантия завершения перед удалением |

---

## 8.2 DestroyAnimator.cs

### Назначение

Анимирует исчезновение элементов с помощью DOTween. Bouncy/Casual стиль — элементы "лопаются" с overshoot.

### Анимация

```
Начало:        Середина:        Конец:
Scale = 1      Scale = 1.2      Scale = 0
Alpha = 1      Alpha = 1        Alpha = 0
               (punch)          (shrink + fade)
```

### Эффекты

1. **Punch Scale** — небольшое увеличение перед исчезновением
2. **Scale to Zero** — сжатие
3. **Fade Out** — плавное исчезновение спрайта
4. **Stagger** — небольшая задержка между элементами (каскадный эффект)

### Код

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Destroy
{
    public class DestroyAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _punchDuration = 0.1f;
        [SerializeField] private float _shrinkDuration = 0.2f;
        [SerializeField] private float _staggerDelay = 0.02f;

        [Header("Effects")]
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private Ease _shrinkEase = Ease.InBack;
        [SerializeField] private float _shrinkOvershoot = 2f;

        private Sequence _currentSequence;

        public void AnimateDestroy(List<ElementComponent> elements, Action onComplete)
        {
            KillCurrentAnimation();

            if (elements == null || elements.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;

                float delay = i * _staggerDelay;

                var elementSequence = CreateElementSequence(element);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementSequence(ElementComponent element)
        {
            var transform = element.transform;
            var spriteRenderer = element.SpriteRenderer;
            var originalScale = transform.localScale;

            var seq = DOTween.Sequence();

            // Phase 1: Punch (expand slightly)
            seq.Append(transform.DOScale(originalScale * _punchScale, _punchDuration)
                .SetEase(Ease.OutQuad));

            // Phase 2: Shrink to zero + fade
            seq.Append(transform.DOScale(Vector3.zero, _shrinkDuration)
                .SetEase(_shrinkEase, _shrinkOvershoot));

            seq.Join(spriteRenderer.DOFade(0f, _shrinkDuration)
                .SetEase(Ease.OutQuad));

            // Reset for pooling
            seq.OnComplete(() =>
            {
                transform.localScale = originalScale;
                spriteRenderer.color = new Color(
                    spriteRenderer.color.r,
                    spriteRenderer.color.g,
                    spriteRenderer.color.b,
                    1f
                );
            });

            return seq;
        }

        public void KillCurrentAnimation()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
```

### Параметры анимации (Inspector)

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `_punchDuration` | 0.1f | Длительность расширения |
| `_shrinkDuration` | 0.2f | Длительность сжатия |
| `_staggerDelay` | 0.02f | Задержка между элементами |
| `_punchScale` | 1.2f | Масштаб расширения |
| `_shrinkEase` | InBack | Easing сжатия |
| `_shrinkOvershoot` | 2f | Сила отскока |

### Timeline

```
Element 0:  [punch]─[shrink+fade]
Element 1:       [punch]─[shrink+fade]
Element 2:            [punch]─[shrink+fade]
...
            ├──0.02──┼──0.02──┼──0.02──►  (stagger)
            ├─0.1─┼────0.2────►           (per element)
```

Общая длительность ≈ `_punchDuration + _shrinkDuration + (count-1) * _staggerDelay`
Для 5 элементов: 0.1 + 0.2 + 0.08 = **0.38 секунды**

---

## 8.3 DestroySystemSetup.cs (Editor)

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Destroy;
using Match3.Grid;
using Match3.Board;
using Match3.Spawn;

namespace Match3.Editor
{
    public static class DestroySystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 8 - Destroy System")]
        public static void SetupDestroySystem()
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

            var factory = grid.GetComponent<ElementFactory>();
            if (factory == null)
            {
                Debug.LogError("[Match3] ElementFactory not found. Run Stage 3 setup first.");
                return;
            }

            var go = grid.gameObject;

            // DestroyAnimator
            var destroyAnimator = go.GetComponent<DestroyAnimator>();
            if (destroyAnimator == null)
                destroyAnimator = Undo.AddComponent<DestroyAnimator>(go);

            // DestroyHandler
            var destroyHandler = go.GetComponent<DestroyHandler>();
            if (destroyHandler == null)
                destroyHandler = Undo.AddComponent<DestroyHandler>(go);

            SetField(destroyHandler, "_board", board);
            SetField(destroyHandler, "_grid", grid);
            SetField(destroyHandler, "_factory", factory);
            SetField(destroyHandler, "_animator", destroyAnimator);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Destroy System setup complete!");
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

## 8.4 Particle Effects (опционально)

### Структура

VFX уничтожения можно добавить позже без изменения основных компонентов.

```
Assets/Prefabs/VFX/
└── DestroyParticle.prefab   # ParticleSystem

Assets/Scripts/Destroy/
└── DestroyVFX.cs            # Спаунер частиц
```

### Минимальная реализация

```csharp
using System.Collections.Generic;
using UnityEngine;
using Match3.Elements;
using Match3.Grid;

namespace Match3.Destroy
{
    public class DestroyVFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particlePrefab;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private DestroyHandler _destroyHandler;

        private void OnEnable()
        {
            _destroyHandler.OnDestroyStarted += OnDestroyStarted;
        }

        private void OnDisable()
        {
            _destroyHandler.OnDestroyStarted -= OnDestroyStarted;
        }

        private void OnDestroyStarted()
        {
            // Получаем позиции из DestroyHandler (нужен доступ к буферу)
            // Для MVP можно пропустить
        }

        public void SpawnParticle(Vector3 worldPosition, Color color)
        {
            var particle = Instantiate(_particlePrefab, worldPosition, Quaternion.identity);
            var main = particle.main;
            main.startColor = color;
            particle.Play();
            Destroy(particle.gameObject, main.duration + main.startLifetime.constantMax);
        }
    }
}
```

**Примечание:** VFX не входит в MVP. Добавить после полного игрового цикла.

---

## Диаграмма компонентов

После Stage 8 на GameManager объекте:

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
├── SwapHandler            [Stage 6]
├── MatchFinder            [Stage 7]
├── MatchHighlighter       [Stage 7] (debug)
├── DestroyAnimator        [Stage 8] ← NEW
└── DestroyHandler         [Stage 8] ← NEW
```

---

## Порядок реализации

| # | Файл | Зависимости | Тест |
|---|------|-------------|------|
| 1 | `DestroyAnimator.cs` | DOTween, ElementComponent | Визуально в Scene |
| 2 | `DestroyHandler.cs` | Board, Factory, Animator | Context menu тест |
| 3 | `DestroySystemSetup.cs` | Все выше | Меню создаёт компоненты |

---

## Тестирование

### Тест 1: Ручной вызов DestroyHandler

Добавить временный ContextMenu для тестирования:

```csharp
// Добавить в DestroyHandler.cs временно:
[ContextMenu("Test Destroy All Matches")]
private void TestDestroyAllMatches()
{
    var matchFinder = GetComponent<MatchFinder>();
    if (matchFinder == null) return;

    var matches = matchFinder.FindAllMatches();
    Debug.Log($"[DestroyHandler] Found {matches.Count} matches, destroying...");
    DestroyMatches(matches);
}
```

### Тест 2: Проверка анимации

1. Play Mode
2. Сделать свап, создающий матч
3. ПКМ на DestroyHandler → "Test Destroy All Matches"
4. Наблюдать: элементы увеличиваются, затем сжимаются и исчезают
5. Проверить что элементы вернулись в пул (ElementPool.PooledCount увеличился)

### Тест 3: Проверка состояния Board

```csharp
// Временно добавить в DestroyHandler:
private void OnAnimationComplete(int count)
{
    Debug.Log($"[DestroyHandler] Animation complete, removing {count} elements...");

    foreach (var pos in _positionsBuffer)
    {
        var element = _board.RemoveElement(pos);
        if (element != null)
        {
            _factory.Return(element);
            Debug.Log($"  Removed element at {pos}");
        }
    }

    FinishDestroy(count);

    // Проверка пустых ячеек
    var emptyCount = _board.GetEmptyPositions().Count;
    Debug.Log($"[DestroyHandler] Empty positions on board: {emptyCount}");
}
```

### Тест 4: Проверка дедупликации

1. Создать L-образный матч (Cross)
2. Вызвать DestroyMatches
3. Проверить что центральный элемент удалён только один раз
4. Нет ошибок "element already removed"

---

## Интеграция (будущее)

### С GameLoopController (Этап 11)

```csharp
// В GameLoopController:
private void OnSwapCompleted(Vector2Int posA, Vector2Int posB)
{
    ChangeState(GameState.Matching);

    var matches = _matchFinder.FindAllMatches();
    if (matches.Count > 0)
    {
        ChangeState(GameState.Destroying);
        _destroyHandler.DestroyMatches(matches);
    }
    else
    {
        ChangeState(GameState.Idle);
    }
}

private void OnEnable()
{
    _destroyHandler.OnDestroyCompleted += OnDestroyCompleted;
}

private void OnDestroyCompleted(int count)
{
    Debug.Log($"Destroyed {count} elements");
    ChangeState(GameState.Falling);  // → Этап 9
}
```

### С InputBlocker

```csharp
// В GameLoopController или отдельном InputCoordinator:
private void OnDestroyStarted()
{
    _inputBlocker.Block();
}

private void OnDestroyCompleted(int count)
{
    // НЕ разблокируем здесь — ждём Fall и Refill
}
```

---

## Визуализация процесса

```
Матч найден:              После DestroyMatches:

y=2: R R R G B            y=2: _ _ _ G B
     ↓ ↓ ↓
y=1: G Y B P R            y=1: G Y B P R
y=0: P B G Y R            y=0: P B G Y R
     0 1 2 3 4                 0 1 2 3 4

Анимация:                 После Return to Pool:
[punch → shrink → fade]   Elements в PooledElements container
                          Board._elements[0,2] = null
                          Board._elements[1,2] = null
                          Board._elements[2,2] = null
```

---

## Известные ограничения

### 1. Нет cascade

На этом этапе элементы уничтожаются, но пустые места не заполняются. Это Fall System (Этап 9).

### 2. Нет scoring

Подсчёт очков не входит в базовую механику. Можно добавить слушатель `OnDestroyCompleted(count)`.

### 3. Нет special элементов

4-match, 5-match не создают бонусные элементы. Это feature для будущих этапов.

---

## Возможные улучшения

| Улучшение | Сложность | Описание |
|-----------|-----------|----------|
| VFX частицы | Низкая | ParticleSystem при уничтожении |
| Screen shake | Низкая | Camera shake при большом матче |
| Combo звуки | Низкая | Разные звуки для 3/4/5 match |
| Scoring | Средняя | ScoreManager слушает OnDestroyCompleted |
| Special creation | Высокая | 4-match → Line bomb, 5-match → Color bomb |

---

## Чеклист

### Код
- [ ] Создать папку `Assets/Scripts/Destroy/`
- [ ] `DestroyAnimator.cs` — анимации DOTween
- [ ] `DestroyHandler.cs` — логика удаления
- [ ] `DestroySystemSetup.cs` — Editor menu

### Тестирование в Unity
- [ ] Меню `Match3 → Setup Scene → Stage 8 - Destroy System` работает
- [ ] Анимация punch → shrink → fade работает
- [ ] Элементы удаляются из BoardComponent
- [ ] Элементы возвращаются в ElementPool
- [ ] Stagger delay создаёт каскадный эффект
- [ ] Cross-матч: центральный элемент удаляется один раз
- [ ] Повторный вызов DestroyMatches игнорируется (IsDestroying)

---

## FAQ

### Q: Почему анимация перед удалением, а не после?

A: Пользователь должен видеть элементы пока они исчезают. Если удалить сразу — визуально будет "телепорт".

### Q: Почему HashSet для позиций?

A: Cross-матч содержит центральную позицию в обоих линиях (H и V). Без дедупликации элемент удалится дважды → ошибка.

### Q: Почему Reset scale/alpha в OnComplete?

A: Элементы переиспользуются через пул. Если не вернуть scale=1 и alpha=1, следующий спаун покажет невидимый элемент.

### Q: Можно ли не использовать DOTween?

A: Можно использовать корутины или AnimationCurve. DOTween выбран для консистентности с остальными анимациями (SwapAnimator уже его использует).
