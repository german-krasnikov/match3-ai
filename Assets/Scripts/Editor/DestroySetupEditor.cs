using UnityEngine;
using UnityEditor;
using Match3.Board;
using Match3.Destroy;

namespace Match3.Editor
{
    public class DestroySetupEditor : EditorWindow
    {
        [MenuItem("Match3/Setup Step 7 - Destroy System")]
        private static void Setup()
        {
            var boardView = Object.FindObjectOfType<BoardView>();
            if (boardView == null)
            {
                EditorUtility.DisplayDialog("Error", "No BoardView found in scene!", "OK");
                return;
            }

            var animator = boardView.GetComponent<DestroyAnimator>();
            if (animator == null)
            {
                animator = boardView.gameObject.AddComponent<DestroyAnimator>();
                EditorUtility.SetDirty(boardView.gameObject);
                Debug.Log($"[Step7] Added DestroyAnimator to {boardView.name}");
            }
            else
            {
                Debug.Log($"[Step7] DestroyAnimator already exists on {boardView.name}");
            }

            Selection.activeGameObject = boardView.gameObject;
            EditorUtility.DisplayDialog("Step 7 Setup",
                "DestroyAnimator added to BoardView.\n\nDefault settings:\n- Scale Duration: 0.2s\n- Cascade Delay: 0.05s\n- Ease: InBack",
                "OK");
        }
    }
}
