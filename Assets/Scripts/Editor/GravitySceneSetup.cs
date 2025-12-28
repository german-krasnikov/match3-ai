using UnityEngine;
using UnityEditor;
using Match3.Grid;
using Match3.Spawn;
using Match3.Gravity;

namespace Match3.Editor
{
    public static class GravitySceneSetup
    {
        [MenuItem("Match3/Setup Step 8 - Gravity")]
        public static void SetupGravityScene()
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

            // Add GravityComponent to Grid object
            var gravity = grid.gameObject.GetComponent<GravityComponent>();
            if (gravity == null)
            {
                gravity = Undo.AddComponent<GravityComponent>(grid.gameObject);
            }

            // Link dependencies
            var so = new SerializedObject(gravity);
            so.FindProperty("_gridComponent").objectReferenceValue = grid;
            so.FindProperty("_spawnComponent").objectReferenceValue = spawn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add GravityTester
            var tester = grid.gameObject.GetComponent<GravityTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<GravityTester>(grid.gameObject);
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_gridComponent").objectReferenceValue = grid;
            testerSo.FindProperty("_gravityComponent").objectReferenceValue = gravity;
            testerSo.FindProperty("_camera").objectReferenceValue = Camera.main;
            testerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(grid.gameObject);
            Selection.activeGameObject = grid.gameObject;

            Debug.Log("Step 8 Gravity setup complete.\n" +
                      "Test in Play mode:\n" +
                      "  - Click element to remove it\n" +
                      "  - Space to apply gravity\n" +
                      "  - G for auto test (remove random + gravity)");
        }

        [MenuItem("Match3/Test/Apply Gravity (Play Mode)")]
        public static void TestApplyGravity()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first!");
                return;
            }

            var gravity = Object.FindFirstObjectByType<GravityComponent>();
            if (gravity == null)
            {
                Debug.LogError("GravityComponent not found! Run setup first.");
                return;
            }

            _ = gravity.ApplyGravity();
            Debug.Log("Gravity applied");
        }

        [MenuItem("Match3/Test/Remove Random Element (Play Mode)")]
        public static void TestRemoveRandom()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first!");
                return;
            }

            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("GridComponent not found!");
                return;
            }

            int x = Random.Range(0, grid.Width);
            int y = Random.Range(0, grid.Height);
            var pos = new Vector2Int(x, y);

            var element = grid.GetElementAt(pos);
            if (element != null)
            {
                Object.Destroy(element.GameObject);
                grid.ClearCell(pos);
                Debug.Log($"Removed element at {pos}");
            }
        }
    }
}
