using UnityEngine;
using UnityEditor;
using Match3.Grid;
using Match3.Elements;
using Match3.Spawn;
using Match3.Gravity;

namespace Match3.Editor
{
    public static class GravitySceneSetup
    {
        [MenuItem("Match3/Setup/Step 8 - Gravity System")]
        public static void SetupGravityScene()
        {
            // Найти существующие компоненты
            var grid = Object.FindFirstObjectByType<GridComponent>();
            var factory = Object.FindFirstObjectByType<ElementFactoryComponent>();
            var spawn = Object.FindFirstObjectByType<SpawnComponent>();

            if (grid == null || factory == null || spawn == null)
            {
                Debug.LogError("Run Step 4 (Spawn) setup first!");
                return;
            }

            // Создать или найти GravitySystem
            var gravity = Object.FindFirstObjectByType<GravityComponent>();
            if (gravity == null)
            {
                var gravityGO = new GameObject("GravitySystem");
                gravity = gravityGO.AddComponent<GravityComponent>();
            }

            // Настроить зависимости через SerializedObject
            var so = new SerializedObject(gravity);
            so.FindProperty("_gridComponent").objectReferenceValue = grid;
            so.FindProperty("_spawnComponent").objectReferenceValue = spawn;
            so.ApplyModifiedProperties();

            // Создать или найти тестер
            var tester = Object.FindFirstObjectByType<GravityTester>();
            if (tester == null)
            {
                var testerGO = new GameObject("GravityTester");
                tester = testerGO.AddComponent<GravityTester>();
            }

            var testerSO = new SerializedObject(tester);
            testerSO.FindProperty("_gridComponent").objectReferenceValue = grid;
            testerSO.FindProperty("_gravityComponent").objectReferenceValue = gravity;
            testerSO.FindProperty("_camera").objectReferenceValue = Camera.main;
            testerSO.ApplyModifiedProperties();

            // Заполнить сетку если пустая
            if (!Application.isPlaying)
            {
                Debug.Log("Gravity System setup complete!");
                Debug.Log("Enter Play Mode, then:");
                Debug.Log("  - Click to remove elements");
                Debug.Log("  - Space to apply gravity");
                Debug.Log("  - G for auto test (remove random + gravity)");
            }

            Selection.activeGameObject = gravity.gameObject;
        }
    }
}
