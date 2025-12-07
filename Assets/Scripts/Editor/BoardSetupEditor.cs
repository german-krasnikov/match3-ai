using UnityEngine;
using UnityEditor;
using Match3.Data;
using Match3.Components.Board;
using Match3.Core;

namespace Match3.Editor
{
    public static class BoardSetupEditor
    {
        private const string DataPath = "Assets/Data/Tiles/";
        private const string PrefabPath = "Assets/Prefabs/";

        [MenuItem("Match3/Setup Board Scene", false, 1)]
        public static void SetupBoardScene()
        {
            CreateFolders();
            var tileDataAssets = CreateTileDataAssets();
            var cellPrefab = CreateCellPrefab();
            var tilePrefab = CreateTilePrefab();
            CreateBoardHierarchy(cellPrefab, tilePrefab, tileDataAssets);
            SetupCamera();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Match3] Board scene setup complete!");
        }

        [MenuItem("Match3/Create Tile Data Assets", false, 20)]
        public static void CreateTileDataAssetsMenu()
        {
            CreateFolders();
            CreateTileDataAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("[Match3] TileData assets created!");
        }

        [MenuItem("Match3/Create Prefabs", false, 21)]
        public static void CreatePrefabsMenu()
        {
            CreateFolders();
            CreateCellPrefab();
            CreateTilePrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Match3] Prefabs created!");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Tiles"))
                AssetDatabase.CreateFolder("Assets/Data", "Tiles");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static TileData[] CreateTileDataAssets()
        {
            var tilesConfig = new (TileType type, Color color)[]
            {
                (TileType.Red, new Color(0.9f, 0.2f, 0.2f)),
                (TileType.Blue, new Color(0.2f, 0.4f, 0.9f)),
                (TileType.Green, new Color(0.2f, 0.8f, 0.3f)),
                (TileType.Yellow, new Color(0.95f, 0.85f, 0.2f)),
                (TileType.Purple, new Color(0.7f, 0.3f, 0.9f)),
                (TileType.Orange, new Color(1f, 0.5f, 0.1f))
            };

            var assets = new TileData[tilesConfig.Length];

            for (int i = 0; i < tilesConfig.Length; i++)
            {
                var (type, color) = tilesConfig[i];
                var path = $"{DataPath}TileData_{type}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<TileData>(path);
                if (existing != null)
                {
                    assets[i] = existing;
                    continue;
                }

                var asset = ScriptableObject.CreateInstance<TileData>();
                asset.type = type;
                asset.color = color;

                AssetDatabase.CreateAsset(asset, path);
                assets[i] = asset;
            }

            return assets;
        }

        private static GameObject CreateCellPrefab()
        {
            var path = $"{PrefabPath}Cell.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Cell");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.9f, 0.9f, 0.9f, 0.5f);
            sr.sortingOrder = 0;

            var cell = go.AddComponent<CellComponent>();
            SetPrivateField(cell, "_backgroundRenderer", sr);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            return prefab;
        }

        private static GameObject CreateTilePrefab()
        {
            var path = $"{PrefabPath}Tile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Tile");
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = Color.white;
            sr.sortingOrder = 1;

            var tile = go.AddComponent<TileComponent>();
            SetPrivateField(tile, "_spriteRenderer", sr);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            return prefab;
        }

        private static void CreateBoardHierarchy(GameObject cellPrefab, GameObject tilePrefab, TileData[] tileData)
        {
            // Clean up existing
            var existingBoard = GameObject.Find("Board");
            if (existingBoard != null)
                Object.DestroyImmediate(existingBoard);

            // Board root
            var board = new GameObject("Board");

            // Grid
            var grid = new GameObject("Grid");
            grid.transform.SetParent(board.transform);

            var gridComponent = grid.AddComponent<GridComponent>();
            SetPrivateField(gridComponent, "_width", 8);
            SetPrivateField(gridComponent, "_height", 8);
            SetPrivateField(gridComponent, "_cellSize", 1f);
            SetPrivateField(gridComponent, "_originOffset", new Vector3(-3.5f, -3.5f, 0f));
            SetPrivateField(gridComponent, "_cellPrefab", cellPrefab.GetComponent<CellComponent>());

            // Tiles container
            var tiles = new GameObject("Tiles");
            tiles.transform.SetParent(board.transform);

            // TileSpawner
            var spawner = board.AddComponent<TileSpawner>();
            SetPrivateField(spawner, "_tilePrefab", tilePrefab.GetComponent<TileComponent>());
            SetPrivateField(spawner, "_grid", gridComponent);
            SetPrivateField(spawner, "_tileContainer", tiles.transform);
            SetPrivateField(spawner, "_availableTiles", tileData);

            // BoardController
            var controller = board.AddComponent<BoardController>();
            SetPrivateField(controller, "_grid", gridComponent);
            SetPrivateField(controller, "_spawner", spawner);

            Selection.activeGameObject = board;
            EditorUtility.SetDirty(board);
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

            EditorUtility.SetDirty(cam);
        }

        private static Sprite CreateSquareSprite()
        {
            var path = "Assets/Data/Tiles/Square.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            int size = 64;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                    pixels[y * size + x] = border ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
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

        private static Sprite CreateCircleSprite()
        {
            var path = "Assets/Data/Tiles/Circle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            int size = 64;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
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
                Debug.LogWarning($"Field {fieldName} not found on {obj.GetType().Name}");
        }
    }
}
