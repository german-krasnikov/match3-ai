// Assets/Scripts/Editor/Setup/Step2SceneSetup.cs
using UnityEngine;
using UnityEditor;
using Configs;

namespace Editor.Setup
{
    public static class Step2SceneSetup
    {
        private const string ConfigPath = "Assets/Configs/GameConfig.asset";
        private const string ConfigFolder = "Assets/Configs";

        [MenuItem("Setup/Step 2 - Spawn Service")]
        public static void Setup()
        {
            CreateConfigFolder();
            CreateOrUpdateGameConfig();

            Debug.Log("[Step 2] Scene setup complete. GameConfig created at: " + ConfigPath);
        }

        private static void CreateConfigFolder()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }
        }

        private static void CreateOrUpdateGameConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

            if (existing != null)
            {
                Debug.Log("[Step 2] GameConfig already exists. Skipping creation.");
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.GridWidth = 8;
            config.GridHeight = 8;
            config.ElementTypeCount = 5;
            config.MinMatchLength = 3;
            config.SwapDuration = 0.3f;
            config.FallDuration = 0.2f;
            config.DestroyDuration = 0.2f;
            config.SpawnDelay = 0.1f;

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Setup/Step 2 - Spawn Service", validate = true)]
        private static bool ValidateSetup()
        {
            return !EditorApplication.isPlaying;
        }
    }
}
