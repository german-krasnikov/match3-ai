#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Matching;
using Match3.Grid;
using Match3.Board;
using Match3.Swap;

namespace Match3.Editor
{
    public static class MatchSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 7 - Match System")]
        public static void SetupMatchSystem()
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

            var swapHandler = grid.GetComponent<SwapHandler>();
            if (swapHandler == null)
            {
                Debug.LogError("[Match3] SwapHandler not found. Run Stage 6 setup first.");
                return;
            }

            var go = grid.gameObject;

            // MatchFinder
            var matchFinder = go.GetComponent<MatchFinder>();
            if (matchFinder == null)
                matchFinder = Undo.AddComponent<MatchFinder>(go);

            SetField(matchFinder, "_board", board);

            // MatchHighlighter
            var matchHighlighter = go.GetComponent<MatchHighlighter>();
            if (matchHighlighter == null)
                matchHighlighter = Undo.AddComponent<MatchHighlighter>(go);

            SetField(matchHighlighter, "_grid", grid);
            SetField(matchHighlighter, "_matchFinder", matchFinder);

            // Wire SwapHandler
            SetField(swapHandler, "_matchFinder", matchFinder);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Match System setup complete!");
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
