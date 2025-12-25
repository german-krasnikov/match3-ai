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

        [MenuItem("Match3/Setup/Create Spawn System")]
        public static void CreateSpawnSystem()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[SpawnSystemSetup] GridComponent not found in scene. Create Grid first.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<ElementComponent>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SpawnSystemSetup] Element prefab not found at {PrefabPath}");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<ElementDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[SpawnSystemSetup] ElementDatabase not found at {DatabasePath}");
                return;
            }

            // Create hierarchy
            var root = new GameObject("SpawnSystem");
            Undo.RegisterCreatedObjectUndo(root, "Create Spawn System");

            // Pool
            var poolGO = new GameObject("ElementPool");
            poolGO.transform.SetParent(root.transform);
            var pool = poolGO.AddComponent<ElementPool>();
            SetPrivateField(pool, "_prefab", prefab);
            SetPrivateField(pool, "_initialSize", 64);

            // Factory
            var factoryGO = new GameObject("ElementFactory");
            factoryGO.transform.SetParent(root.transform);
            var factory = factoryGO.AddComponent<ElementFactory>();
            SetPrivateField(factory, "_pool", pool);
            SetPrivateField(factory, "_database", database);

            // Spawner
            var spawnerGO = new GameObject("InitialBoardSpawner");
            spawnerGO.transform.SetParent(root.transform);
            var spawner = spawnerGO.AddComponent<InitialBoardSpawner>();
            SetPrivateField(spawner, "_grid", grid);
            SetPrivateField(spawner, "_factory", factory);
            SetPrivateField(spawner, "_spawnOnStart", true);

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);

            Debug.Log("[SpawnSystemSetup] Created SpawnSystem hierarchy. Wire references if needed.");
        }

        private static void SetPrivateField<T>(Component component, string fieldName, T value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (typeof(T) == typeof(int))
                    prop.intValue = (int)(object)value;
                else if (value is Object obj)
                    prop.objectReferenceValue = obj;

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [MenuItem("Match3/Setup/Create Spawn System", validate = true)]
        public static bool ValidateCreateSpawnSystem()
        {
            return Object.FindFirstObjectByType<GridComponent>() != null;
        }
    }
}
