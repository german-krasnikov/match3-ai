using UnityEngine;
using UnityEditor;
using System.IO;

public static class Match3SceneSetup
{
    private const string ConfigPath = "Assets/Configs/GridConfig.asset";
    private const string ConfigFolder = "Assets/Configs";

    [MenuItem("Match3/Setup Scene %#m")]
    public static void SetupScene()
    {
        var config = GetOrCreateConfig();
        var board = CreateBoard(config);
        SetupCamera(config);

        Selection.activeGameObject = board;
        Debug.Log("Match3 scene setup complete!");
    }

    [MenuItem("Match3/Create GridConfig Only")]
    public static void CreateConfigOnly()
    {
        var config = GetOrCreateConfig();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    private static GridConfig GetOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<GridConfig>(ConfigPath);

        if (config == null)
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }

            config = ScriptableObject.CreateInstance<GridConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created GridConfig at {ConfigPath}");
        }

        return config;
    }

    private static GameObject CreateBoard(GridConfig config)
    {
        var existing = GameObject.Find("Board");
        if (existing != null)
        {
            Debug.Log("Board already exists, updating config");
            var grid = existing.GetComponent<GridComponent>();
            if (grid == null)
                grid = existing.AddComponent<GridComponent>();

            SetGridConfig(grid, config);
            return existing;
        }

        var board = new GameObject("Board");
        var gridComponent = board.AddComponent<GridComponent>();
        SetGridConfig(gridComponent, config);

        Undo.RegisterCreatedObjectUndo(board, "Create Board");
        Debug.Log("Created Board GameObject");

        return board;
    }

    private static void SetGridConfig(GridComponent grid, GridConfig config)
    {
        var so = new SerializedObject(grid);
        var configProp = so.FindProperty("_config");
        configProp.objectReferenceValue = config;
        so.ApplyModifiedProperties();
    }

    private static void SetupCamera(GridConfig config)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No Main Camera found");
            return;
        }

        float centerX = (config.Width - 1) * config.CellSize / 2f + config.OriginOffset.x;
        float centerY = (config.Height - 1) * config.CellSize / 2f + config.OriginOffset.y;

        cam.transform.position = new Vector3(centerX, centerY, -10f);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(config.Width, config.Height) * config.CellSize / 2f + 1f;

        Undo.RecordObject(cam.transform, "Setup Camera");
        Undo.RecordObject(cam, "Setup Camera");

        Debug.Log($"Camera positioned at ({centerX}, {centerY}) with size {cam.orthographicSize}");
    }
}
