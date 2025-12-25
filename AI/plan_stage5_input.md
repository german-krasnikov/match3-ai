# Этап 5: Input System - Детальный План Реализации ✅

## Статус: ЗАВЕРШЁН

## Обзор

Input System обрабатывает ввод игрока (мышь/тач) и преобразует его в команды для игровой логики. Система не выполняет свап напрямую - она только **запрашивает** его через событие.

### Важно: Namespace конфликт

Namespace `Match3.Input` конфликтует с `UnityEngine.Input`. Решение - alias:
```csharp
using UnityInput = UnityEngine.Input;
// затем использовать UnityInput.GetMouseButtonDown(0) и т.д.
```

### Связь с другими системами

```
InputDetector → OnSwapRequested → SwapHandler (Этап 6)
     ↓
GridComponent.WorldToGrid()
     ↓
BoardComponent.GetElement()
```

---

## Архитектура

### Компоненты

| Компонент | Ответственность |
|-----------|-----------------|
| `InputDetector` | Обнаружение клика/тача, определение свайпа, публикация событий |
| `SwipeDirection` | Enum для направлений (Up, Down, Left, Right) |
| `InputBlocker` | Блокировка ввода во время анимаций |

### Диаграмма взаимодействия

```
[Player Input]
      │
      ▼
┌─────────────────┐
│  InputBlocker   │──── IsBlocked? ────► Игнорировать ввод
└────────┬────────┘
         │ (если не заблокирован)
         ▼
┌─────────────────┐
│  InputDetector  │
│                 │
│ 1. Screen → World│
│ 2. World → Grid  │
│ 3. Validate pos  │
│ 4. Detect swipe  │
└────────┬────────┘
         │
         ▼
   OnSwapRequested(posA, posB)
         │
         ▼
   [SwapHandler - Этап 6]
```

---

## Файлы для создания

```
Assets/Scripts/Input/
├── SwipeDirection.cs      # Enum направлений
├── InputBlocker.cs        # Блокировка ввода
└── InputDetector.cs       # Главный компонент обработки ввода
```

---

## 5.1 SwipeDirection.cs

### Назначение
Enum для направлений свайпа + хелпер-методы.

### Код

```csharp
using UnityEngine;

namespace Match3.Input
{
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public static class SwipeDirectionExtensions
    {
        /// <summary>
        /// Конвертирует направление в смещение на сетке
        /// </summary>
        public static Vector2Int ToGridOffset(this SwipeDirection direction)
        {
            return direction switch
            {
                SwipeDirection.Up => Vector2Int.up,
                SwipeDirection.Down => Vector2Int.down,
                SwipeDirection.Left => Vector2Int.left,
                SwipeDirection.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };
        }

        /// <summary>
        /// Определяет направление свайпа по дельте
        /// </summary>
        public static SwipeDirection FromDelta(Vector2 delta, float threshold)
        {
            if (delta.magnitude < threshold)
                return SwipeDirection.None;

            // Определяем доминирующую ось
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }
    }
}
```

---

## 5.2 InputBlocker.cs

### Назначение
Простой компонент для блокировки ввода. GameLoop будет устанавливать `IsBlocked = true` во время анимаций.

### Код

```csharp
using UnityEngine;

namespace Match3.Input
{
    /// <summary>
    /// Управляет блокировкой ввода во время анимаций.
    /// Другие системы устанавливают IsBlocked.
    /// </summary>
    public class InputBlocker : MonoBehaviour
    {
        private int _blockCount;

        /// <summary>
        /// true если ввод заблокирован
        /// </summary>
        public bool IsBlocked => _blockCount > 0;

        /// <summary>
        /// Блокирует ввод. Можно вызывать несколько раз (стек).
        /// </summary>
        public void Block()
        {
            _blockCount++;
        }

        /// <summary>
        /// Разблокирует ввод. Вызывать столько же раз, сколько Block().
        /// </summary>
        public void Unblock()
        {
            _blockCount = Mathf.Max(0, _blockCount - 1);
        }

        /// <summary>
        /// Полностью сбрасывает блокировку
        /// </summary>
        public void Reset()
        {
            _blockCount = 0;
        }
    }
}
```

### Почему стек-подход?
Если несколько систем одновременно блокируют ввод (падение + уничтожение), каждая вызывает Block/Unblock независимо. Счётчик гарантирует, что ввод разблокируется только когда ВСЕ системы завершили работу.

---

## 5.3 InputDetector.cs

### Назначение
Главный компонент. Обрабатывает клик → свайп и публикует `OnSwapRequested`.

### Логика работы

```
Состояние: Idle
   │
   ├─► Клик по элементу → Сохранить позицию → Состояние: Selected
   │
Состояние: Selected
   │
   ├─► Драг → Определить направление → OnSwapRequested → Idle
   │
   ├─► Клик по СОСЕДНЕМУ элементу → OnSwapRequested → Idle
   │
   ├─► Клик по НЕсоседнему элементу → Выбрать новый → Selected
   │
   └─► Клик по тому же элементу → Отменить выбор → Idle
```

### Код

```csharp
using System;
using UnityEngine;
using Match3.Grid;
using Match3.Board;
using UnityInput = UnityEngine.Input; // ВАЖНО: alias для избежания конфликта

namespace Match3.Input
{
    /// <summary>
    /// Обрабатывает ввод игрока и определяет запросы на свап.
    /// </summary>
    public class InputDetector : MonoBehaviour
    {
        // === СОБЫТИЯ ===

        /// <summary>
        /// Вызывается когда игрок выбрал элемент
        /// </summary>
        public event Action<Vector2Int> OnElementSelected;

        /// <summary>
        /// Вызывается когда игрок отменил выбор
        /// </summary>
        public event Action OnSelectionCancelled;

        /// <summary>
        /// Вызывается когда игрок хочет поменять местами два элемента
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwapRequested;

        // === НАСТРОЙКИ ===

        [Header("Settings")]
        [SerializeField] private float _swipeThreshold = 0.5f;
        [Tooltip("Минимальное расстояние в world units для определения свайпа")]

        [SerializeField] private Camera _camera;

        // === ЗАВИСИМОСТИ ===

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private BoardComponent _board;
        [SerializeField] private InputBlocker _inputBlocker;

        // === ПРИВАТНЫЕ ПОЛЯ ===

        private Vector2Int? _selectedPosition;
        private Vector3 _pointerDownPosition;
        private bool _isDragging;

        // === ПУБЛИЧНЫЕ СВОЙСТВА ===

        public Vector2Int? SelectedPosition => _selectedPosition;
        public bool HasSelection => _selectedPosition.HasValue;

        // === UNITY CALLBACKS ===

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            if (_inputBlocker != null && _inputBlocker.IsBlocked)
                return;

            HandleInput();
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Сбрасывает текущий выбор
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedPosition.HasValue)
            {
                _selectedPosition = null;
                OnSelectionCancelled?.Invoke();
            }
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        private void HandleInput()
        {
            // Начало нажатия
            if (UnityInput.GetMouseButtonDown(0))
            {
                _pointerDownPosition = GetPointerWorldPosition();
                _isDragging = false;

                var gridPos = _grid.WorldToGrid(_pointerDownPosition);

                // Клик вне сетки - отменить выбор
                if (!_grid.IsValidPosition(gridPos))
                {
                    ClearSelection();
                    return;
                }

                // Клик по пустой ячейке - игнорировать
                if (_board.IsEmpty(gridPos))
                    return;

                HandlePointerDown(gridPos);
            }

            // Удерживание - проверка на свайп
            if (UnityInput.GetMouseButton(0) && _selectedPosition.HasValue && !_isDragging)
            {
                Vector3 currentPos = GetPointerWorldPosition();
                Vector2 delta = currentPos - _pointerDownPosition;

                var direction = SwipeDirectionExtensions.FromDelta(delta, _swipeThreshold);

                if (direction != SwipeDirection.None)
                {
                    _isDragging = true;
                    HandleSwipe(direction);
                }
            }
        }

        private void HandlePointerDown(Vector2Int gridPos)
        {
            // Если нет выбора - выбрать элемент
            if (!_selectedPosition.HasValue)
            {
                SelectElement(gridPos);
                return;
            }

            // Клик по тому же элементу - отменить выбор
            if (_selectedPosition.Value == gridPos)
            {
                ClearSelection();
                return;
            }

            // Клик по соседнему элементу - запросить свап
            if (IsAdjacent(_selectedPosition.Value, gridPos))
            {
                RequestSwap(_selectedPosition.Value, gridPos);
                return;
            }

            // Клик по несоседнему элементу - выбрать новый
            SelectElement(gridPos);
        }

        private void HandleSwipe(SwipeDirection direction)
        {
            if (!_selectedPosition.HasValue)
                return;

            Vector2Int targetPos = _selectedPosition.Value + direction.ToGridOffset();

            // Проверяем валидность целевой позиции
            if (!_grid.IsValidPosition(targetPos))
                return;

            // Проверяем что там есть элемент
            if (_board.IsEmpty(targetPos))
                return;

            RequestSwap(_selectedPosition.Value, targetPos);
        }

        private void SelectElement(Vector2Int gridPos)
        {
            _selectedPosition = gridPos;
            OnElementSelected?.Invoke(gridPos);
        }

        private void RequestSwap(Vector2Int from, Vector2Int to)
        {
            var fromPos = _selectedPosition.Value;
            ClearSelection();
            OnSwapRequested?.Invoke(fromPos, to);
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);

            // Соседи по горизонтали или вертикали (не диагонали!)
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private Vector3 GetPointerWorldPosition()
        {
            Vector3 mousePos = UnityInput.mousePosition;
            mousePos.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(mousePos);
        }
    }
}
```

---

## 5.4 Визуальная подсветка выбранного элемента (опционально)

### SelectionHighlighter.cs

Отдельный компонент для подсветки. Подписывается на события InputDetector.

```csharp
using UnityEngine;
using Match3.Grid;
using Match3.Board;
using DG.Tweening;

namespace Match3.Input
{
    /// <summary>
    /// Визуально подсвечивает выбранный элемент.
    /// </summary>
    public class SelectionHighlighter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputDetector _inputDetector;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private BoardComponent _board;

        [Header("Settings")]
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDuration = 0.3f;

        private Tween _currentTween;
        private Transform _selectedTransform;
        private Vector3 _originalScale;

        private void OnEnable()
        {
            _inputDetector.OnElementSelected += OnElementSelected;
            _inputDetector.OnSelectionCancelled += OnSelectionCancelled;
            _inputDetector.OnSwapRequested += OnSwapRequested;
        }

        private void OnDisable()
        {
            _inputDetector.OnElementSelected -= OnElementSelected;
            _inputDetector.OnSelectionCancelled -= OnSelectionCancelled;
            _inputDetector.OnSwapRequested -= OnSwapRequested;
        }

        private void OnElementSelected(Vector2Int pos)
        {
            StopHighlight();

            var element = _board.GetElement(pos);
            if (element == null) return;

            _selectedTransform = element.transform;
            _originalScale = _selectedTransform.localScale;

            // Пульсирующая анимация
            _currentTween = _selectedTransform
                .DOScale(_originalScale * _pulseScale, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void OnSelectionCancelled()
        {
            StopHighlight();
        }

        private void OnSwapRequested(Vector2Int from, Vector2Int to)
        {
            StopHighlight();
        }

        private void StopHighlight()
        {
            _currentTween?.Kill();
            _currentTween = null;

            if (_selectedTransform != null)
            {
                _selectedTransform.localScale = _originalScale;
                _selectedTransform = null;
            }
        }
    }
}
```

---

## 5.5 Editor Setup

### InputSystemSetup.cs

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Input;
using Match3.Grid;
using Match3.Board;

namespace Match3.Editor
{
    public static class InputSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 5 - Input System")]
        public static void SetupInputSystem()
        {
            // Находим GridComponent (как в предыдущих этапах)
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

            var gameObject = grid.gameObject;

            // InputBlocker
            var inputBlocker = gameObject.GetComponent<InputBlocker>();
            if (inputBlocker == null)
                inputBlocker = Undo.AddComponent<InputBlocker>(gameObject);

            // InputDetector
            var inputDetector = gameObject.GetComponent<InputDetector>();
            if (inputDetector == null)
                inputDetector = Undo.AddComponent<InputDetector>(gameObject);

            SetField(inputDetector, "_grid", grid);
            SetField(inputDetector, "_board", board);
            SetField(inputDetector, "_inputBlocker", inputBlocker);
            SetField(inputDetector, "_camera", Camera.main);

            // SelectionHighlighter
            var highlighter = gameObject.GetComponent<SelectionHighlighter>();
            if (highlighter == null)
                highlighter = Undo.AddComponent<SelectionHighlighter>(gameObject);

            SetField(highlighter, "_inputDetector", inputDetector);
            SetField(highlighter, "_board", board);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[Match3] Input System setup complete!");
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
| 1 | `SwipeDirection.cs` | - | Unit test: FromDelta правильно определяет направления |
| 2 | `InputBlocker.cs` | - | Unit test: Block/Unblock работает как стек |
| 3 | `InputDetector.cs` | Grid, Board, InputBlocker | Play mode: клики выбирают элементы, свайпы логируются |
| 4 | `SelectionHighlighter.cs` | InputDetector | Play mode: выбранный элемент пульсирует |
| 5 | `InputSystemSetup.cs` | Все выше | Editor: меню создаёт компоненты |

---

## Тестирование

### Ручной тест
1. Запустить Play Mode
2. Кликнуть по элементу → должен выделиться (пульсация)
3. Кликнуть по тому же → выделение снимается
4. Кликнуть по элементу, затем свайп → в консоли `OnSwapRequested(from, to)`
5. Кликнуть по элементу, затем кликнуть по соседнему → `OnSwapRequested`
6. Кликнуть по элементу, кликнуть по далёкому → выбор переключается

### Debug компонент (временный)

```csharp
using UnityEngine;
using Match3.Input;

namespace Match3.Debug
{
    public class InputDebugger : MonoBehaviour
    {
        [SerializeField] private InputDetector _inputDetector;

        private void OnEnable()
        {
            _inputDetector.OnElementSelected += pos =>
                UnityEngine.Debug.Log($"Selected: {pos}");
            _inputDetector.OnSelectionCancelled += () =>
                UnityEngine.Debug.Log("Selection cancelled");
            _inputDetector.OnSwapRequested += (from, to) =>
                UnityEngine.Debug.Log($"Swap requested: {from} → {to}");
        }
    }
}
```

---

## Интеграция с Этапом 6 (Swap System)

SwapHandler будет подписываться на `OnSwapRequested`:

```csharp
// В SwapHandler.cs (Этап 6)
private void OnEnable()
{
    _inputDetector.OnSwapRequested += HandleSwapRequest;
}

private void HandleSwapRequest(Vector2Int from, Vector2Int to)
{
    // 1. Блокировать ввод
    _inputBlocker.Block();

    // 2. Выполнить анимацию свапа
    // 3. Проверить матч
    // 4. Если нет матча - реверс
    // 5. Разблокировать ввод
    _inputBlocker.Unblock();
}
```

---

## Чеклист

- [x] `SwipeDirection.cs` создан
- [x] `InputBlocker.cs` создан
- [x] `InputDetector.cs` создан (с alias `UnityInput`)
- [x] `SelectionHighlighter.cs` создан
- [x] `InputSystemSetup.cs` создан (с `FindFirstObjectByType`)
- [x] Меню Setup работает
- [ ] Клик по элементу выделяет его
- [ ] Свайп определяется правильно
- [ ] Клик по соседнему элементу запрашивает свап
- [ ] События публикуются корректно
- [ ] InputBlocker блокирует ввод

---

## Возможные улучшения (будущее)

1. **Touch support** - `UnityInput.touchCount` для мобильных устройств
2. **Haptic feedback** - вибрация при выборе
3. **Sound feedback** - звук при выборе/свапе
4. **Visual feedback** - линия между элементами при драге
5. **New Input System** - переход на Unity Input System пакет

---

## Известные проблемы и решения

### 1. Namespace конфликт `Match3.Input` vs `UnityEngine.Input`

**Проблема:** Компилятор не может разрешить `Input.GetMouseButtonDown()` т.к. думает что это `Match3.Input`.

**Решение:** Использовать alias в начале файла:
```csharp
using UnityInput = UnityEngine.Input;
```

### 2. Editor Setup не находит GameManager

**Проблема:** `GameObject.Find("GameManager")` не работает если объект называется иначе.

**Решение:** Использовать `Object.FindFirstObjectByType<GridComponent>()` как в предыдущих этапах - ищем по компоненту, а не по имени.
