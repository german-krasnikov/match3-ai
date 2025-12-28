using UnityEngine;
using UnityEditor;
using Match3.Swap;
using Match3.Grid;
using Match3.Spawn;
using Match3.Match;

namespace Match3.Editor
{
    public static class SwapSceneSetup
    {
        [MenuItem("Match3/Setup Step 6 - Swap System")]
        public static void SetupSwapScene()
        {
            // Ensure previous steps
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

            var matchDetection = Object.FindFirstObjectByType<MatchDetectionComponent>();
            if (matchDetection == null)
            {
                Debug.Log("MatchDetection not found, running Match setup...");
                MatchSceneSetup.SetupMatchScene();
                matchDetection = Object.FindFirstObjectByType<MatchDetectionComponent>();
            }

            // Disable other testers
            DisableOtherTesters(grid.gameObject);

            // Get main camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found! Ensure scene has a camera tagged MainCamera.");
                return;
            }

            // Add InputComponent
            var input = grid.gameObject.GetComponent<InputComponent>();
            if (input == null)
            {
                input = Undo.AddComponent<InputComponent>(grid.gameObject);
            }

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("_grid").objectReferenceValue = grid;
            inputSo.FindProperty("_camera").objectReferenceValue = mainCamera;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // Add SwapComponent
            var swap = grid.gameObject.GetComponent<SwapComponent>();
            if (swap == null)
            {
                swap = Undo.AddComponent<SwapComponent>(grid.gameObject);
            }

            var swapSo = new SerializedObject(swap);
            swapSo.FindProperty("_grid").objectReferenceValue = grid;
            swapSo.ApplyModifiedPropertiesWithoutUndo();

            // Add SwapTester
            var tester = grid.gameObject.GetComponent<SwapTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<SwapTester>(grid.gameObject);
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.FindProperty("_input").objectReferenceValue = input;
            testerSo.FindProperty("_swap").objectReferenceValue = swap;
            testerSo.FindProperty("_matchDetection").objectReferenceValue = matchDetection;
            testerSo.FindProperty("_fillOnStart").boolValue = true;
            testerSo.FindProperty("_autoSwapBack").boolValue = true;
            testerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(grid.gameObject);
            Selection.activeGameObject = grid.gameObject;

            Debug.Log("[Step 6] Swap System setup complete!\n" +
                      "- InputComponent added (click handling)\n" +
                      "- SwapComponent added (swap + animation)\n" +
                      "- SwapTester added (orchestration)\n" +
                      "Press Play, then click two adjacent cells to swap.");
        }

        private static void DisableOtherTesters(GameObject gridObj)
        {
            var spawnTester = gridObj.GetComponent<SpawnTester>();
            if (spawnTester != null)
            {
                var so = new SerializedObject(spawnTester);
                so.FindProperty("_fillOnStart").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var matchTester = gridObj.GetComponent<MatchTester>();
            if (matchTester != null)
            {
                var so = new SerializedObject(matchTester);
                so.FindProperty("_fillGridOnStart").boolValue = false;
                so.FindProperty("_testMatchesOnStart").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
