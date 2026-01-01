using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Match3.Grid;
using Match3.Input;
using Match3.Swap;

public static class SwapSetupEditor
{
    [MenuItem("Match3/Setup Swap System (Step 5)")]
    private static void SetupSwapSystem()
    {
        // 1. Ensure GridView exists
        GridView gridView = Object.FindObjectOfType<GridView>();
        if (gridView == null)
        {
            Debug.LogError("GridView not found. Run Match3/Setup Grid first.");
            return;
        }

        // 2. Find or create Systems GameObject
        GameObject systemsObj = GameObject.Find("Systems");
        if (systemsObj == null)
        {
            systemsObj = new GameObject("Systems");
            Undo.RegisterCreatedObjectUndo(systemsObj, "Create Systems");
            systemsObj.transform.position = Vector3.zero;
        }

        // 3. Add SwipeDetector if missing
        SwipeDetector swipeDetector = systemsObj.GetComponent<SwipeDetector>();
        if (swipeDetector == null)
        {
            swipeDetector = Undo.AddComponent<SwipeDetector>(systemsObj);
        }

        // 4. Configure SwipeDetector
        SerializedObject soSwipe = new SerializedObject(swipeDetector);
        soSwipe.FindProperty("_minSwipeDistance").floatValue = 0.3f;
        soSwipe.FindProperty("_gridView").objectReferenceValue = gridView;
        soSwipe.ApplyModifiedProperties();

        // 5. Add SwapAnimator if missing
        SwapAnimator swapAnimator = systemsObj.GetComponent<SwapAnimator>();
        if (swapAnimator == null)
        {
            swapAnimator = Undo.AddComponent<SwapAnimator>(systemsObj);
        }

        // 6. Configure SwapAnimator (defaults are fine, but set explicitly)
        SerializedObject soAnim = new SerializedObject(swapAnimator);
        soAnim.FindProperty("_swapDuration").floatValue = 0.2f;
        soAnim.FindProperty("_swapBackDuration").floatValue = 0.15f;
        soAnim.ApplyModifiedProperties();

        // 7. Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = systemsObj;
        Debug.Log("Swap System setup complete! SwipeDetector + SwapAnimator added to Systems.");
    }
}
