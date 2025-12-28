using UnityEngine;
using UnityEditor;
using Match3.Grid;
using Match3.Spawn;
using Match3.Match;
using Match3.Destruction;

namespace Match3.Editor
{
    public static class DestructionSceneSetup
    {
        [MenuItem("Match3/Setup Step 7 - Destruction")]
        public static void SetupDestructionScene()
        {
            // Ensure Grid is set up
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.Log("Grid not found, running Grid setup...");
                GridSceneSetup.SetupGridScene();
                grid = Object.FindFirstObjectByType<GridComponent>();
            }

            // Ensure Spawn is set up
            var spawn = Object.FindFirstObjectByType<SpawnComponent>();
            if (spawn == null)
            {
                Debug.Log("Spawn not found, running Spawn setup...");
                SpawnSceneSetup.SetupSpawnScene();
                spawn = Object.FindFirstObjectByType<SpawnComponent>();
            }

            // Add DestructionComponent to Grid object
            var destruction = grid.gameObject.GetComponent<DestructionComponent>();
            if (destruction == null)
            {
                destruction = Undo.AddComponent<DestructionComponent>(grid.gameObject);
            }

            // Link dependencies
            var so = new SerializedObject(destruction);
            so.FindProperty("_grid").objectReferenceValue = grid;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add DestructionTester
            var tester = grid.gameObject.GetComponent<DestructionTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<DestructionTester>(grid.gameObject);
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_destruction").objectReferenceValue = destruction;
            testerSo.FindProperty("_grid").objectReferenceValue = grid;
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.ApplyModifiedPropertiesWithoutUndo();

            // Link Destruction to MatchTester if exists
            var matchTester = grid.gameObject.GetComponent<MatchTester>();
            if (matchTester != null)
            {
                var matchTesterSo = new SerializedObject(matchTester);
                matchTesterSo.FindProperty("_destruction").objectReferenceValue = destruction;
                matchTesterSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("Linked DestructionComponent to MatchTester");
            }

            EditorUtility.SetDirty(grid.gameObject);
            Selection.activeGameObject = grid.gameObject;

            Debug.Log("Step 7 Destruction setup complete.\n" +
                      "Test in Play mode - Right-click DestructionTester:\n" +
                      "  1. Fill Grid\n" +
                      "  2. Destroy Test Positions\n" +
                      "  3. Destroy Random Row\n" +
                      "  4. Destroy Random Column");
        }

        [MenuItem("Match3/Test/Destroy Diagonal (Play Mode)")]
        public static void TestDestroyDiagonal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first!");
                return;
            }

            var tester = Object.FindFirstObjectByType<DestructionTester>();
            if (tester == null)
            {
                Debug.LogError("DestructionTester not found! Run setup first.");
                return;
            }

            // Invoke via reflection since method is private
            var method = typeof(DestructionTester).GetMethod("DestroyTestPositions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(tester, null);
        }
    }
}
