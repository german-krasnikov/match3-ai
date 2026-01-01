using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Match3.Grid;
using Match3.Fall;

public static class FallSetupEditor
{
    [MenuItem("Match3/Setup Fall System")]
    private static void SetupFallSystem()
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

        // 3. Add FallAnimator if missing
        FallAnimator fallAnimator = systemsObj.GetComponent<FallAnimator>();
        if (fallAnimator == null)
        {
            fallAnimator = Undo.AddComponent<FallAnimator>(systemsObj);
        }

        // 4. Assign GridView reference
        SerializedObject so = new SerializedObject(fallAnimator);
        so.FindProperty("_gridView").objectReferenceValue = gridView;
        so.FindProperty("_fallSpeed").floatValue = 8f;
        so.FindProperty("_minDuration").floatValue = 0.1f;
        so.ApplyModifiedProperties();

        // 5. Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = systemsObj;
        Debug.Log("Fall System setup complete! FallAnimator added to Systems.");
    }
}
