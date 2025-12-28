#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Grid;
using Match3.Spawn;
using Match3.Destruction;

namespace Match3.Editor
{
    public static class DestructionSceneSetup
    {
        [MenuItem("Match3/Setup/Step 7 - Destruction System")]
        public static void SetupDestructionSystem()
        {
            // Find existing systems
            var grid = Object.FindFirstObjectByType<GridComponent>();
            var spawn = Object.FindFirstObjectByType<SpawnComponent>();

            if (grid == null)
            {
                Debug.LogError("GridComponent not found! Run Step 2 setup first.");
                return;
            }

            if (spawn == null)
            {
                Debug.LogError("SpawnComponent not found! Run Step 4 setup first.");
                return;
            }

            // Create Destruction system
            var destructionGO = new GameObject("Destruction");
            var destruction = destructionGO.AddComponent<DestructionComponent>();

            // Set references via SerializedObject
            var so = new SerializedObject(destruction);
            so.FindProperty("_grid").objectReferenceValue = grid;
            so.ApplyModifiedProperties();

            // Create tester
            var testerGO = new GameObject("DestructionTester");
            var tester = testerGO.AddComponent<DestructionTester>();

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_destruction").objectReferenceValue = destruction;
            testerSo.FindProperty("_grid").objectReferenceValue = grid;
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.ApplyModifiedProperties();

            Selection.activeGameObject = destructionGO;

            Debug.Log("Destruction System setup complete!\n" +
                      "Test: Use ContextMenu on DestructionTester:\n" +
                      "1. Fill Grid\n" +
                      "2. Destroy Test Positions\n" +
                      "3. Destroy Random Row\n" +
                      "4. Destroy Random Column");
        }
    }
}
#endif
