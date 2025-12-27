using UnityEngine;
using UnityEditor;
using Match3.Grid;

namespace Match3.Editor
{
    public static class Step1_GridSetup
    {
        [MenuItem("Match3/Setup/Step 1 - Create Grid Assets")]
        public static void Setup()
        {
            CreateFolders();
            CreateSortingLayers();
            var config = CreateGridConfig();
            var cellPrefab = CreateCellPrefab();
            CreateGridPrefab(config, cellPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Step 1 Setup Complete!\n" +
                      "- GridConfig.asset created\n" +
                      "- Cell.prefab created\n" +
                      "- Grid.prefab created");
        }

        [MenuItem("Match3/Setup/Step 1 - Create Grid in Scene")]
        public static void CreateGridInScene()
        {
            var gridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid.prefab");

            if (gridPrefab == null)
            {
                Debug.LogError("Run 'Step 1 - Create Grid Assets' first!");
                return;
            }

            // Удаляем старую сетку если есть
            var existingGrid = Object.FindFirstObjectByType<GridComponent>();
            if (existingGrid != null)
            {
                Undo.DestroyObjectImmediate(existingGrid.gameObject);
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab);
            go.transform.position = Vector3.zero;

            var grid = go.GetComponent<GridComponent>();
            grid.Initialize();

            Undo.RegisterCreatedObjectUndo(go, "Create Grid");

            // Центрируем камеру
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0, -10);
                cam.orthographicSize = 6;
            }

            Selection.activeGameObject = go;
            Debug.Log("✅ Grid created in scene (8x8)");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
                AssetDatabase.CreateFolder("Assets", "Configs");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static void CreateSortingLayers()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var sortingLayers = tagManager.FindProperty("m_SortingLayers");

            string[] layersToAdd = { "Background", "Grid", "Pieces", "Effects", "UI" };

            foreach (var layerName in layersToAdd)
            {
                if (!SortingLayerExists(sortingLayers, layerName))
                    AddSortingLayer(sortingLayers, layerName);
            }

            tagManager.ApplyModifiedProperties();
        }

        private static bool SortingLayerExists(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return true;
            }
            return false;
        }

        private static void AddSortingLayer(SerializedProperty layers, string name)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var newLayer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = name;
            newLayer.FindPropertyRelative("uniqueID").intValue =
                (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF) + layers.arraySize;
        }

        private static GridConfig CreateGridConfig()
        {
            const string path = "Assets/Configs/GridConfig.asset";

            var existing = AssetDatabase.LoadAssetAtPath<GridConfig>(path);
            if (existing != null) return existing;

            var config = ScriptableObject.CreateInstance<GridConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static GameObject CreateCellPrefab()
        {
            const string path = "Assets/Prefabs/Cell.prefab";

            // Удаляем старый если есть
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var go = new GameObject("Cell");

            // SpriteRenderer с квадратным спрайтом
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSquareSprite();
            sr.sortingLayerName = "Grid";
            sr.color = new Color(0.9f, 0.9f, 0.9f);
            sr.drawMode = SpriteDrawMode.Simple;

            // CellComponent
            var cell = go.AddComponent<CellComponent>();
            var field = typeof(CellComponent).GetField("_spriteRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(cell, sr);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite CreateWhiteSquareSprite()
        {
            const string path = "Assets/Sprites/WhiteSquare.png";

            // Создаём папку
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                AssetDatabase.CreateFolder("Assets", "Sprites");

            // Проверяем существующий
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            // Создаём белую текстуру 64x64
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            // Сохраняем как PNG
            var bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);

            // Настраиваем import settings
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64; // 1 unit = 64 pixels
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateGridPrefab(GridConfig config, GameObject cellPrefab)
        {
            const string path = "Assets/Prefabs/Grid.prefab";

            // Удаляем старый если есть
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var go = new GameObject("Grid");
            var grid = go.AddComponent<GridComponent>();

            // Связываем через reflection
            var configField = typeof(GridComponent).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField?.SetValue(grid, config);

            var cellField = typeof(GridComponent).GetField("_cellPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cellField?.SetValue(grid, cellPrefab.GetComponent<CellComponent>());

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        [MenuItem("Match3/Setup/Step 1+2 - Full Scene Setup")]
        public static void FullSceneSetup()
        {
            // Step 1
            Setup();

            // Step 2
            Step2_PiecesSetup.Setup();

            // Create grid in scene
            CreateGridInScene();

            // Create test pieces on grid
            CreateTestPiecesOnGrid();

            Debug.Log("✅ Full scene setup complete!");
        }

        private static void CreateTestPiecesOnGrid()
        {
            var grid = Object.FindFirstObjectByType<GridComponent>();
            var piecePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pieces/Piece.prefab");
            var pieceConfig = AssetDatabase.LoadAssetAtPath<Pieces.PieceConfig>("Assets/Configs/PieceConfig.asset");

            if (grid == null || piecePrefab == null || pieceConfig == null)
            {
                Debug.LogWarning("Missing assets for test pieces");
                return;
            }

            var types = new[] {
                Core.PieceType.Red, Core.PieceType.Blue, Core.PieceType.Green,
                Core.PieceType.Yellow, Core.PieceType.Purple, Core.PieceType.Orange
            };

            // Создаём несколько фишек для теста
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new Core.GridPosition(x, y);
                    var worldPos = grid.GridToWorld(pos);

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(piecePrefab);
                    go.transform.position = worldPos;
                    go.transform.SetParent(grid.transform);

                    var piece = go.GetComponent<Pieces.PieceComponent>();
                    var type = types[(x + y) % types.Length];
                    piece.Initialize(type, pieceConfig);
                    piece.Position = pos;

                    Undo.RegisterCreatedObjectUndo(go, "Create Test Piece");
                }
            }
        }
    }
}
