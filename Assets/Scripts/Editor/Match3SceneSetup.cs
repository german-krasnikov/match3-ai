using UnityEngine;
using UnityEditor;
using System.IO;

public static class Match3SceneSetup
{
    private const string ConfigPath = "Assets/Configs/GridConfig.asset";
    private const string ElementConfigPath = "Assets/Configs/ElementConfig.asset";
    private const string SwapConfigPath = "Assets/Configs/SwapConfig.asset";
    private const string ElementPrefabPath = "Assets/Prefabs/Element.prefab";
    private const string ConfigFolder = "Assets/Configs";

    [MenuItem("Match3/Setup Scene %#m")]
    public static void SetupScene()
    {
        var config = GetOrCreateConfig();
        var board = CreateBoard(config);
        SetupCamera(config);

        Selection.activeGameObject = board;
        Debug.Log("Match3 scene setup complete!");
    }

    [MenuItem("Match3/Create GridConfig Only")]
    public static void CreateConfigOnly()
    {
        var config = GetOrCreateConfig();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    [MenuItem("Match3/Setup Elements (Stage 2)")]
    public static void SetupElements()
    {
        var prefab = GetOrCreateElementPrefab();
        var config = GetOrCreateElementConfig(prefab);
        var factory = CreateElementFactory(config);

        Selection.activeGameObject = factory;
        Debug.Log("Elements setup complete! Use Match3/Test Elements to spawn test elements.");
    }

    [MenuItem("Match3/Setup Match Detection (Stage 4)")]
    public static void SetupMatchDetection()
    {
        var grid = Object.FindFirstObjectByType<GridComponent>();
        if (grid == null)
        {
            Debug.LogError("GridComponent not found. Run Match3/Setup Scene first.");
            return;
        }

        var board = grid.gameObject;

        var detector = board.GetComponent<MatchDetectorComponent>();
        if (detector == null)
        {
            detector = board.AddComponent<MatchDetectorComponent>();
            Undo.RegisterCreatedObjectUndo(detector, "Add MatchDetectorComponent");
        }

        var so = new SerializedObject(detector);
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = board;
        Debug.Log("Match Detection setup complete! Use ContextMenu on MatchDetectorComponent to test.");
    }

    [MenuItem("Match3/Setup Input Swap (Stage 5)")]
    public static void SetupInputSwap()
    {
        var grid = Object.FindFirstObjectByType<GridComponent>();
        if (grid == null)
        {
            Debug.LogError("GridComponent not found. Run Match3/Setup Scene first.");
            return;
        }

        var board = grid.gameObject;
        var swapConfig = GetOrCreateSwapConfig();

        // SwapAnimationComponent
        var swapAnim = board.GetComponent<SwapAnimationComponent>();
        if (swapAnim == null)
        {
            swapAnim = board.AddComponent<SwapAnimationComponent>();
            Undo.RegisterCreatedObjectUndo(swapAnim, "Add SwapAnimationComponent");
        }
        SetSwapAnimationRefs(swapAnim, swapConfig, grid);

        // SwapComponent
        var swap = board.GetComponent<SwapComponent>();
        if (swap == null)
        {
            swap = board.AddComponent<SwapComponent>();
            Undo.RegisterCreatedObjectUndo(swap, "Add SwapComponent");
        }
        SetSwapComponentRefs(swap, swapAnim, grid);

        // InputComponent
        var input = board.GetComponent<InputComponent>();
        if (input == null)
        {
            input = board.AddComponent<InputComponent>();
            Undo.RegisterCreatedObjectUndo(input, "Add InputComponent");
        }
        SetInputComponentRefs(input, grid, swapConfig, swap);

        EditorUtility.SetDirty(board);
        Selection.activeGameObject = board;
        Debug.Log("Input & Swap setup complete! Press Play and swipe elements.");
    }

    private static SwapConfig GetOrCreateSwapConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SwapConfig>(SwapConfigPath);
        if (config != null) return config;

        if (!AssetDatabase.IsValidFolder(ConfigFolder))
            AssetDatabase.CreateFolder("Assets", "Configs");

        config = ScriptableObject.CreateInstance<SwapConfig>();
        AssetDatabase.CreateAsset(config, SwapConfigPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created SwapConfig at {SwapConfigPath}");
        return config;
    }

    private static void SetSwapAnimationRefs(SwapAnimationComponent anim, SwapConfig config, GridComponent grid)
    {
        var so = new SerializedObject(anim);
        so.FindProperty("_config").objectReferenceValue = config;
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.ApplyModifiedProperties();
    }

    private static void SetSwapComponentRefs(SwapComponent swap, SwapAnimationComponent anim, GridComponent grid)
    {
        var so = new SerializedObject(swap);
        so.FindProperty("_animation").objectReferenceValue = anim;
        so.FindProperty("_debugGrid").objectReferenceValue = grid;
        so.ApplyModifiedProperties();
    }

    private static void SetInputComponentRefs(InputComponent input, GridComponent grid, SwapConfig config, SwapComponent swap)
    {
        var so = new SerializedObject(input);
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.FindProperty("_config").objectReferenceValue = config;
        so.FindProperty("_swap").objectReferenceValue = swap;
        so.FindProperty("_camera").objectReferenceValue = Camera.main;
        so.ApplyModifiedProperties();
    }

    [MenuItem("Match3/Setup Spawn (Stage 3)")]
    public static void SetupSpawn()
    {
        var grid = Object.FindFirstObjectByType<GridComponent>();
        var factory = Object.FindFirstObjectByType<ElementFactory>();

        if (grid == null)
        {
            Debug.LogError("GridComponent not found. Run Match3/Setup Scene first.");
            return;
        }
        if (factory == null)
        {
            Debug.LogError("ElementFactory not found. Run Match3/Setup Elements first.");
            return;
        }

        var board = grid.gameObject;

        // SpawnComponent
        var spawner = board.GetComponent<SpawnComponent>();
        if (spawner == null)
        {
            spawner = board.AddComponent<SpawnComponent>();
            Undo.RegisterCreatedObjectUndo(spawner, "Add SpawnComponent");
        }
        SetSpawnComponentRefs(spawner, grid, factory);

        // BoardInitializer
        var initializer = board.GetComponent<BoardInitializer>();
        if (initializer == null)
        {
            initializer = board.AddComponent<BoardInitializer>();
            Undo.RegisterCreatedObjectUndo(initializer, "Add BoardInitializer");
        }
        SetBoardInitializerRefs(initializer, grid, spawner);

        Selection.activeGameObject = board;
        Debug.Log("Spawn setup complete! Press Play to fill the board.");
    }

    private static void SetSpawnComponentRefs(SpawnComponent spawner, GridComponent grid, ElementFactory factory)
    {
        var so = new SerializedObject(spawner);
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.FindProperty("_factory").objectReferenceValue = factory;
        so.ApplyModifiedProperties();
    }

    private static void SetBoardInitializerRefs(BoardInitializer initializer, GridComponent grid, SpawnComponent spawner)
    {
        var so = new SerializedObject(initializer);
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.FindProperty("_spawner").objectReferenceValue = spawner;
        so.ApplyModifiedProperties();
    }

    [MenuItem("Match3/Test Elements")]
    public static void TestElements()
    {
        var factory = Object.FindFirstObjectByType<ElementFactory>();
        var grid = Object.FindFirstObjectByType<GridComponent>();

        if (factory == null)
        {
            Debug.LogError("ElementFactory not found. Run Match3/Setup Elements first.");
            return;
        }
        if (grid == null)
        {
            Debug.LogError("GridComponent not found. Run Match3/Setup Scene first.");
            return;
        }

        // Spawn one element of each type in a row
        for (int i = 0; i < 5; i++)
        {
            var type = (ElementType)i;
            var pos = grid.GridToWorld(i + 1, grid.Height / 2);
            var element = factory.Create(type, pos, i + 1, grid.Height / 2);
            Undo.RegisterCreatedObjectUndo(element.gameObject, "Test Element");
        }

        Debug.Log("Created 5 test elements (one of each type)");
    }

    private static ElementComponent GetOrCreateElementPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ElementComponent>(ElementPrefabPath);
        if (existing != null)
        {
            Debug.Log("Element prefab already exists");
            return existing;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var go = new GameObject("Element");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var element = go.AddComponent<ElementComponent>();
        SetSpriteRenderer(element, sr);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, ElementPrefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"Created Element prefab at {ElementPrefabPath}");
        return prefab.GetComponent<ElementComponent>();
    }

    private static ElementConfig GetOrCreateElementConfig(ElementComponent prefab)
    {
        var existing = AssetDatabase.LoadAssetAtPath<ElementConfig>(ElementConfigPath);
        if (existing != null)
        {
            Debug.Log("ElementConfig already exists");
            return existing;
        }

        var config = ScriptableObject.CreateInstance<ElementConfig>();

        var so = new SerializedObject(config);
        so.FindProperty("_defaultSprite").objectReferenceValue = GetDefaultSprite();
        so.FindProperty("_prefab").objectReferenceValue = prefab;

        var colors = so.FindProperty("_colors");
        colors.arraySize = 5;
        colors.GetArrayElementAtIndex(0).colorValue = Color.red;
        colors.GetArrayElementAtIndex(1).colorValue = Color.blue;
        colors.GetArrayElementAtIndex(2).colorValue = Color.green;
        colors.GetArrayElementAtIndex(3).colorValue = Color.yellow;
        colors.GetArrayElementAtIndex(4).colorValue = new Color(0.6f, 0.2f, 0.8f);
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(config, ElementConfigPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created ElementConfig at {ElementConfigPath}");
        return config;
    }

    private static GameObject CreateElementFactory(ElementConfig config)
    {
        var existing = GameObject.Find("ElementFactory");
        if (existing != null)
        {
            Debug.Log("ElementFactory already exists, updating config");
            var factory = existing.GetComponent<ElementFactory>();
            if (factory == null)
                factory = existing.AddComponent<ElementFactory>();
            SetElementFactoryConfig(factory, config);
            return existing;
        }

        var factoryGO = new GameObject("ElementFactory");
        var factoryComp = factoryGO.AddComponent<ElementFactory>();

        var container = new GameObject("Elements");
        container.transform.SetParent(factoryGO.transform);

        SetElementFactoryConfig(factoryComp, config);
        SetElementFactoryContainer(factoryComp, container.transform);

        Undo.RegisterCreatedObjectUndo(factoryGO, "Create ElementFactory");
        Debug.Log("Created ElementFactory GameObject");

        return factoryGO;
    }

    private static Sprite GetDefaultSprite()
    {
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (knob != null) return knob;

        var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in allSprites)
        {
            if (s.name == "Knob" || s.name == "UISprite") return s;
        }

        return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }

    private static void SetSpriteRenderer(ElementComponent element, SpriteRenderer sr)
    {
        var so = new SerializedObject(element);
        so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedProperties();
    }

    private static void SetElementFactoryConfig(ElementFactory factory, ElementConfig config)
    {
        var so = new SerializedObject(factory);
        so.FindProperty("_config").objectReferenceValue = config;
        so.ApplyModifiedProperties();
    }

    private static void SetElementFactoryContainer(ElementFactory factory, Transform container)
    {
        var so = new SerializedObject(factory);
        so.FindProperty("_container").objectReferenceValue = container;
        so.ApplyModifiedProperties();
    }

    private static GridConfig GetOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<GridConfig>(ConfigPath);

        if (config == null)
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }

            config = ScriptableObject.CreateInstance<GridConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created GridConfig at {ConfigPath}");
        }

        return config;
    }

    private static GameObject CreateBoard(GridConfig config)
    {
        var existing = GameObject.Find("Board");
        if (existing != null)
        {
            Debug.Log("Board already exists, updating config");
            var grid = existing.GetComponent<GridComponent>();
            if (grid == null)
                grid = existing.AddComponent<GridComponent>();

            SetGridConfig(grid, config);
            return existing;
        }

        var board = new GameObject("Board");
        var gridComponent = board.AddComponent<GridComponent>();
        SetGridConfig(gridComponent, config);

        Undo.RegisterCreatedObjectUndo(board, "Create Board");
        Debug.Log("Created Board GameObject");

        return board;
    }

    private static void SetGridConfig(GridComponent grid, GridConfig config)
    {
        var so = new SerializedObject(grid);
        var configProp = so.FindProperty("_config");
        configProp.objectReferenceValue = config;
        so.ApplyModifiedProperties();
    }

    private static void SetupCamera(GridConfig config)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No Main Camera found");
            return;
        }

        float centerX = (config.Width - 1) * config.CellSize / 2f + config.OriginOffset.x;
        float centerY = (config.Height - 1) * config.CellSize / 2f + config.OriginOffset.y;

        cam.transform.position = new Vector3(centerX, centerY, -10f);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(config.Width, config.Height) * config.CellSize / 2f + 1f;

        Undo.RecordObject(cam.transform, "Setup Camera");
        Undo.RecordObject(cam, "Setup Camera");

        Debug.Log($"Camera positioned at ({centerX}, {centerY}) with size {cam.orthographicSize}");
    }
}
