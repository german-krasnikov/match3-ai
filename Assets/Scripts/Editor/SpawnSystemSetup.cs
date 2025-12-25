#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Spawn;
using Match3.Elements;
using Match3.Grid;

namespace Match3.Editor
{
    public static class SpawnSystemSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Element.prefab";
        private const string DatabasePath = "Assets/Data/Elements/ElementDatabase.asset";

        [MenuItem("Match3/Setup Scene/Stage 3 - Spawn System")]
        public static void SetupSpawnScene()
        {
            // 1. Ensure Grid exists
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.Log("[Match3] Grid not found, running Stage 1 setup first...");
                GridSceneSetup.SetupGridScene();
                grid = Object.FindFirstObjectByType<GridComponent>();
            }

            // 2. Load required assets
            var prefab = AssetDatabase.LoadAssetAtPath<ElementComponent>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Match3] Element prefab not found at {PrefabPath}. Run 'Match3/Setup/Create Element Assets' first.");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<ElementDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[Match3] ElementDatabase not found at {DatabasePath}. Run 'Match3/Setup/Create Element Assets' first.");
                return;
            }

            // 3. Remove old test spawner if exists
            RemoveOldTestSpawner();

            // 4. Create or update SpawnSystem
            var spawnSystem = CreateOrUpdateSpawnSystem(grid, prefab, database);

            Selection.activeGameObject = spawnSystem;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[Match3] Spawn System setup complete! Press Play to test.");
        }

        private static void RemoveOldTestSpawner()
        {
            // Find by type name since class might be deleted
            var allMonos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mono in allMonos)
            {
                if (mono != null && mono.GetType().Name == "ElementSpawnerTest")
                {
                    Debug.Log($"[Match3] Removing old ElementSpawnerTest from {mono.gameObject.name}");
                    Undo.DestroyObjectImmediate(mono);
                }
            }
        }

        private static GameObject CreateOrUpdateSpawnSystem(GridComponent grid, ElementComponent prefab, ElementDatabase database)
        {
            // Check if already exists
            var existingPool = Object.FindFirstObjectByType<ElementPool>();
            if (existingPool != null)
            {
                Debug.Log("[Match3] SpawnSystem already exists, updating references...");
                UpdateExistingSystem(existingPool, grid, prefab, database);
                return existingPool.transform.root.gameObject;
            }

            // Create new hierarchy
            var root = new GameObject("SpawnSystem");
            Undo.RegisterCreatedObjectUndo(root, "Create Spawn System");

            // Pool
            var poolGO = new GameObject("ElementPool");
            poolGO.transform.SetParent(root.transform);
            var pool = poolGO.AddComponent<ElementPool>();
            SetField(pool, "_prefab", prefab);
            SetField(pool, "_initialSize", 64);

            // Factory
            var factoryGO = new GameObject("ElementFactory");
            factoryGO.transform.SetParent(root.transform);
            var factory = factoryGO.AddComponent<ElementFactory>();
            SetField(factory, "_pool", pool);
            SetField(factory, "_database", database);

            // Spawner
            var spawnerGO = new GameObject("InitialBoardSpawner");
            spawnerGO.transform.SetParent(root.transform);
            var spawner = spawnerGO.AddComponent<InitialBoardSpawner>();
            SetField(spawner, "_grid", grid);
            SetField(spawner, "_factory", factory);
            SetField(spawner, "_spawnOnStart", true);

            EditorUtility.SetDirty(root);
            return root;
        }

        private static void UpdateExistingSystem(ElementPool pool, GridComponent grid, ElementComponent prefab, ElementDatabase database)
        {
            SetField(pool, "_prefab", prefab);

            var factory = Object.FindFirstObjectByType<ElementFactory>();
            if (factory != null)
            {
                SetField(factory, "_pool", pool);
                SetField(factory, "_database", database);
            }

            var spawner = Object.FindFirstObjectByType<InitialBoardSpawner>();
            if (spawner != null)
            {
                SetField(spawner, "_grid", grid);
                SetField(spawner, "_factory", factory);
            }
        }

        private static void SetField<T>(Component component, string fieldName, T value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            if (typeof(T) == typeof(int))
                prop.intValue = (int)(object)value;
            else if (typeof(T) == typeof(bool))
                prop.boolValue = (bool)(object)value;
            else if (value is Object obj)
                prop.objectReferenceValue = obj;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }
    }
}
#endif
