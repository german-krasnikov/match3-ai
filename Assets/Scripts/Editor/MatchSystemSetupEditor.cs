#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Core;
using Match3.Components.Board;
using Match3.Components.Animation;

namespace Match3.Editor
{
    public static class MatchSystemSetupEditor
    {
        [MenuItem("Match3/Setup Match System")]
        public static void SetupMatchSystem()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("GridComponent not found! Setup Board first.");
                return;
            }

            var parent = grid.transform.parent ?? grid.transform;

            // MatchDetector
            var detector = Object.FindFirstObjectByType<MatchDetector>();
            if (detector == null)
            {
                var detectorGo = new GameObject("MatchDetector");
                detectorGo.transform.SetParent(parent);
                detector = detectorGo.AddComponent<MatchDetector>();
                SetPrivateField(detector, "_grid", grid);
                Debug.Log("Created MatchDetector");
            }

            // DestroyAnimator
            var destroyAnimator = Object.FindFirstObjectByType<DestroyAnimator>();
            if (destroyAnimator == null)
            {
                var animatorGo = new GameObject("DestroyAnimator");
                animatorGo.transform.SetParent(parent);
                destroyAnimator = animatorGo.AddComponent<DestroyAnimator>();
                Debug.Log("Created DestroyAnimator");
            }

            // DestroyHandler
            var destroyHandler = Object.FindFirstObjectByType<DestroyHandler>();
            if (destroyHandler == null)
            {
                var handlerGo = new GameObject("DestroyHandler");
                handlerGo.transform.SetParent(parent);
                destroyHandler = handlerGo.AddComponent<DestroyHandler>();
                SetPrivateField(destroyHandler, "_grid", grid);
                SetPrivateField(destroyHandler, "_animator", destroyAnimator);
                Debug.Log("Created DestroyHandler");
            }

            // ScoreCalculator
            var scoreCalc = Object.FindFirstObjectByType<ScoreCalculator>();
            if (scoreCalc == null)
            {
                var scoreGo = new GameObject("ScoreCalculator");
                scoreGo.transform.SetParent(parent);
                scoreCalc = scoreGo.AddComponent<ScoreCalculator>();
                Debug.Log("Created ScoreCalculator");
            }

            // MatchController
            var matchController = Object.FindFirstObjectByType<MatchController>();
            if (matchController == null)
            {
                var controllerGo = new GameObject("MatchController");
                controllerGo.transform.SetParent(parent);
                matchController = controllerGo.AddComponent<MatchController>();
                SetPrivateField(matchController, "_detector", detector);
                SetPrivateField(matchController, "_destroyHandler", destroyHandler);
                SetPrivateField(matchController, "_scoreCalculator", scoreCalc);
                Debug.Log("Created MatchController");
            }

            // Update SwapValidator
            var swapValidator = Object.FindFirstObjectByType<SwapValidator>();
            if (swapValidator != null)
            {
                SetPrivateField(swapValidator, "_matchDetector", detector);
                Debug.Log("Updated SwapValidator with MatchDetector");
            }

            // Update BoardInputHandler
            var inputHandler = Object.FindFirstObjectByType<BoardInputHandler>();
            if (inputHandler != null)
            {
                SetPrivateField(inputHandler, "_matchController", matchController);
                Debug.Log("Updated BoardInputHandler with MatchController");
            }

            EditorUtility.SetDirty(parent.gameObject);
            Debug.Log("Match System setup complete!");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
        }
    }
}
#endif
