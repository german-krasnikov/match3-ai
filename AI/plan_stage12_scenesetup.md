# Этап 12: Scene Setup - Детальный План Реализации

## Статус: В РАБОТЕ 🔄

---

## Обзор

Финализация структуры сцены и создание Master Setup script для полной настройки игры одним кликом.

### Текущее состояние

После 11 этапов все компоненты добавляются на один GameObject (где находится GridComponent). Каждый этап имеет свой Editor script:
- `GridSceneSetup.cs` → Stage 1
- `ElementsSetup.cs` → Stage 2
- `SpawnSystemSetup.cs` → Stage 3
- `BoardSystemSetup.cs` → Stage 4
- `InputSystemSetup.cs` → Stage 5
- `SwapSystemSetup.cs` → Stage 6
- `MatchSystemSetup.cs` → Stage 7
- `DestroySystemSetup.cs` → Stage 8
- `FallSystemSetup.cs` → Stage 9
- `RefillSystemSetup.cs` → Stage 10
- `GameLoopSetup.cs` → Stage 11

### Архитектурное решение

**Выбран плоский подход** — все компоненты на одном GameObject:

```
GameManager (GameObject)
├── GridComponent
├── BoardComponent
├── ElementPool
├── ElementFactory
├── InitialBoardSpawner
├── InputBlocker
├── InputDetector
├── SelectionHighlighter
├── SwapAnimator
├── SwapHandler
├── MatchFinder
├── MatchHighlighter (debug)
├── DestroyAnimator
├── DestroyHandler
├── FallAnimator
├── FallHandler
├── RefillAnimator
├── RefillHandler
├── BoardShuffler
└── GameLoopController
```

**Почему не иерархия?**
- Все компоненты тесно связаны через события
- Inspector удобнее с плоской структурой
- Нет необходимости в физическом разделении
- Проще отладка

---

## Задачи этапа

### 12.1 GameManagerSetup.cs (Master Editor Script)

Единый скрипт для полной настройки сцены одной кнопкой.

### 12.2 Создание Prefab

Prefab с полностью настроенным GameManager.

---

## Файлы

```
Assets/Scripts/
└── Editor/
    └── GameManagerSetup.cs      # Master setup script

Assets/Prefabs/
└── GameManager.prefab           # Готовый prefab
```

---

## 12.1 GameManagerSetup.cs

### Назначение

Единый Editor script для настройки всей сцены одним кликом.

### Код

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Core;
using Match3.Grid;
using Match3.Board;
using Match3.Input;
using Match3.Spawn;
using Match3.Swap;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;
using Match3.GameLoop;
using Match3.Elements;

namespace Match3.Editor
{
    public static class GameManagerSetup
    {
        private const string PREFAB_PATH = "Assets/Prefabs/GameManager.prefab";
        private const string GRID_DATA_PATH = "Assets/Data/Grid/DefaultGridData.asset";
        private const string ELEMENT_DB_PATH = "Assets/Data/Elements/ElementDatabase.asset";
        private const string ELEMENT_PREFAB_PATH = "Assets/Prefabs/Element.prefab";

        [MenuItem("Match3/Setup Scene/Complete Setup (All Stages)", priority = 0)]
        public static void CompleteSetup()
        {
            // 1. Clean existing GameManager if present
            CleanExistingGameManager();

            // 2. Create fresh GameManager
            var gameManager = CreateGameManager();

            // 3. Add all components
            AddCoreComponents(gameManager);
            AddSpawnComponents(gameManager);
            AddInputComponents(gameManager);
            AddSwapComponents(gameManager);
            AddMatchComponents(gameManager);
            AddDestroyComponents(gameManager);
            AddFallComponents(gameManager);
            AddRefillComponents(gameManager);
            AddGameLoopComponents(gameManager);

            // 4. Wire dependencies (already done in Add* methods)
            EditorUtility.SetDirty(gameManager);
            Selection.activeGameObject = gameManager;
            Debug.Log("[Match3] Complete setup finished! Select GameManager in hierarchy.");
        }

        [MenuItem("Match3/Setup Scene/Clean Scene", priority = 10)]
        public static void CleanScene()
        {
            CleanExistingGameManager();
            Debug.Log("[Match3] Scene cleaned.");
        }

        private static void CleanExistingGameManager()
        {
            var existing = Object.FindFirstObjectByType<GridComponent>();
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log("[Match3] Removed existing GameManager.");
            }
        }

        private static GameObject CreateGameManager()
        {
            var go = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
            return go;
        }

        [MenuItem("Match3/Setup Scene/Create GameManager Prefab", priority = 1)]
        public static void CreatePrefab()
        {
            var existing = Object.FindFirstObjectByType<GridComponent>();
            if (existing == null)
            {
                Debug.LogError("[Match3] Run Complete Setup first!");
                return;
            }

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Create prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(existing.gameObject, PREFAB_PATH);
            Debug.Log($"[Match3] Prefab created at {PREFAB_PATH}");

            Selection.activeObject = prefab;
        }

        [MenuItem("Match3/Setup Scene/Validate Setup", priority = 2)]
        public static void ValidateSetup()
        {
            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            // Check GameManager exists
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                errors.Add("GridComponent not found");
                Debug.LogError("[Match3] Validation failed: " + string.Join(", ", errors));
                return;
            }

            var go = grid.gameObject;

            // Check all components exist
            CheckComponent<BoardComponent>(go, errors);
            CheckComponent<ElementPool>(go, errors);
            CheckComponent<ElementFactory>(go, errors);
            CheckComponent<InitialBoardSpawner>(go, errors);
            CheckComponent<InputBlocker>(go, errors);
            CheckComponent<InputDetector>(go, errors);
            CheckComponent<SwapAnimator>(go, errors);
            CheckComponent<SwapHandler>(go, errors);
            CheckComponent<MatchFinder>(go, errors);
            CheckComponent<DestroyAnimator>(go, errors);
            CheckComponent<DestroyHandler>(go, errors);
            CheckComponent<FallAnimator>(go, errors);
            CheckComponent<FallHandler>(go, errors);
            CheckComponent<RefillAnimator>(go, errors);
            CheckComponent<RefillHandler>(go, errors);
            CheckComponent<BoardShuffler>(go, errors);
            CheckComponent<GameLoopController>(go, errors);
            CheckComponent<SelectionHighlighter>(go, errors);

            // Check optional components
            if (go.GetComponent<MatchHighlighter>() == null)
                warnings.Add("MatchHighlighter not found (optional debug)");

            // Check assets
            var gridData = AssetDatabase.LoadAssetAtPath<GridData>(GRID_DATA_PATH);
            if (gridData == null) errors.Add($"GridData not found at {GRID_DATA_PATH}");

            var elementDb = AssetDatabase.LoadAssetAtPath<ElementDatabase>(ELEMENT_DB_PATH);
            if (elementDb == null) errors.Add($"ElementDatabase not found at {ELEMENT_DB_PATH}");

            var elementPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ELEMENT_PREFAB_PATH);
            if (elementPrefab == null) errors.Add($"Element prefab not found at {ELEMENT_PREFAB_PATH}");

            // Report
            if (errors.Count > 0)
            {
                Debug.LogError("[Match3] Validation FAILED:\n- " + string.Join("\n- ", errors));
            }
            else
            {
                Debug.Log("[Match3] Validation PASSED!");
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning("[Match3] Warnings:\n- " + string.Join("\n- ", warnings));
            }
        }

        private static void CheckComponent<T>(GameObject go, System.Collections.Generic.List<string> errors) where T : Component
        {
            if (go.GetComponent<T>() == null)
                errors.Add($"{typeof(T).Name} not found");
        }

        private static void AddCoreComponents(GameObject go)
        {
            // GridComponent
            var grid = GetOrAddComponent<GridComponent>(go);
            var gridData = AssetDatabase.LoadAssetAtPath<GridData>(GRID_DATA_PATH);
            if (gridData != null)
                SetField(grid, "_gridData", gridData);

            // BoardComponent
            var board = GetOrAddComponent<BoardComponent>(go);
            SetField(board, "_grid", grid);
        }

        private static void AddSpawnComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();

            var elementDb = AssetDatabase.LoadAssetAtPath<ElementDatabase>(ELEMENT_DB_PATH);
            var elementPrefab = AssetDatabase.LoadAssetAtPath<ElementComponent>(ELEMENT_PREFAB_PATH);

            // ElementPool
            var pool = GetOrAddComponent<ElementPool>(go);
            if (elementPrefab != null)
                SetField(pool, "_prefab", elementPrefab); // NOTE: field is _prefab, not _elementPrefab

            // ElementFactory
            var factory = GetOrAddComponent<ElementFactory>(go);
            SetField(factory, "_pool", pool);
            if (elementDb != null)
                SetField(factory, "_database", elementDb);

            // InitialBoardSpawner
            var spawner = GetOrAddComponent<InitialBoardSpawner>(go);
            SetField(spawner, "_grid", grid);
            SetField(spawner, "_factory", factory);
            SetField(spawner, "_board", board);
        }

        private static void AddInputComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();

            // InputBlocker
            GetOrAddComponent<InputBlocker>(go);

            // InputDetector
            var inputDetector = GetOrAddComponent<InputDetector>(go);
            SetField(inputDetector, "_grid", grid);
            SetField(inputDetector, "_board", board);
            SetField(inputDetector, "_inputBlocker", go.GetComponent<InputBlocker>());

            // SelectionHighlighter
            var highlighter = GetOrAddComponent<SelectionHighlighter>(go);
            SetField(highlighter, "_inputDetector", inputDetector);
            SetField(highlighter, "_board", board);
        }

        private static void AddSwapComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();
            var inputDetector = go.GetComponent<InputDetector>();

            // SwapAnimator
            var swapAnimator = GetOrAddComponent<SwapAnimator>(go);

            // SwapHandler
            var swapHandler = GetOrAddComponent<SwapHandler>(go);
            SetField(swapHandler, "_board", board);
            SetField(swapHandler, "_grid", grid);
            SetField(swapHandler, "_inputDetector", inputDetector);
            SetField(swapHandler, "_swapAnimator", swapAnimator);
        }

        private static void AddMatchComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();
            var swapHandler = go.GetComponent<SwapHandler>();

            // MatchFinder
            var matchFinder = GetOrAddComponent<MatchFinder>(go);
            SetField(matchFinder, "_board", board);

            // Wire to SwapHandler
            SetField(swapHandler, "_matchFinder", matchFinder);

            // MatchHighlighter (debug, optional)
            var highlighter = GetOrAddComponent<MatchHighlighter>(go);
            SetField(highlighter, "_matchFinder", matchFinder);
            SetField(highlighter, "_grid", grid);
        }

        private static void AddDestroyComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var factory = go.GetComponent<ElementFactory>();

            // DestroyAnimator
            var destroyAnimator = GetOrAddComponent<DestroyAnimator>(go);

            // DestroyHandler
            var destroyHandler = GetOrAddComponent<DestroyHandler>(go);
            SetField(destroyHandler, "_board", board);
            SetField(destroyHandler, "_factory", factory); // NOTE: uses _factory, not _pool
            SetField(destroyHandler, "_animator", destroyAnimator);
        }

        private static void AddFallComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();

            // FallAnimator
            var fallAnimator = GetOrAddComponent<FallAnimator>(go);

            // FallHandler
            var fallHandler = GetOrAddComponent<FallHandler>(go);
            SetField(fallHandler, "_board", board);
            SetField(fallHandler, "_grid", grid);
            SetField(fallHandler, "_animator", fallAnimator);
        }

        private static void AddRefillComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();
            var factory = go.GetComponent<ElementFactory>();

            // RefillAnimator
            var refillAnimator = GetOrAddComponent<RefillAnimator>(go);

            // RefillHandler
            var refillHandler = GetOrAddComponent<RefillHandler>(go);
            SetField(refillHandler, "_board", board);
            SetField(refillHandler, "_grid", grid);
            SetField(refillHandler, "_factory", factory);
            SetField(refillHandler, "_animator", refillAnimator);
        }

        private static void AddGameLoopComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();
            var inputBlocker = go.GetComponent<InputBlocker>();
            var swapHandler = go.GetComponent<SwapHandler>();
            var matchFinder = go.GetComponent<MatchFinder>();
            var destroyHandler = go.GetComponent<DestroyHandler>();
            var fallHandler = go.GetComponent<FallHandler>();
            var refillHandler = go.GetComponent<RefillHandler>();

            // BoardShuffler
            var shuffler = GetOrAddComponent<BoardShuffler>(go);
            SetField(shuffler, "_board", board);
            SetField(shuffler, "_grid", grid);

            // GameLoopController
            var gameLoop = GetOrAddComponent<GameLoopController>(go);
            SetField(gameLoop, "_board", board);
            SetField(gameLoop, "_inputBlocker", inputBlocker);
            SetField(gameLoop, "_swapHandler", swapHandler);
            SetField(gameLoop, "_matchFinder", matchFinder);
            SetField(gameLoop, "_destroyHandler", destroyHandler);
            SetField(gameLoop, "_fallHandler", fallHandler);
            SetField(gameLoop, "_refillHandler", refillHandler);
            SetField(gameLoop, "_boardShuffler", shuffler);
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = Undo.AddComponent<T>(go);
            return component;
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

## 12.2 GameInitializer.cs

### Назначение

Опциональный компонент для ручной инициализации (если `InitialBoardSpawner._spawnOnStart = false`).

### Важно

**`ElementPool.Prewarm()` уже вызывается в `ElementPool.Awake()`!**

Текущая реализация ElementPool:
```csharp
private void Awake()
{
    _pool = new Stack<ElementComponent>(_initialSize);
    // ...
    Prewarm(); // Уже вызывается автоматически!
}
```

Поэтому `GameInitializer` нужен только если:
1. Хотите отключить `_spawnOnStart` у `InitialBoardSpawner`
2. Хотите контролировать порядок инициализации вручную

### Код (опциональный)

```csharp
using UnityEngine;
using Match3.Spawn;

namespace Match3.Core
{
    /// <summary>
    /// Optional: Manual initialization control.
    /// Note: ElementPool.Prewarm() is already called in Awake().
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private InitialBoardSpawner _spawner;

        [Header("Settings")]
        [SerializeField] private bool _autoInitialize = true;

        private bool _initialized;

        public bool IsInitialized => _initialized;

        private void Start()
        {
            if (_autoInitialize)
                Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;

            // Pool is already prewarmed in ElementPool.Awake()
            // Just spawn the board
            if (_spawner != null)
                _spawner.SpawnInitialBoard();

            _initialized = true;
            Debug.Log("[GameInitializer] Initialization complete");
        }
    }
}
```

### Порядок инициализации (текущий)

```
Awake() order:
  ElementPool.Awake() → Prewarm() ← автоматически!

Start() order:
  InitialBoardSpawner.Start() → SpawnInitialBoard() ← если _spawnOnStart=true
```

Всё уже работает без GameInitializer! Он нужен только для ручного контроля.

---

## 12.3 Порядок инициализации

```
Scene Load
    │
    ▼
Awake() calls (order undefined between components)
    │
    ├── GameInitializer.Awake()
    │       └── ElementPool.Prewarm()
    │
    ├── Other Awake() calls...
    │
    ▼
Start() calls
    │
    └── InitialBoardSpawner.Start()
            └── SpawnInitialBoard()
                    └── BoardComponent.Initialize()
```

---

## 12.4 Camera Setup

Камера должна быть настроена для отображения всей сетки.

### Рекомендуемые настройки

```
Main Camera:
  Projection: Orthographic
  Size: 5 (для 8x8 сетки с cellSize=1)
  Position: (3.5, 3.5, -10) // Центр сетки
  Background: Solid Color (#1a1a2e или по вкусу)
```

### Расчёт размера камеры

```
GridWidth = 8
CellSize = 1.0
Spacing = 0.1 (если есть)

CameraSize = (GridWidth * (CellSize + Spacing)) / 2 + padding
           = (8 * 1.1) / 2 + 0.5
           = 4.9 ≈ 5
```

---

## Структура папок (финальная)

```
Assets/
├── Data/
│   ├── Grid/
│   │   └── DefaultGridData.asset
│   └── Elements/
│       ├── ElementDatabase.asset
│       ├── Red.asset
│       ├── Blue.asset
│       ├── Green.asset
│       ├── Yellow.asset
│       └── Purple.asset
│
├── Prefabs/
│   ├── Element.prefab
│   └── GameManager.prefab        ← NEW
│
├── Scenes/
│   └── SampleScene.unity
│
├── Scripts/
│   ├── Core/
│   │   └── GameInitializer.cs    ← NEW
│   ├── Grid/
│   ├── Elements/
│   ├── Spawn/
│   ├── Board/
│   ├── Input/
│   ├── Swap/
│   ├── Match/
│   ├── Destroy/
│   ├── Fall/
│   ├── Refill/
│   ├── GameLoop/
│   └── Editor/
│       └── GameManagerSetup.cs   ← NEW
│
└── Settings/
    └── URP settings...
```

---

## Использование

### Быстрый старт (новая сцена)

1. `Match3 → Setup Scene → Complete Setup (All Stages)`
2. Настроить камеру
3. Play!

### Создание Prefab

1. После Complete Setup
2. `Match3 → Setup Scene → Create GameManager Prefab`
3. Использовать prefab в других сценах

### Валидация

`Match3 → Setup Scene → Validate Setup` — проверяет все компоненты и assets.

---

## Тестирование

### Тест 1: Complete Setup на пустой сцене

1. Новая сцена (File → New Scene)
2. `Match3 → Setup Scene → Complete Setup (All Stages)`
3. Проверить что все компоненты добавлены
4. Play → доска должна появиться

### Тест 2: Validate Setup

1. После Complete Setup
2. `Match3 → Setup Scene → Validate Setup`
3. Не должно быть ошибок

### Тест 3: Prefab

1. Создать prefab
2. Новая сцена
3. Добавить prefab
4. Play → игра работает

### Тест 4: Full Game Loop

1. Play Mode
2. Сделать swap → match → destroy → fall → refill
3. Проверить cascade
4. Создать deadlock situation → shuffle

---

## Чеклист

### Код
- [ ] `GameManagerSetup.cs` — master setup script

### Editor Menu
- [ ] `Match3 → Setup Scene → Complete Setup (All Stages)` — полная настройка с нуля
- [ ] `Match3 → Setup Scene → Clean Scene` — удаление GameManager
- [ ] `Match3 → Setup Scene → Create GameManager Prefab` — создание prefab
- [ ] `Match3 → Setup Scene → Validate Setup` — проверка компонентов

### Assets
- [ ] `Assets/Prefabs/GameManager.prefab`

### Тестирование
- [ ] Complete Setup работает на пустой сцене
- [ ] Complete Setup удаляет старый GameManager и создаёт новый
- [ ] Validate Setup проходит без ошибок
- [ ] Prefab работает в новой сцене
- [ ] Полный game loop работает

---

## FAQ

### Q: Зачем Clean Scene если Complete Setup и так удаляет?

A: Complete Setup автоматически удаляет старый GameManager. Отдельный Clean Scene нужен только если хотите очистить без создания нового.

### Q: Почему плоская структура, а не иерархия?

A: Для Match-3 игры все системы тесно связаны. Иерархия добавила бы сложности без пользы. Плоская структура проще для отладки и понимания.

### Q: Как добавить новую систему?

A:
1. Создать компоненты в соответствующей папке
2. Добавить `Add*Components()` метод в `GameManagerSetup.cs`
3. Вызвать его из `CompleteSetup()`

### Q: Как тестировать отдельные системы?

A: Каждый этап имеет свой Editor script. Можно запускать отдельно через `Match3 → Setup Scene → Stage N - Name`.
