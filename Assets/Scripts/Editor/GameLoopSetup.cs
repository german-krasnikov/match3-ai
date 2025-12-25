#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.GameLoop;
using Match3.Grid;
using Match3.Board;
using Match3.Input;
using Match3.Swap;
using Match3.Matching;
using Match3.Destroy;
using Match3.Fall;
using Match3.Refill;

namespace Match3.Editor
{
    public static class GameLoopSetup
    {
        [MenuItem("Match3/Setup Scene/Stage 11 - Game Loop")]
        public static void SetupGameLoop()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found. Run Stage 1 setup first.");
                return;
            }

            var go = grid.gameObject;

            // Get required components
            var board = go.GetComponent<BoardComponent>();
            var inputBlocker = go.GetComponent<InputBlocker>();
            var swapHandler = go.GetComponent<SwapHandler>();
            var matchFinder = go.GetComponent<MatchFinder>();
            var destroyHandler = go.GetComponent<DestroyHandler>();
            var fallHandler = go.GetComponent<FallHandler>();
            var refillHandler = go.GetComponent<RefillHandler>();

            if (board == null || swapHandler == null || matchFinder == null ||
                destroyHandler == null || fallHandler == null || refillHandler == null)
            {
                Debug.LogError("[Match3] Missing required components. Run previous stages first.");
                return;
            }

            // BoardShuffler
            var shuffler = go.GetComponent<BoardShuffler>();
            if (shuffler == null)
                shuffler = Undo.AddComponent<BoardShuffler>(go);

            SetField(shuffler, "_board", board);
            SetField(shuffler, "_grid", grid);

            // GameLoopController
            var gameLoop = go.GetComponent<GameLoopController>();
            if (gameLoop == null)
                gameLoop = Undo.AddComponent<GameLoopController>(go);

            SetField(gameLoop, "_board", board);
            SetField(gameLoop, "_inputBlocker", inputBlocker);
            SetField(gameLoop, "_swapHandler", swapHandler);
            SetField(gameLoop, "_matchFinder", matchFinder);
            SetField(gameLoop, "_destroyHandler", destroyHandler);
            SetField(gameLoop, "_fallHandler", fallHandler);
            SetField(gameLoop, "_refillHandler", refillHandler);
            SetField(gameLoop, "_boardShuffler", shuffler);

            EditorUtility.SetDirty(go);
            Debug.Log("[Match3] Game Loop setup complete!");
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
