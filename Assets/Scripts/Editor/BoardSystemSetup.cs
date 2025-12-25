#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Grid;
using Match3.Board;
using Match3.Spawn;

namespace Match3.Editor
{
    public static class BoardSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 4 - Board System")]
        public static void SetupBoardScene()
        {
            // 1. Ensure previous stages are set up
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            var spawner = Object.FindFirstObjectByType<InitialBoardSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[Match3] InitialBoardSpawner not found. Run Stage 3 setup first.");
                return;
            }

            // 2. Add BoardComponent to Grid GameObject (if not exists)
            var board = grid.GetComponent<BoardComponent>();
            if (board == null)
            {
                board = Undo.AddComponent<BoardComponent>(grid.gameObject);
                Debug.Log("[Match3] Added BoardComponent to Grid");
            }

            // 3. Wire dependencies
            SetField(board, "_grid", grid);
            SetField(spawner, "_board", board);

            EditorUtility.SetDirty(grid.gameObject);
            EditorUtility.SetDirty(spawner);

            Selection.activeGameObject = grid.gameObject;
            Debug.Log("[Match3] Board System setup complete! Press Play to test.");
        }

        private static void SetField<T>(Component component, string fieldName, T value) where T : Object
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);
            }
        }
    }
}
#endif
