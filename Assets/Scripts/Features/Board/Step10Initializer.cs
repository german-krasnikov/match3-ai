using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Configs;
using Features.Board.Views;
using Core;
#endif

namespace Features.Board
{
    public static class Step10Initializer
    {
#if UNITY_EDITOR
        private const string ConfigPath = "Assets/Configs/GameConfig.asset";

        [MenuItem("Setup/Step 10 - Full Game Loop")]
        public static void Setup()
        {
            ClearPreviousSetup();

            // Load or create config
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[Step10] GameConfig not found at {ConfigPath}. Run Step 2 setup first.");
                return;
            }

            // Create main camera if needed
            if (Camera.main == null)
            {
                var cameraGO = new GameObject("Main Camera");
                var camera = cameraGO.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0, 0, -10);
                cameraGO.tag = "MainCamera";
            }

            // Create game root
            var gameRoot = new GameObject("[Match3Game]");

            // Create Board View
            var boardGO = new GameObject("Board");
            boardGO.transform.SetParent(gameRoot.transform);
            var boardView = boardGO.AddComponent<BoardView>();
            boardView.SetConfig(config);

            // Create Entry Point
            var entryPointGO = new GameObject("GameEntryPoint");
            entryPointGO.transform.SetParent(gameRoot.transform);
            var entryPoint = entryPointGO.AddComponent<GameEntryPoint>();

            // Assign references via SerializedObject
            var so = new SerializedObject(entryPoint);
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_boardView").objectReferenceValue = boardView;
            so.ApplyModifiedProperties();

            // Select the entry point
            Selection.activeGameObject = entryPointGO;

            // Mark scene dirty
            EditorUtility.SetDirty(gameRoot);

            Debug.Log("[Step10] Scene setup complete. Press Play to test full game loop!");
            Debug.Log("[Step10] Drag elements to swap. Valid swaps create matches which destroy, fall, and refill.");
        }

        private static void ClearPreviousSetup()
        {
            // Remove old game root
            var existing = GameObject.Find("[Match3Game]");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            // Also clear any old step initializers
            var oldRoot = GameObject.Find("[Board]");
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            var stepInitializers = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in stepInitializers)
            {
                if (mb != null && mb.GetType().Name.Contains("Step") && mb.GetType().Name.Contains("Initializer"))
                {
                    Object.DestroyImmediate(mb.gameObject);
                }
            }
        }
#endif
    }
}
