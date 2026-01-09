// Assets/Scripts/Editor/Setup/Step5SceneSetup.cs
using UnityEngine;
using UnityEditor;
using Configs;
using Features.Board;
using Features.Board.Views;

namespace Editor.Setup
{
    public static class Step5SceneSetup
    {
        private const string ConfigPath = "Assets/Configs/GameConfig.asset";

        [MenuItem("Setup/Step 5 - Input + Drag")]
        public static void Setup()
        {
            ClearPreviousSetup();

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[Step 5] GameConfig not found at " + ConfigPath);
                return;
            }

            CreateBoardWithInput(config);
            SetupCamera(config);

            Debug.Log("[Step 5] Scene setup complete.");
            Debug.Log("[Step 5] Enter Play mode and drag elements to test swap detection.");
        }

        private static void ClearPreviousSetup()
        {
            var existing = GameObject.Find("[Board]");
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        private static void CreateBoardWithInput(GameConfig config)
        {
            var boardGO = new GameObject("[Board]");
            var boardView = boardGO.AddComponent<BoardView>();
            boardView.SetConfig(config);

            var initializer = boardGO.AddComponent<Step5Initializer>();
            initializer.Config = config;

            EditorUtility.SetDirty(boardGO);
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

            float gridHeight = config.GridHeight * config.CellSize;
            float gridWidth = config.GridWidth * config.CellSize;
            float aspectRatio = (float)Screen.width / Screen.height;

            float verticalSize = gridHeight * 0.6f;
            float horizontalSize = (gridWidth * 0.6f) / aspectRatio;

            camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.3f);

            EditorUtility.SetDirty(camera.gameObject);
        }

        [MenuItem("Setup/Step 5 - Input + Drag", validate = true)]
        private static bool ValidateSetup()
        {
            return !EditorApplication.isPlaying;
        }
    }
}
