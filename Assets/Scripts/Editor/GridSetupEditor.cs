using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Match3.Grid;

public static class GridSetupEditor
{
    [MenuItem("Match3/Setup Grid")]
    private static void SetupGrid()
    {
        // 1. Create or find GridConfig asset
        GridConfig config = GetOrCreateGridConfig();
        if (config == null)
        {
            Debug.LogError("Failed to create GridConfig asset");
            return;
        }

        // 2. Find or create Grid GameObject
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj == null)
        {
            gridObj = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Grid GameObject");
            gridObj.transform.position = Vector3.zero;
        }

        // 3. Add GridView component if missing
        GridView gridView = gridObj.GetComponent<GridView>();
        if (gridView == null)
        {
            gridView = Undo.AddComponent<GridView>(gridObj);
        }

        // 4. Assign GridConfig to GridView
        SerializedObject so = new SerializedObject(gridView);
        SerializedProperty configProp = so.FindProperty("_config");
        configProp.objectReferenceValue = config;
        so.ApplyModifiedProperties();

        // 5. Create Cells parent Transform
        Transform cellsParent = gridObj.transform.Find("Cells");
        if (cellsParent == null)
        {
            GameObject cellsObj = new GameObject("Cells");
            Undo.RegisterCreatedObjectUndo(cellsObj, "Create Cells GameObject");
            Undo.SetTransformParent(cellsObj.transform, gridObj.transform, "Parent Cells to Grid");
            cellsParent = cellsObj.transform;
        }

        // Assign Cells parent to GridView
        SerializedProperty cellsParentProp = so.FindProperty("_cellsParent");
        cellsParentProp.objectReferenceValue = cellsParent;
        so.ApplyModifiedProperties();

        // 6. Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = gridObj;
        Debug.Log("Grid setup complete!");
    }

    private static GridConfig GetOrCreateGridConfig()
    {
        string assetPath = "Assets/ScriptableObjects/GridConfig.asset";

        // Try to load existing
        GridConfig config = AssetDatabase.LoadAssetAtPath<GridConfig>(assetPath);
        if (config != null)
        {
            return config;
        }

        // Create directory if needed
        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create new asset
        config = ScriptableObject.CreateInstance<GridConfig>();
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created GridConfig at {assetPath}");
        return config;
    }
}
