#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Refill;
using Match3.Grid;
using Match3.Board;
using Match3.Spawn;
using Match3.Fall;
using Match3.Swap;

namespace Match3.Editor
{
    public static class RefillSystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 10 - Refill System")]
        public static void SetupRefillSystem()
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

            var factory = Object.FindFirstObjectByType<ElementFactory>();
            if (factory == null)
            {
                Debug.LogError("[Match3] ElementFactory not found. Run Stage 3 setup first.");
                return;
            }

            var fallHandler = grid.GetComponent<FallHandler>();
            if (fallHandler == null)
            {
                Debug.LogError("[Match3] FallHandler not found. Run Stage 9 setup first.");
                return;
            }

            var swapHandler = grid.GetComponent<SwapHandler>();
            if (swapHandler == null)
            {
                Debug.LogError("[Match3] SwapHandler not found. Run Stage 6 setup first.");
                return;
            }

            var go = grid.gameObject;

            // RefillAnimator
            var refillAnimator = go.GetComponent<RefillAnimator>();
            if (refillAnimator == null)
                refillAnimator = Undo.AddComponent<RefillAnimator>(go);

            // RefillHandler
            var refillHandler = go.GetComponent<RefillHandler>();
            if (refillHandler == null)
                refillHandler = Undo.AddComponent<RefillHandler>(go);

            SetField(refillHandler, "_board", board);
            SetField(refillHandler, "_grid", grid);
            SetField(refillHandler, "_factory", factory);
            SetField(refillHandler, "_animator", refillAnimator);

            // Wire SwapHandler
            SetField(swapHandler, "_refillHandler", refillHandler);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Refill System setup complete!");
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
