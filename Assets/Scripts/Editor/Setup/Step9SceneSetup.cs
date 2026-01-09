// Assets/Scripts/Editor/Setup/Step9SceneSetup.cs
using UnityEngine;
using UnityEditor;
using Configs;
using Features.Board.Views;
using Features.Board;

namespace Editor.Setup
{
    public static class Step9SceneSetup
    {
        private const string ConfigPath = "Assets/Configs/GameConfig.asset";

        [MenuItem("Setup/Step 9 - Refill + Cascade")]
        public static void Setup()
        {
            var existing = GameObject.Find("[Board]");
            if (existing != null) Object.DestroyImmediate(existing);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[Step 9] GameConfig not found at " + ConfigPath);
                return;
            }

            var boardGO = new GameObject("[Board]");
            var boardView = boardGO.AddComponent<BoardView>();
            boardView.SetConfig(config);
            boardGO.AddComponent<Step9Initializer>();

            SetupCamera(config);

            EditorUtility.SetDirty(boardGO);
            Debug.Log("[Step 9] Setup complete. Enter Play mode to test cascade.");
        }

        private static void SetupCamera(GameConfig config)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraGO = new GameObject("Main Camera");
                camera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
            }

            camera.orthographic = true;
            float size = Mathf.Max(config.GridWidth, config.GridHeight) * 0.6f;
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
        }

        [MenuItem("Setup/Step 9 - Refill + Cascade", validate = true)]
        private static bool Validate() => !EditorApplication.isPlaying;
    }
}
