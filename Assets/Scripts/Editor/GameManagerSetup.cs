#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
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
        private const string GRID_DATA_PATH = "Assets/Data/Grid/DefaultGridData.asset";
        private const string ELEMENT_DB_PATH = "Assets/Data/Elements/ElementDatabase.asset";
        private const string ELEMENT_PREFAB_PATH = "Assets/Prefabs/Element.prefab";
        private const string PREFAB_PATH = "Assets/Prefabs/GameManager.prefab";

        [MenuItem("Match3/Setup Scene/Complete Setup (All Stages)", priority = 0)]
        public static void CompleteSetup()
        {
            CleanExistingGameManager();
            var go = CreateGameManager();

            AddCoreComponents(go);
            AddSpawnComponents(go);
            AddInputComponents(go);
            AddSwapComponents(go);
            AddMatchComponents(go);
            AddDestroyComponents(go);
            AddFallComponents(go);
            AddRefillComponents(go);
            AddGameLoopComponents(go);

            EditorUtility.SetDirty(go);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("[Match3] Complete setup finished!");
        }

        [MenuItem("Match3/Setup Scene/Clean Scene", priority = 10)]
        public static void CleanScene()
        {
            CleanExistingGameManager();
            Debug.Log("[Match3] Scene cleaned.");
        }

        [MenuItem("Match3/Setup Scene/Create GameManager Prefab", priority = 20)]
        public static void CreatePrefab()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] Run Complete Setup first!");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(grid.gameObject, PREFAB_PATH);
            Debug.Log($"[Match3] Prefab created at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        [MenuItem("Match3/Setup Scene/Validate Setup", priority = 30)]
        public static void ValidateSetup()
        {
            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] Validation failed: GridComponent not found");
                return;
            }

            var go = grid.gameObject;

            // Required components
            Check<BoardComponent>(go, errors);
            Check<ElementPool>(go, errors);
            Check<ElementFactory>(go, errors);
            Check<InitialBoardSpawner>(go, errors);
            Check<InputBlocker>(go, errors);
            Check<InputDetector>(go, errors);
            Check<SelectionHighlighter>(go, errors);
            Check<SwapAnimator>(go, errors);
            Check<SwapHandler>(go, errors);
            Check<MatchFinder>(go, errors);
            Check<DestroyAnimator>(go, errors);
            Check<DestroyHandler>(go, errors);
            Check<FallAnimator>(go, errors);
            Check<FallHandler>(go, errors);
            Check<RefillAnimator>(go, errors);
            Check<RefillHandler>(go, errors);
            Check<BoardShuffler>(go, errors);
            Check<GameLoopController>(go, errors);

            // Optional
            if (go.GetComponent<MatchHighlighter>() == null)
                warnings.Add("MatchHighlighter (optional debug)");

            // Assets
            if (AssetDatabase.LoadAssetAtPath<GridData>(GRID_DATA_PATH) == null)
                errors.Add($"GridData not found at {GRID_DATA_PATH}");
            if (AssetDatabase.LoadAssetAtPath<ElementDatabase>(ELEMENT_DB_PATH) == null)
                errors.Add($"ElementDatabase not found at {ELEMENT_DB_PATH}");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ELEMENT_PREFAB_PATH) == null)
                errors.Add($"Element prefab not found at {ELEMENT_PREFAB_PATH}");

            if (errors.Count > 0)
                Debug.LogError("[Match3] Validation FAILED:\n- " + string.Join("\n- ", errors));
            else
                Debug.Log("[Match3] Validation PASSED!");

            if (warnings.Count > 0)
                Debug.LogWarning("[Match3] Missing optional:\n- " + string.Join("\n- ", warnings));
        }

        private static void Check<T>(GameObject go, System.Collections.Generic.List<string> errors) where T : Component
        {
            if (go.GetComponent<T>() == null)
                errors.Add(typeof(T).Name);
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

        private static void AddCoreComponents(GameObject go)
        {
            var grid = Add<GridComponent>(go);
            var gridData = AssetDatabase.LoadAssetAtPath<GridData>(GRID_DATA_PATH);
            if (gridData != null)
                Set(grid, "_gridData", gridData);
            else
                Debug.LogError($"[Match3] GridData not found at {GRID_DATA_PATH}");

            var board = Add<BoardComponent>(go);
            Set(board, "_grid", grid);
        }

        private static void AddSpawnComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();
            var elementDb = AssetDatabase.LoadAssetAtPath<ElementDatabase>(ELEMENT_DB_PATH);
            var elementPrefab = AssetDatabase.LoadAssetAtPath<ElementComponent>(ELEMENT_PREFAB_PATH);

            if (elementDb == null)
                Debug.LogError($"[Match3] ElementDatabase not found at {ELEMENT_DB_PATH}");
            if (elementPrefab == null)
                Debug.LogError($"[Match3] Element prefab not found at {ELEMENT_PREFAB_PATH}");

            var pool = Add<ElementPool>(go);
            if (elementPrefab != null)
                Set(pool, "_prefab", elementPrefab);

            var factory = Add<ElementFactory>(go);
            Set(factory, "_pool", pool);
            if (elementDb != null)
                Set(factory, "_database", elementDb);

            var spawner = Add<InitialBoardSpawner>(go);
            Set(spawner, "_grid", grid);
            Set(spawner, "_factory", factory);
            Set(spawner, "_board", board);
        }

        private static void AddInputComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();

            Add<InputBlocker>(go);

            var input = Add<InputDetector>(go);
            Set(input, "_grid", grid);
            Set(input, "_board", board);
            Set(input, "_inputBlocker", go.GetComponent<InputBlocker>());

            var highlight = Add<SelectionHighlighter>(go);
            Set(highlight, "_inputDetector", input);
            Set(highlight, "_board", board);
        }

        private static void AddSwapComponents(GameObject go)
        {
            var grid = go.GetComponent<GridComponent>();
            var board = go.GetComponent<BoardComponent>();
            var input = go.GetComponent<InputDetector>();

            var animator = Add<SwapAnimator>(go);

            var handler = Add<SwapHandler>(go);
            Set(handler, "_board", board);
            Set(handler, "_grid", grid);
            Set(handler, "_inputDetector", input);
            Set(handler, "_swapAnimator", animator);
        }

        private static void AddMatchComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();
            var swap = go.GetComponent<SwapHandler>();

            var finder = Add<MatchFinder>(go);
            Set(finder, "_board", board);
            Set(swap, "_matchFinder", finder);

            var highlight = Add<MatchHighlighter>(go);
            Set(highlight, "_matchFinder", finder);
            Set(highlight, "_grid", grid);
        }

        private static void AddDestroyComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var factory = go.GetComponent<ElementFactory>();

            var animator = Add<DestroyAnimator>(go);

            var handler = Add<DestroyHandler>(go);
            Set(handler, "_board", board);
            Set(handler, "_factory", factory);
            Set(handler, "_animator", animator);
        }

        private static void AddFallComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();

            var animator = Add<FallAnimator>(go);

            var handler = Add<FallHandler>(go);
            Set(handler, "_board", board);
            Set(handler, "_grid", grid);
            Set(handler, "_animator", animator);
        }

        private static void AddRefillComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();
            var factory = go.GetComponent<ElementFactory>();

            var animator = Add<RefillAnimator>(go);

            var handler = Add<RefillHandler>(go);
            Set(handler, "_board", board);
            Set(handler, "_grid", grid);
            Set(handler, "_factory", factory);
            Set(handler, "_animator", animator);
        }

        private static void AddGameLoopComponents(GameObject go)
        {
            var board = go.GetComponent<BoardComponent>();
            var grid = go.GetComponent<GridComponent>();

            var shuffler = Add<BoardShuffler>(go);
            Set(shuffler, "_board", board);
            Set(shuffler, "_grid", grid);

            var loop = Add<GameLoopController>(go);
            Set(loop, "_board", board);
            Set(loop, "_inputBlocker", go.GetComponent<InputBlocker>());
            Set(loop, "_swapHandler", go.GetComponent<SwapHandler>());
            Set(loop, "_matchFinder", go.GetComponent<MatchFinder>());
            Set(loop, "_destroyHandler", go.GetComponent<DestroyHandler>());
            Set(loop, "_fallHandler", go.GetComponent<FallHandler>());
            Set(loop, "_refillHandler", go.GetComponent<RefillHandler>());
            Set(loop, "_boardShuffler", shuffler);
        }

        private static T Add<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : Undo.AddComponent<T>(go);
        }

        private static void Set(Component target, string field, Object value)
        {
            using (var so = new SerializedObject(target))
            {
                so.Update();
                var prop = so.FindProperty(field);
                if (prop != null)
                {
                    prop.objectReferenceValue = value;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning($"[Match3] Field '{field}' not found on {target.GetType().Name}");
                }
            }
        }
    }
}
#endif
