using System.IO;
using Match3.Data;
using Match3.Elements;
using Match3.Game;
using Match3.Grid;
using Match3.Match;
using Match3.Spawn;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    /// <summary>
    /// Editor utility to automatically setup Match3 scene.
    /// Menu: Match3 → Setup Scene
    /// </summary>
    public static class Match3SceneSetup
    {
        private const string DataPath = "Assets/Data";
        private const string PrefabsPath = "Assets/Prefabs";

        private static readonly Color[] ElementColors =
        {
            new Color(1f, 0.3f, 0.3f),    // Red
            new Color(0.3f, 0.5f, 1f),    // Blue
            new Color(0.3f, 1f, 0.3f),    // Green
            new Color(1f, 1f, 0.3f),      // Yellow
            new Color(0.8f, 0.3f, 1f)     // Purple
        };

        private static readonly string[] ElementNames = { "Red", "Blue", "Green", "Yellow", "Purple" };

        [MenuItem("Match3/Setup Scene", false, 1)]
        public static void SetupScene()
        {
            CreateDirectories();
            var sprite = CreateDefaultSprite();
            var elementTypes = CreateElementTypes(sprite);
            var gridConfig = CreateGridConfig(elementTypes);
            var elementPrefab = CreateElementPrefab();
            var cellPrefab = CreateCellPrefab();
            SetupSceneHierarchy(gridConfig, elementPrefab, cellPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Match3] Scene setup complete!");
        }

        [MenuItem("Match3/Create Data Only", false, 2)]
        public static void CreateDataOnly()
        {
            CreateDirectories();
            var sprite = CreateDefaultSprite();
            var elementTypes = CreateElementTypes(sprite);
            CreateGridConfig(elementTypes);
            CreateElementPrefab();
            CreateCellPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Match3] Data assets created!");
        }

        [MenuItem("Match3/Fix Element Sprites", false, 3)]
        public static void FixElementSprites()
        {
            CreateDirectories();
            var sprite = CreateDefaultSprite();
            CreateElementTypes(sprite);
            AssetDatabase.SaveAssets();
            Debug.Log("[Match3] Element sprites fixed!");
        }

        [MenuItem("Match3/Add Match System", false, 11)]
        public static void AddMatchSystem()
        {
            var existingMatch = Object.FindFirstObjectByType<MatchController>();
            if (existingMatch != null)
            {
                Debug.LogWarning("[Match3] MatchController already exists.");
                return;
            }

            var matchGO = new GameObject("MatchController");
            var matchController = matchGO.AddComponent<MatchController>();

            // Link to GameBootstrap if exists
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                var bootstrapSO = new SerializedObject(bootstrap);
                bootstrapSO.FindProperty("_matchController").objectReferenceValue = matchController;
                bootstrapSO.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeGameObject = matchGO;
            Debug.Log("[Match3] Match System added!");
        }

        [MenuItem("Match3/Add Spawn System", false, 10)]
        public static void AddSpawnSystem()
        {
            var gridView = Object.FindFirstObjectByType<GridView>();
            if (gridView == null)
            {
                Debug.LogError("[Match3] GridView not found. Run 'Setup Scene' first.");
                return;
            }

            var factory = Object.FindFirstObjectByType<ElementFactory>();
            if (factory == null)
            {
                Debug.LogError("[Match3] ElementFactory not found. Run 'Setup Scene' first.");
                return;
            }

            // Check if already exists
            var existingSpawn = Object.FindFirstObjectByType<SpawnController>();
            if (existingSpawn != null)
            {
                Debug.LogWarning("[Match3] SpawnController already exists.");
                return;
            }

            // Create SpawnController
            var spawnGO = new GameObject("SpawnController");
            var spawnController = spawnGO.AddComponent<SpawnController>();

            var spawnSO = new SerializedObject(spawnController);
            spawnSO.FindProperty("_gridView").objectReferenceValue = gridView;
            spawnSO.FindProperty("_factory").objectReferenceValue = factory;
            spawnSO.ApplyModifiedPropertiesWithoutUndo();

            // Create GameBootstrap
            var existingBootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (existingBootstrap == null)
            {
                var bootstrapGO = new GameObject("GameBootstrap");
                var bootstrap = bootstrapGO.AddComponent<GameBootstrap>();

                var bootstrapSO = new SerializedObject(bootstrap);
                bootstrapSO.FindProperty("_gridView").objectReferenceValue = gridView;
                bootstrapSO.FindProperty("_spawnController").objectReferenceValue = spawnController;
                bootstrapSO.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeGameObject = spawnGO;
            Debug.Log("[Match3] Spawn System added!");
        }

        private static void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Elements"))
                AssetDatabase.CreateFolder("Assets/Data", "Elements");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static ElementType[] CreateElementTypes(Sprite sprite)
        {
            var types = new ElementType[5];

            for (int i = 0; i < 5; i++)
            {
                string path = $"{DataPath}/Elements/{ElementNames[i]}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<ElementType>(path);
                if (existing != null)
                {
                    // Update sprite if missing
                    var existingSO = new SerializedObject(existing);
                    if (existingSO.FindProperty("_sprite").objectReferenceValue == null)
                    {
                        existingSO.FindProperty("_sprite").objectReferenceValue = sprite;
                        existingSO.ApplyModifiedPropertiesWithoutUndo();
                    }
                    types[i] = existing;
                    continue;
                }

                var elementType = ScriptableObject.CreateInstance<ElementType>();

                var so = new SerializedObject(elementType);
                so.FindProperty("_id").stringValue = ElementNames[i].ToLower();
                so.FindProperty("_sprite").objectReferenceValue = sprite;
                so.FindProperty("_color").colorValue = ElementColors[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                AssetDatabase.CreateAsset(elementType, path);
                types[i] = elementType;

                Debug.Log($"[Match3] Created ElementType: {ElementNames[i]}");
            }

            return types;
        }

        private static GridConfig CreateGridConfig(ElementType[] elementTypes)
        {
            string path = $"{DataPath}/GridConfig.asset";

            var existing = AssetDatabase.LoadAssetAtPath<GridConfig>(path);
            if (existing != null)
            {
                // Update element types if needed
                var so = new SerializedObject(existing);
                so.FindProperty("_elementTypes").arraySize = elementTypes.Length;
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    so.FindProperty("_elementTypes").GetArrayElementAtIndex(i).objectReferenceValue = elementTypes[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                return existing;
            }

            var config = ScriptableObject.CreateInstance<GridConfig>();

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_width").intValue = 8;
            serialized.FindProperty("_height").intValue = 8;
            serialized.FindProperty("_cellSize").floatValue = 1f;
            serialized.FindProperty("_swapDuration").floatValue = 0.2f;
            serialized.FindProperty("_fallSpeed").floatValue = 10f;
            serialized.FindProperty("_destroyDuration").floatValue = 0.15f;

            serialized.FindProperty("_elementTypes").arraySize = elementTypes.Length;
            for (int i = 0; i < elementTypes.Length; i++)
            {
                serialized.FindProperty("_elementTypes").GetArrayElementAtIndex(i).objectReferenceValue = elementTypes[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(config, path);
            Debug.Log("[Match3] Created GridConfig");

            return config;
        }

        private static GameObject CreateElementPrefab()
        {
            string path = $"{PrefabsPath}/Element.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = new GameObject("Element");

            // Add SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDefaultSprite();
            sr.sortingOrder = 1;

            // Add ElementView
            var view = go.AddComponent<ElementView>();

            // Link SpriteRenderer via SerializedObject
            var so = new SerializedObject(view);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.Log("[Match3] Created Element prefab");
            return prefab;
        }

        private static GameObject CreateCellPrefab()
        {
            string path = $"{PrefabsPath}/Cell.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = new GameObject("Cell");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDefaultSprite();
            sr.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            sr.sortingOrder = 0;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.Log("[Match3] Created Cell prefab");
            return prefab;
        }

        private static Sprite CreateDefaultSprite()
        {
            // Try to find existing
            string path = $"{PrefabsPath}/DefaultSquare.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            // Create a simple white square texture
            int size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Rounded square with border
                    float dx = Mathf.Abs(x - size / 2f) / (size / 2f);
                    float dy = Mathf.Abs(y - size / 2f) / (size / 2f);
                    float d = Mathf.Max(dx, dy);

                    if (d < 0.85f)
                        pixels[y * size + x] = Color.white;
                    else if (d < 0.95f)
                        pixels[y * size + x] = new Color(0.8f, 0.8f, 0.8f);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Save texture
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            // Configure texture import settings
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetupSceneHierarchy(GridConfig config, GameObject elementPrefab, GameObject cellPrefab)
        {
            // Clean up existing
            var existingGrid = Object.FindFirstObjectByType<GridView>();
            if (existingGrid != null)
            {
                Debug.Log("[Match3] GridView already exists, skipping scene setup");
                return;
            }

            // Create Grid
            var gridGO = new GameObject("Grid");
            var gridView = gridGO.AddComponent<GridView>();

            var gridSO = new SerializedObject(gridView);
            gridSO.FindProperty("_config").objectReferenceValue = config;
            gridSO.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab.GetComponent<SpriteRenderer>();

            // Create Cells parent
            var cellsParent = new GameObject("Cells");
            cellsParent.transform.SetParent(gridGO.transform);
            gridSO.FindProperty("_cellsParent").objectReferenceValue = cellsParent.transform;
            gridSO.ApplyModifiedPropertiesWithoutUndo();

            // Create ElementPool
            var poolGO = new GameObject("ElementPool");
            var pool = poolGO.AddComponent<ElementPool>();

            var poolSO = new SerializedObject(pool);
            poolSO.FindProperty("_prefab").objectReferenceValue = elementPrefab.GetComponent<ElementView>();
            poolSO.FindProperty("_initialSize").intValue = 64;
            poolSO.ApplyModifiedPropertiesWithoutUndo();

            // Create Elements parent
            var elementsParent = new GameObject("Elements");

            // Create ElementFactory
            var factoryGO = new GameObject("ElementFactory");
            var factory = factoryGO.AddComponent<ElementFactory>();

            var factorySO = new SerializedObject(factory);
            factorySO.FindProperty("_pool").objectReferenceValue = pool;
            factorySO.FindProperty("_elementsParent").objectReferenceValue = elementsParent.transform;
            factorySO.ApplyModifiedPropertiesWithoutUndo();

            // Create SpawnController
            var spawnGO = new GameObject("SpawnController");
            var spawnController = spawnGO.AddComponent<SpawnController>();

            var spawnSO = new SerializedObject(spawnController);
            spawnSO.FindProperty("_gridView").objectReferenceValue = gridView;
            spawnSO.FindProperty("_factory").objectReferenceValue = factory;
            spawnSO.ApplyModifiedPropertiesWithoutUndo();

            // Create MatchController
            var matchGO = new GameObject("MatchController");
            var matchController = matchGO.AddComponent<MatchController>();

            // Create GameBootstrap
            var bootstrapGO = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGO.AddComponent<GameBootstrap>();

            var bootstrapSO = new SerializedObject(bootstrap);
            bootstrapSO.FindProperty("_gridView").objectReferenceValue = gridView;
            bootstrapSO.FindProperty("_spawnController").objectReferenceValue = spawnController;
            bootstrapSO.FindProperty("_matchController").objectReferenceValue = matchController;
            bootstrapSO.ApplyModifiedPropertiesWithoutUndo();

            // Setup camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0, -10);
                cam.orthographic = true;
                cam.orthographicSize = 6;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            // Select grid for visibility
            Selection.activeGameObject = gridGO;

            Debug.Log("[Match3] Scene hierarchy created");
        }

        [MenuItem("Match3/Create Visual Grid", false, 20)]
        public static void CreateVisualGrid()
        {
            var gridView = Object.FindFirstObjectByType<GridView>();
            if (gridView == null)
            {
                Debug.LogError("[Match3] No GridView found in scene. Run Setup Scene first.");
                return;
            }

            gridView.CreateVisualGrid();
            Debug.Log("[Match3] Visual grid created");
        }
    }
}
