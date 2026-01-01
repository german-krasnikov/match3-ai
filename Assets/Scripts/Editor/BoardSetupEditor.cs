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
        {
            SetupDefaultColors(config);
            return config;
        }

        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        config = ScriptableObject.CreateInstance<GemConfig>();
        AssetDatabase.CreateAsset(config, assetPath);
        SetupDefaultColors(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created GemConfig at {assetPath}");
        return config;
    }

    private static void SetupDefaultColors(GemConfig config)
    {
        var so = new SerializedObject(config);
        var gemsProp = so.FindProperty("_gems");

        // Default colors for gem types
        Color[] colors = {
            new Color(1f, 0.2f, 0.2f),    // Red
            new Color(0.2f, 0.4f, 1f),    // Blue
            new Color(0.2f, 0.9f, 0.3f),  // Green
            new Color(1f, 0.9f, 0.2f),    // Yellow
            new Color(0.7f, 0.3f, 0.9f),  // Purple
            new Color(1f, 0.5f, 0.1f)     // Orange
        };

        // Only setup if empty or colors are default (black/clear)
        if (gemsProp.arraySize == 0)
        {
            gemsProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var element = gemsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").enumValueIndex = i;
                element.FindPropertyRelative("Color").colorValue = colors[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            Debug.Log("GemConfig: Set default colors");
        }
        else
        {
            // Update colors if they're black/clear
            bool needsUpdate = false;
            for (int i = 0; i < gemsProp.arraySize && i < colors.Length; i++)
            {
                var element = gemsProp.GetArrayElementAtIndex(i);
                var colorProp = element.FindPropertyRelative("Color");
                if (colorProp.colorValue == Color.black || colorProp.colorValue.a == 0)
                {
                    colorProp.colorValue = colors[i];
                    needsUpdate = true;
                }
            }
            if (needsUpdate)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                Debug.Log("GemConfig: Updated colors");
            }
        }
    }

    private static GemView GetOrCreateGemPrefab(GemConfig config)
    {
        // Try multiple paths
        string[] paths = {
            "Assets/Prefabs/Gem.prefab",
            "Assets/Prefabs/Gems/Gem.prefab"
        };

        foreach (var path in paths)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                var view = existing.GetComponent<GemView>();
                if (view != null)
                {
                    Debug.Log($"Using existing Gem prefab at {path}");
                    return view;
                }
            }
        }

        Debug.LogError("Gem.prefab not found! Create it manually with GemView + SpriteRenderer.");
        return null;
    }
}
