using UnityEngine;
using UnityEditor;
using Match3.Game;
using Match3.Grid;
using Match3.Board;
using Match3.Gem;
using Match3.Input;
using Match3.Swap;
using Match3.Destroy;
using Match3.Fall;

namespace Match3.Editor
{
    /// <summary>
    /// Editor utility to set up Step 8: GameController with all dependencies.
    /// </summary>
    public static class GameSetupEditor
    {
        [MenuItem("Match3/Setup Step 8 - Game Controller")]
        public static void SetupGameController()
        {
            // Find existing components
            var gridView = Object.FindFirstObjectByType<GridView>();
            var boardView = Object.FindFirstObjectByType<BoardView>();
            var swipeDetector = Object.FindFirstObjectByType<SwipeDetector>();
            var swapAnimator = Object.FindFirstObjectByType<SwapAnimator>();
            var destroyAnimator = Object.FindFirstObjectByType<DestroyAnimator>();
            var fallAnimator = Object.FindFirstObjectByType<FallAnimator>();

            // Validate all required components exist
            if (gridView == null)
            {
                Debug.LogError("GridView not found! Run Match3/Setup Step 1 first.");
                return;
            }
            if (boardView == null)
            {
                Debug.LogError("BoardView not found! Run Match3/Setup Step 3 first.");
                return;
            }
            if (swipeDetector == null)
            {
                Debug.LogError("SwipeDetector not found! Run Match3/Setup Step 5 first.");
                return;
            }
            if (swapAnimator == null)
            {
                Debug.LogError("SwapAnimator not found! Run Match3/Setup Step 5 first.");
                return;
            }
            if (destroyAnimator == null)
            {
                Debug.LogError("DestroyAnimator not found! Run Match3/Setup Step 7 first.");
                return;
            }
            if (fallAnimator == null)
            {
                Debug.LogError("FallAnimator not found! Run Match3/Setup Step 4 first.");
                return;
            }

            // Find GemConfig
            var gemConfig = FindGemConfig();
            if (gemConfig == null)
            {
                Debug.LogError("GemConfig not found! Create it first.");
                return;
            }

            // Find or create GameController
            var gameController = Object.FindFirstObjectByType<GameController>();
            if (gameController == null)
            {
                var go = new GameObject("GameController");
                gameController = go.AddComponent<GameController>();
                Undo.RegisterCreatedObjectUndo(go, "Create GameController");
            }

            // Wire up references via SerializedObject
            var so = new SerializedObject(gameController);

            so.FindProperty("_gridView").objectReferenceValue = gridView;
            so.FindProperty("_boardView").objectReferenceValue = boardView;
            so.FindProperty("_gemConfig").objectReferenceValue = gemConfig;
            so.FindProperty("_swipeDetector").objectReferenceValue = swipeDetector;
            so.FindProperty("_swapAnimator").objectReferenceValue = swapAnimator;
            so.FindProperty("_destroyAnimator").objectReferenceValue = destroyAnimator;
            so.FindProperty("_fallAnimator").objectReferenceValue = fallAnimator;

            so.ApplyModifiedProperties();

            // Select GameController
            Selection.activeGameObject = gameController.gameObject;

            Debug.Log("✓ Step 8: GameController created and configured!\n" +
                      "  - GridView: assigned\n" +
                      "  - BoardView: assigned\n" +
                      "  - GemConfig: assigned\n" +
                      "  - SwipeDetector: assigned\n" +
                      "  - SwapAnimator: assigned\n" +
                      "  - DestroyAnimator: assigned\n" +
                      "  - FallAnimator: assigned\n\n" +
                      "Game loop ready! Press Play to test.");
        }

        private static GemConfig FindGemConfig()
        {
            // Search in Assets
            var guids = AssetDatabase.FindAssets("t:GemConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GemConfig>(path);
            }
            return null;
        }

        [MenuItem("Match3/Setup Step 8 - Game Controller", true)]
        public static bool ValidateSetupGameController()
        {
            // Only enable in edit mode
            return !Application.isPlaying;
        }
    }
}
