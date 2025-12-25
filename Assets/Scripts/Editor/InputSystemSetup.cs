#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Input;
using Match3.Grid;
using Match3.Board;

namespace Match3.Editor
{
    public static class InputSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 5 - Input System")]
        public static void SetupInputSystem()
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

            var gameObject = grid.gameObject;

            // InputBlocker
            var inputBlocker = gameObject.GetComponent<InputBlocker>();
            if (inputBlocker == null)
                inputBlocker = Undo.AddComponent<InputBlocker>(gameObject);

            // InputDetector
            var inputDetector = gameObject.GetComponent<InputDetector>();
            if (inputDetector == null)
                inputDetector = Undo.AddComponent<InputDetector>(gameObject);

            SetField(inputDetector, "_grid", grid);
            SetField(inputDetector, "_board", board);
            SetField(inputDetector, "_inputBlocker", inputBlocker);
            SetField(inputDetector, "_camera", Camera.main);

            // SelectionHighlighter
            var highlighter = gameObject.GetComponent<SelectionHighlighter>();
            if (highlighter == null)
                highlighter = Undo.AddComponent<SelectionHighlighter>(gameObject);

            SetField(highlighter, "_inputDetector", inputDetector);
            SetField(highlighter, "_board", board);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[Match3] Input System setup complete!");
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
