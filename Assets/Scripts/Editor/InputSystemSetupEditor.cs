using UnityEngine;
using UnityEditor;
using Match3.Components.Board;
using Match3.Components.Visual;
using Match3.Components.Animation;
using Match3.Core;
using Match3.Input;

namespace Match3.Editor
{
    public static class InputSystemSetupEditor
    {
        private const string PrefabPath = "Assets/Prefabs/";

        [MenuItem("Match3/Setup Input System", false, 2)]
        public static void SetupInputSystem()
        {
            var grid = Object.FindObjectOfType<GridComponent>();
            if (grid == null)
            {
                Debug.LogError("[Match3] GridComponent not found! Run 'Setup Board Scene' first.");
                return;
            }

            var board = grid.transform.parent?.gameObject ?? grid.gameObject;

            CreateFolders();
            var selectionPrefab = CreateSelectionHighlightPrefab();
            CreateInputSystemHierarchy(board, grid, selectionPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("[Match3] Input System setup complete!");
        }

        [MenuItem("Match3/Create Selection Highlight Prefab", false, 22)]
        public static void CreateSelectionHighlightPrefabMenu()
        {
            CreateFolders();
            CreateSelectionHighlightPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Match3] SelectionHighlight prefab created!");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static GameObject CreateSelectionHighlightPrefab()
        {
            var path = $"{PrefabPath}SelectionHighlight.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("SelectionHighlight");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadOrCreateHighlightSprite();
            sr.color = new Color(1f, 1f, 1f, 0.6f);
            sr.sortingOrder = 10;

            var selection = go.AddComponent<SelectionVisualComponent>();
            SetPrivateField(selection, "_renderer", sr);
            SetPrivateField(selection, "_pulseScale", 1.15f);
            SetPrivateField(selection, "_pulseDuration", 0.3f);

            go.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            return prefab;
        }

        private static void CreateInputSystemHierarchy(GameObject board, GridComponent grid, GameObject selectionPrefab)
        {
            // Clean up existing
            var existingInput = board.transform.Find("InputSystem");
            if (existingInput != null)
                Object.DestroyImmediate(existingInput.gameObject);

            // InputSystem container
            var inputSystem = new GameObject("InputSystem");
            inputSystem.transform.SetParent(board.transform);

            // SelectionHighlight instance
            var selectionInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectionPrefab);
            selectionInstance.name = "SelectionHighlight";
            selectionInstance.transform.SetParent(inputSystem.transform);
            selectionInstance.SetActive(false);
            var selectionVisual = selectionInstance.GetComponent<SelectionVisualComponent>();

            // Add components
            var inputController = inputSystem.AddComponent<InputController>();
            var swapValidator = inputSystem.AddComponent<SwapValidator>();
            var swapAnimator = inputSystem.AddComponent<SwapAnimator>();
            var swapController = inputSystem.AddComponent<SwapController>();
            var boardInputHandler = inputSystem.AddComponent<BoardInputHandler>();

            // Add AudioSource
            var audioSource = inputSystem.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Get camera
            var mainCamera = Camera.main;

            // Wire up InputController
            SetPrivateField(inputController, "_camera", mainCamera);
            SetPrivateField(inputController, "_grid", grid);
            SetPrivateField(inputController, "_swipeThreshold", 0.3f);

            // Wire up SwapValidator
            SetPrivateField(swapValidator, "_grid", grid);

            // Wire up SwapAnimator
            SetPrivateField(swapAnimator, "_swapDuration", 0.2f);
            SetPrivateField(swapAnimator, "_revertDuration", 0.15f);

            // Wire up SwapController
            SetPrivateField(swapController, "_grid", grid);
            SetPrivateField(swapController, "_validator", swapValidator);
            SetPrivateField(swapController, "_animator", swapAnimator);

            // Wire up BoardInputHandler
            SetPrivateField(boardInputHandler, "_inputController", inputController);
            SetPrivateField(boardInputHandler, "_swapController", swapController);
            SetPrivateField(boardInputHandler, "_selectionVisual", selectionVisual);
            SetPrivateField(boardInputHandler, "_grid", grid);
            SetPrivateField(boardInputHandler, "_audioSource", audioSource);

            Selection.activeGameObject = inputSystem;
            EditorUtility.SetDirty(inputSystem);
            EditorUtility.SetDirty(board);
        }

        private static Sprite LoadOrCreateHighlightSprite()
        {
            // Try to use existing square sprite
            var squarePath = "Assets/Data/Tiles/Square.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            if (existing != null) return existing;

            // Create highlight sprite
            var path = "Assets/Data/Tiles/Highlight.png";
            existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Tiles"))
                AssetDatabase.CreateFolder("Assets/Data", "Tiles");

            int size = 64;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int borderWidth = 4;
                    bool border = x < borderWidth || x >= size - borderWidth ||
                                  y < borderWidth || y >= size - borderWidth;
                    pixels[y * size + x] = border ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            var bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
                field.SetValue(obj, value);
            else
                Debug.LogWarning($"[Match3] Field {fieldName} not found on {obj.GetType().Name}");
        }
    }
}
