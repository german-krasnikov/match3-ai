using UnityEngine;
using UnityEditor;
using Match3.Match;
using Match3.Grid;
using Match3.Spawn;

namespace Match3.Editor
{
    public static class MatchSceneSetup
    {
        [MenuItem("Match3/Setup Step 5 - Match Detection")]
        public static void SetupMatchScene()
        {
            // Ensure previous steps are set up
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.Log("Grid not found, running Grid setup...");
                GridSceneSetup.SetupGridScene();
                grid = Object.FindFirstObjectByType<GridComponent>();
            }

            var spawn = Object.FindFirstObjectByType<SpawnComponent>();
            if (spawn == null)
            {
                Debug.Log("SpawnComponent not found, running Spawn setup...");
                SpawnSceneSetup.SetupSpawnScene();
                spawn = Object.FindFirstObjectByType<SpawnComponent>();
            }

            // Disable SpawnTester auto-fill (MatchTester will handle it)
            var spawnTester = grid.gameObject.GetComponent<SpawnTester>();
            if (spawnTester != null)
            {
                var spawnTesterSo = new SerializedObject(spawnTester);
                spawnTesterSo.FindProperty("_fillOnStart").boolValue = false;
                spawnTesterSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Add MatchDetectionComponent
            var matchDetection = grid.gameObject.GetComponent<MatchDetectionComponent>();
            if (matchDetection == null)
            {
                matchDetection = Undo.AddComponent<MatchDetectionComponent>(grid.gameObject);
            }

            // Link dependencies
            var so = new SerializedObject(matchDetection);
            so.FindProperty("_grid").objectReferenceValue = grid;
            so.FindProperty("_minMatchLength").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add MatchTester
            var tester = grid.gameObject.GetComponent<MatchTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<MatchTester>(grid.gameObject);
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_grid").objectReferenceValue = grid;
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.FindProperty("_matchDetection").objectReferenceValue = matchDetection;
            testerSo.FindProperty("_fillGridOnStart").boolValue = true;
            testerSo.FindProperty("_testMatchesOnStart").boolValue = true;
            testerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(grid.gameObject);
            Selection.activeGameObject = grid.gameObject;

            Debug.Log("[Step 5] Match Detection setup complete!\n" +
                      "- MatchDetectionComponent added\n" +
                      "- MatchTester added (will fill grid and test on Play)\n" +
                      "Press Play to test. Console will show match count.");
        }

        [MenuItem("Match3/Test/Find All Matches (Play Mode)")]
        public static void TestFindMatchesInEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first.");
                return;
            }

            var matchDetection = Object.FindFirstObjectByType<MatchDetectionComponent>();
            if (matchDetection == null)
            {
                Debug.LogError("MatchDetectionComponent not found! Run setup first.");
                return;
            }

            var matches = matchDetection.FindAllMatches();
            Debug.Log($"Found {matches.Count} match positions.");
        }
    }
}
