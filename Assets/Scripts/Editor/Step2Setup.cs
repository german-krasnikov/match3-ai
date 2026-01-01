using UnityEngine;
using UnityEditor;
using System.IO;
using Match3.Gem;

public static class Step2Setup
{
    [MenuItem("Match3/Setup Step 2 - Gem System")]
    public static void Setup()
    {
        CreateFolders();
        CreateGemPrefab();
        CreateGemConfig();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Step 2 Setup Complete!");
    }

    private static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
    }

    private static void CreateGemPrefab()
    {
        const string path = "Assets/Prefabs/Gem.prefab";
        if (File.Exists(path))
        {
            Debug.Log("Gem.prefab already exists, skipping");
            return;
        }

        var go = new GameObject("Gem");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        go.AddComponent<GemView>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log("Created Gem.prefab");
    }

    private static void CreateGemConfig()
    {
        const string path = "Assets/ScriptableObjects/GemConfig.asset";
        if (File.Exists(path))
        {
            Debug.Log("GemConfig.asset already exists, skipping");
            return;
        }

        var config = ScriptableObject.CreateInstance<GemConfig>();
        AssetDatabase.CreateAsset(config, path);

        Debug.Log("Created GemConfig.asset (assign sprites manually)");
    }
}
