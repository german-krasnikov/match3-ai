using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Match3.Grid;
using Match3.Gem;
using Match3.Board;
using Match3.Input;
using Match3.Swap;
using Match3.Fall;
using Match3.Destroy;
using Match3.Game;

namespace Match3.Editor
{
    /// <summary>
    /// Sets up the entire Match3 scene from scratch.
    /// Creates all GameObjects, components, and ScriptableObjects.
    /// </summary>
    public static class FullSceneSetup
    {
        [MenuItem("Match3/Setup Complete Scene (All Steps)")]
        public static void SetupCompleteScene()
        {
            Debug.Log("=== Starting Full Scene Setup ===");

            // Step 1: Grid
            var gridConfig = CreateGridConfig();
            var gridView = CreateGrid(gridConfig);

            // Step 2+3: Board (includes GemConfig, GemPrefab)
            var gemConfig = CreateGemConfig();
            var gemPrefab = CreateGemPrefab();
            var boardView = CreateBoard(gridView, gemConfig, gemPrefab);

            // Step 4+5+7: Systems GameObject with all animators
            var systemsObj = CreateSystems(gridView);
            var fallAnimator = systemsObj.GetComponent<FallAnimator>();
            var swipeDetector = systemsObj.GetComponent<SwipeDetector>();
            var swapAnimator = systemsObj.GetComponent<SwapAnimator>();
            var destroyAnimator = systemsObj.GetComponent<DestroyAnimator>();

            // Step 8: GameController
            CreateGameController(gridView, boardView, gemConfig,
                swipeDetector, swapAnimator, destroyAnimator, fallAnimator);

            // Setup Camera
            SetupCamera(gridConfig);

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("=== Full Scene Setup Complete! Press Play to test. ===");
        }

        // --- Step 1: Grid ---

        private static GridConfig CreateGridConfig()
        {
            string path = "Assets/ScriptableObjects/GridConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GridConfig>(path);

            if (config == null)
            {
                EnsureDirectory(path);
                config = ScriptableObject.CreateInstance<GridConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                Debug.Log("[Step1] Created GridConfig");
            }
            return config;
        }

        private static GridView CreateGrid(GridConfig config)
        {
            var gridObj = FindOrCreate("Grid");
            var gridView = gridObj.GetComponent<GridView>();
            if (gridView == null)
                gridView = Undo.AddComponent<GridView>(gridObj);

            // Create Cells parent
            var cellsParent = FindOrCreateChild(gridObj, "Cells");

            // Assign references
            var so = new SerializedObject(gridView);
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_cellsParent").objectReferenceValue = cellsParent;
            so.ApplyModifiedProperties();

            Debug.Log("[Step1] Grid setup complete");
            return gridView;
        }

        // --- Step 2+3: Board ---

        private static GemConfig CreateGemConfig()
        {
            string path = "Assets/ScriptableObjects/GemConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GemConfig>(path);

            if (config == null)
            {
                EnsureDirectory(path);
                config = ScriptableObject.CreateInstance<GemConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }

            // Setup default colors
            var so = new SerializedObject(config);
            var gemsProp = so.FindProperty("_gems");

            if (gemsProp.arraySize == 0)
            {
                Color[] colors = {
                    new Color(1f, 0.2f, 0.2f),    // Red
                    new Color(0.2f, 0.4f, 1f),    // Blue
                    new Color(0.2f, 0.9f, 0.3f),  // Green
                    new Color(1f, 0.9f, 0.2f),    // Yellow
                    new Color(0.7f, 0.3f, 0.9f),  // Purple
                    new Color(1f, 0.5f, 0.1f)     // Orange
                };

                gemsProp.arraySize = 6;
                for (int i = 0; i < 6; i++)
                {
                    var element = gemsProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Type").enumValueIndex = i;
                    element.FindPropertyRelative("Color").colorValue = colors[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
            }

            Debug.Log("[Step2] GemConfig ready");
            return config;
        }

        private static GemView CreateGemPrefab()
        {
            string path = "Assets/Prefabs/Gem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existing != null)
            {
                var view = existing.GetComponent<GemView>();
                if (view != null)
                {
                    Debug.Log("[Step2] Using existing Gem prefab");
                    return view;
                }
            }

            // Create prefab
            EnsureDirectory(path);

            var gemObj = new GameObject("Gem");
            var sr = gemObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.sortingOrder = 1;

            var gemView = gemObj.AddComponent<GemView>();

            // Assign SpriteRenderer reference
            var soGem = new SerializedObject(gemView);
            soGem.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            soGem.ApplyModifiedProperties();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(gemObj, path);
            Object.DestroyImmediate(gemObj);

            Debug.Log("[Step2] Created Gem prefab");
            return prefab.GetComponent<GemView>();
        }

        private static Sprite CreateCircleSprite()
        {
            // Try to find existing circle sprite
            var guids = AssetDatabase.FindAssets("t:Sprite Circle");
            foreach (var guid in guids)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sp != null) return sp;
            }

            // Create a simple white texture
            string texPath = "Assets/Sprites/GemCircle.png";
            EnsureDirectory(texPath);

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = dist < radius ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();

            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(texPath, bytes);
            AssetDatabase.Refresh();

            // Import as sprite
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        }

        private static BoardView CreateBoard(GridView gridView, GemConfig gemConfig, GemView gemPrefab)
        {
            var boardObj = FindOrCreate("Board");
            var boardView = boardObj.GetComponent<BoardView>();
            if (boardView == null)
                boardView = Undo.AddComponent<BoardView>(boardObj);

            var gemsParent = FindOrCreateChild(boardObj, "Gems");

            var so = new SerializedObject(boardView);
            so.FindProperty("_gridView").objectReferenceValue = gridView;
            so.FindProperty("_gemConfig").objectReferenceValue = gemConfig;
            so.FindProperty("_gemPrefab").objectReferenceValue = gemPrefab;
            so.FindProperty("_gemsParent").objectReferenceValue = gemsParent;
            so.ApplyModifiedProperties();

            Debug.Log("[Step3] Board setup complete");
            return boardView;
        }

        // --- Step 4+5+7: Systems ---

        private static GameObject CreateSystems(GridView gridView)
        {
            var systemsObj = FindOrCreate("Systems");

            // FallAnimator (Step 4)
            var fallAnimator = systemsObj.GetComponent<FallAnimator>();
            if (fallAnimator == null)
                fallAnimator = Undo.AddComponent<FallAnimator>(systemsObj);

            var soFall = new SerializedObject(fallAnimator);
            soFall.FindProperty("_gridView").objectReferenceValue = gridView;
            soFall.FindProperty("_fallSpeed").floatValue = 8f;
            soFall.FindProperty("_minDuration").floatValue = 0.1f;
            soFall.ApplyModifiedProperties();
            Debug.Log("[Step4] FallAnimator ready");

            // SwipeDetector (Step 5)
            var swipeDetector = systemsObj.GetComponent<SwipeDetector>();
            if (swipeDetector == null)
                swipeDetector = Undo.AddComponent<SwipeDetector>(systemsObj);

            var soSwipe = new SerializedObject(swipeDetector);
            soSwipe.FindProperty("_minSwipeDistance").floatValue = 0.3f;
            soSwipe.FindProperty("_gridView").objectReferenceValue = gridView;
            soSwipe.ApplyModifiedProperties();

            // SwapAnimator (Step 5)
            var swapAnimator = systemsObj.GetComponent<SwapAnimator>();
            if (swapAnimator == null)
                swapAnimator = Undo.AddComponent<SwapAnimator>(systemsObj);

            var soSwap = new SerializedObject(swapAnimator);
            soSwap.FindProperty("_swapDuration").floatValue = 0.2f;
            soSwap.FindProperty("_swapBackDuration").floatValue = 0.15f;
            soSwap.ApplyModifiedProperties();
            Debug.Log("[Step5] Swap system ready");

            // DestroyAnimator (Step 7)
            var destroyAnimator = systemsObj.GetComponent<DestroyAnimator>();
            if (destroyAnimator == null)
                destroyAnimator = Undo.AddComponent<DestroyAnimator>(systemsObj);
            Debug.Log("[Step7] DestroyAnimator ready");

            return systemsObj;
        }

        // --- Step 8: GameController ---

        private static void CreateGameController(
            GridView gridView, BoardView boardView, GemConfig gemConfig,
            SwipeDetector swipeDetector, SwapAnimator swapAnimator,
            DestroyAnimator destroyAnimator, FallAnimator fallAnimator)
        {
            var gcObj = FindOrCreate("GameController");
            var gc = gcObj.GetComponent<GameController>();
            if (gc == null)
                gc = Undo.AddComponent<GameController>(gcObj);

            var so = new SerializedObject(gc);
            so.FindProperty("_gridView").objectReferenceValue = gridView;
            so.FindProperty("_boardView").objectReferenceValue = boardView;
            so.FindProperty("_gemConfig").objectReferenceValue = gemConfig;
            so.FindProperty("_swipeDetector").objectReferenceValue = swipeDetector;
            so.FindProperty("_swapAnimator").objectReferenceValue = swapAnimator;
            so.FindProperty("_destroyAnimator").objectReferenceValue = destroyAnimator;
            so.FindProperty("_fallAnimator").objectReferenceValue = fallAnimator;
            so.ApplyModifiedProperties();

            Debug.Log("[Step8] GameController ready");
        }

        // --- Camera Setup ---

        private static void SetupCamera(GridConfig config)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            // Center camera on grid
            // Grid is 8x8, cell size 1.0, origin at 0,0
            // Center should be at (3.5, 3.5) for 8x8 grid
            var so = new SerializedObject(config);
            int width = so.FindProperty("_width").intValue;
            int height = so.FindProperty("_height").intValue;
            float cellSize = so.FindProperty("_cellSize").floatValue;

            if (width == 0) width = 8;
            if (height == 0) height = 8;
            if (cellSize == 0) cellSize = 1f;

            float centerX = (width - 1) * cellSize / 2f;
            float centerY = (height - 1) * cellSize / 2f;

            cam.transform.position = new Vector3(centerX, centerY, -10f);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(width, height) * cellSize / 2f + 1f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

            Debug.Log("[Camera] Centered on grid");
        }

        // --- Helpers ---

        private static GameObject FindOrCreate(string name)
        {
            var obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
                obj.transform.position = Vector3.zero;
            }
            return obj;
        }

        private static Transform FindOrCreateChild(GameObject parent, string name)
        {
            var child = parent.transform.Find(name);
            if (child == null)
            {
                var childObj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(childObj, $"Create {name}");
                childObj.transform.SetParent(parent.transform);
                childObj.transform.localPosition = Vector3.zero;
                child = childObj.transform;
            }
            return child;
        }

        private static void EnsureDirectory(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        [MenuItem("Match3/Setup Complete Scene (All Steps)", true)]
        public static bool ValidateSetup()
        {
            return !Application.isPlaying;
        }
    }
}
