#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Destroy;
using Match3.Grid;
using Match3.Board;
using Match3.Spawn;
using Match3.Swap;

namespace Match3.Editor
{
    public static class DestroySystemSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 8 - Destroy System")]
        public static void SetupDestroySystem()
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

            var go = grid.gameObject;

            var destroyAnimator = go.GetComponent<DestroyAnimator>();
            if (destroyAnimator == null)
                destroyAnimator = Undo.AddComponent<DestroyAnimator>(go);

            var destroyHandler = go.GetComponent<DestroyHandler>();
            if (destroyHandler == null)
                destroyHandler = Undo.AddComponent<DestroyHandler>(go);

            SetField(destroyHandler, "_board", board);
            SetField(destroyHandler, "_factory", factory);
            SetField(destroyHandler, "_animator", destroyAnimator);

            // Wire SwapHandler
            var swapHandler = go.GetComponent<SwapHandler>();
            if (swapHandler != null)
                SetField(swapHandler, "_destroyHandler", destroyHandler);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Destroy System setup complete!");
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
