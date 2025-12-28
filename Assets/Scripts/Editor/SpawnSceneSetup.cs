using UnityEngine;
using UnityEditor;
using Match3.Spawn;
using Match3.Grid;
using Match3.Elements;

namespace Match3.Editor
{
    public static class SpawnSceneSetup
    {
        [MenuItem("Match3/Setup Step 4 - Spawn")]
        public static void SetupSpawnScene()
        {
            // Ensure Grid is set up
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.Log("Grid not found, running Grid setup...");
                GridSceneSetup.SetupGridScene();
                grid = Object.FindFirstObjectByType<GridComponent>();
            }

            // Ensure Elements are set up
            var factory = Object.FindFirstObjectByType<ElementFactoryComponent>();
            if (factory == null)
            {
                Debug.Log("ElementFactory not found, running Elements setup...");
                ElementSceneSetup.SetupElementsScene();
                factory = Object.FindFirstObjectByType<ElementFactoryComponent>();
            }

            // Add SpawnComponent to Grid object
            var spawn = grid.gameObject.GetComponent<SpawnComponent>();
            if (spawn == null)
            {
                spawn = Undo.AddComponent<SpawnComponent>(grid.gameObject);
            }

            // Link dependencies
            var so = new SerializedObject(spawn);
            so.FindProperty("_gridComponent").objectReferenceValue = grid;
            so.FindProperty("_factoryComponent").objectReferenceValue = factory;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add SpawnTester
            var tester = grid.gameObject.GetComponent<SpawnTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<SpawnTester>(grid.gameObject);
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(grid.gameObject);
            Selection.activeGameObject = grid.gameObject;

            Debug.Log("Step 4 Spawn setup complete. Press Play → Space to fill grid.");
        }

        [MenuItem("Match3/Test/Fill Grid Now (Editor)")]
        public static void TestFillGridInEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first, then press Space to fill grid.");
                return;
            }

            var spawn = Object.FindFirstObjectByType<SpawnComponent>();
            if (spawn == null)
            {
                Debug.LogError("SpawnComponent not found! Run setup first.");
                return;
            }

            spawn.FillGrid();
            Debug.Log("Grid filled!");
        }
    }
}
