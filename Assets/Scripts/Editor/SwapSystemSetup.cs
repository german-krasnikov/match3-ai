#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Swap;
using Match3.Grid;
using Match3.Board;
using Match3.Input;

namespace Match3.Editor
{
    /// <summary>
    /// Editor utility for setting up Stage 6 (Swap System) components.
    /// Adds SwapAnimator and SwapHandler to the scene and wires all dependencies.
    /// </summary>
    public static class SwapSystemSetup
    {
        /// <summary>
        /// Sets up Swap System components on the Grid GameObject.
        /// Prerequisites: Stages 1, 4, 5 must be set up first.
        /// </summary>
        [MenuItem("Match3/Setup Scene/Stage 6 - Swap System")]
        public static void SetupSwapSystem()
        {
            // Find Grid (root object for all game components)
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            // Verify Stage 4 (Board)
            var board = grid.GetComponent<BoardComponent>();
            if (board == null)
            {
                Debug.LogError("[Match3] BoardComponent not found. Run Stage 4 setup first.");
                return;
            }

            // Verify Stage 5 (Input)
            var inputDetector = grid.GetComponent<InputDetector>();
            var inputBlocker = grid.GetComponent<InputBlocker>();
            if (inputDetector == null || inputBlocker == null)
            {
                Debug.LogError("[Match3] Input system not found. Run Stage 5 setup first.");
                return;
            }

            var gameObject = grid.gameObject;

            // Add SwapAnimator (handles DOTween animations)
            var swapAnimator = GetOrAddComponent<SwapAnimator>(gameObject);

            // Add SwapHandler (orchestrates swap logic)
            var swapHandler = GetOrAddComponent<SwapHandler>(gameObject);

            // Wire dependencies
            SetField(swapHandler, "_board", board);
            SetField(swapHandler, "_grid", grid);
            SetField(swapHandler, "_inputDetector", inputDetector);
            SetField(swapHandler, "_inputBlocker", inputBlocker);
            SetField(swapHandler, "_swapAnimator", swapAnimator);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[Match3] Swap System setup complete! Components added: SwapAnimator, SwapHandler");
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = Undo.AddComponent<T>(go);
            return component;
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
