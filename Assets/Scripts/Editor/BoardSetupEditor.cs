using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Match3.Grid;
using Match3.Gem;
using Match3.Board;

public static class BoardSetupEditor
{
    [MenuItem("Match3/Setup Board")]
    private static void SetupBoard()
    {
        // 1. Ensure Grid exists
        GridView gridView = Object.FindObjectOfType<GridView>();
        if (gridView == null)
        {
            Debug.LogError("GridView not found in scene. Run Match3/Setup Grid first.");
            return;
        }

        // 2. Get or create GemConfig
        GemConfig gemConfig = GetOrCreateGemConfig();
        if (gemConfig == null)
        {
            Debug.LogError("Failed to create GemConfig asset");
            return;
        }

        // 3. Get or create Gem prefab
        GemView gemPrefab = GetOrCreateGemPrefab(gemConfig);
        if (gemPrefab == null)
        {
            Debug.LogError("Failed to create Gem prefab");
            return;
        }

        // 4. Find or create Board GameObject
        GameObject boardObj = GameObject.Find("Board");
        if (boardObj == null)
        {
            boardObj = new GameObject("Board");
            Undo.RegisterCreatedObjectUndo(boardObj, "Create Board GameObject");
            boardObj.transform.position = Vector3.zero;
        }

        // 5. Add BoardView component if missing
        BoardView boardView = boardObj.GetComponent<BoardView>();
        if (boardView == null)
        {
            boardView = Undo.AddComponent<BoardView>(boardObj);
        }

        // 6. Create Gems parent Transform
        Transform gemsParent = boardObj.transform.Find("Gems");
        if (gemsParent == null)
        {
            GameObject gemsObj = new GameObject("Gems");
            Undo.RegisterCreatedObjectUndo(gemsObj, "Create Gems GameObject");
            Undo.SetTransformParent(gemsObj.transform, boardObj.transform, "Parent Gems to Board");
            gemsParent = gemsObj.transform;
        }

        // 7. Assign references to BoardView
        SerializedObject so = new SerializedObject(boardView);
        so.FindProperty("_gridView").objectReferenceValue = gridView;
        so.FindProperty("_gemConfig").objectReferenceValue = gemConfig;
        so.FindProperty("_gemPrefab").objectReferenceValue = gemPrefab;
        so.FindProperty("_gemsParent").objectReferenceValue = gemsParent;
        so.ApplyModifiedProperties();

        // 8. Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = boardObj;
        Debug.Log("Board setup complete! Press Play to see gems spawn.");
    }

    private static GemConfig GetOrCreateGemConfig()
    {
        string assetPath = "Assets/ScriptableObjects/GemConfig.asset";

        GemConfig config = AssetDatabase.LoadAssetAtPath<GemConfig>(assetPath);
        if (config != null)
            return config;

        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        config = ScriptableObject.CreateInstance<GemConfig>();
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created GemConfig at {assetPath}");
        return config;
    }

    private static GemView GetOrCreateGemPrefab(GemConfig config)
    {
        string prefabPath = "Assets/Prefabs/Gem.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
            return existing.GetComponent<GemView>();

        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Create prefab
        GameObject gemObj = new GameObject("Gem");

        // Add SpriteRenderer
        SpriteRenderer sr = gemObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        // Add GemView
        gemObj.AddComponent<GemView>();

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gemObj, prefabPath);
        Object.DestroyImmediate(gemObj);

        Debug.Log($"Created Gem prefab at {prefabPath}");
        return prefab.GetComponent<GemView>();
    }
}
