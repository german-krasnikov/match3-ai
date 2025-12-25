#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Fall;
using Match3.Grid;
using Match3.Board;
using Match3.Swap;

namespace Match3.Editor
{
    public static class FallSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 9 - Fall System")]
        public static void SetupFallSystem()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            var board = grid.GetComponent<BoardComponent>();
            if (board == null)
            {
                Debug.LogError("[Match3] BoardComponent not found. Run Stage 4 setup first.");
                return;
            }

            var go = grid.gameObject;

            // FallAnimator
            var fallAnimator = go.GetComponent<FallAnimator>();
            if (fallAnimator == null)
                fallAnimator = Undo.AddComponent<FallAnimator>(go);

            // FallHandler
            var fallHandler = go.GetComponent<FallHandler>();
            if (fallHandler == null)
                fallHandler = Undo.AddComponent<FallHandler>(go);

            SetField(fallHandler, "_board", board);
            SetField(fallHandler, "_grid", grid);
            SetField(fallHandler, "_animator", fallAnimator);

            // Wire SwapHandler
            var swapHandler = go.GetComponent<SwapHandler>();
            if (swapHandler != null)
            {
                SetField(swapHandler, "_fallHandler", fallHandler);
            }

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Fall System setup complete!");
        }

        private static void SetField<T>(Component component, string fieldName, T value) where T : Object
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
