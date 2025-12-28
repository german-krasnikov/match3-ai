using UnityEngine;
using UnityEditor;
using Match3.Core;
using Match3.Grid;
using Match3.Elements;
using Match3.Spawn;
using Match3.Swap;
using Match3.Match;
using Match3.Destruction;
using Match3.Gravity;
using Match3.GameLoop;

namespace Match3.Editor
{
    /// <summary>
    /// Мастер-скрипт для создания полностью рабочей Match-3 сцены.
    /// </summary>
    public static class Match3SceneSetup
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Element.prefab";
        private const string CONFIG_PATH = "Assets/ScriptableObjects/ElementColors.asset";

        [MenuItem("Match3/Setup Full Scene", false, 1)]
        public static void SetupFullScene()
        {
            // Загружаем необходимые ассеты
            var elementPrefab = AssetDatabase.LoadAssetAtPath<ElementComponent>(PREFAB_PATH);
            var colorConfig = AssetDatabase.LoadAssetAtPath<ElementColorConfig>(CONFIG_PATH);

            if (elementPrefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab",
                    $"Element prefab not found at {PREFAB_PATH}\n\nPlease create it first.",
                    "OK");
                return;
            }

            if (colorConfig == null)
            {
                EditorUtility.DisplayDialog("Missing Config",
                    $"ElementColorConfig not found at {CONFIG_PATH}\n\nPlease create it first.",
                    "OK");
                return;
            }

            // Удаляем старые объекты если есть
            CleanupOldObjects();

            // === 1. GRID ===
            var gridGO = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridGO, "Create Grid");

            var grid = gridGO.AddComponent<GridComponent>();
            var factory = gridGO.AddComponent<ElementFactoryComponent>();
            var spawn = gridGO.AddComponent<SpawnComponent>();
            var matchDetection = gridGO.AddComponent<MatchDetectionComponent>();
            var swap = gridGO.AddComponent<SwapComponent>();
            var destruction = gridGO.AddComponent<DestructionComponent>();
            var gravity = gridGO.AddComponent<GravityComponent>();

            // Контейнер для элементов
            var elementsContainer = new GameObject("Elements");
            elementsContainer.transform.SetParent(gridGO.transform);

            // Настраиваем Factory
            var factorySO = new SerializedObject(factory);
            factorySO.FindProperty("_elementPrefab").objectReferenceValue = elementPrefab;
            factorySO.FindProperty("_colorConfig").objectReferenceValue = colorConfig;
            factorySO.FindProperty("_elementsParent").objectReferenceValue = elementsContainer.transform;
            factorySO.ApplyModifiedProperties();

            // Настраиваем Spawn
            var spawnSO = new SerializedObject(spawn);
            spawnSO.FindProperty("_gridComponent").objectReferenceValue = grid;
            spawnSO.FindProperty("_factoryComponent").objectReferenceValue = factory;
            spawnSO.ApplyModifiedProperties();

            // Настраиваем MatchDetection
            var matchSO = new SerializedObject(matchDetection);
            matchSO.FindProperty("_grid").objectReferenceValue = grid;
            matchSO.ApplyModifiedProperties();

            // Настраиваем Swap
            var swapSO = new SerializedObject(swap);
            swapSO.FindProperty("_grid").objectReferenceValue = grid;
            swapSO.ApplyModifiedProperties();

            // Настраиваем Destruction
            var destructionSO = new SerializedObject(destruction);
            destructionSO.FindProperty("_grid").objectReferenceValue = grid;
            destructionSO.ApplyModifiedProperties();

            // Настраиваем Gravity
            var gravitySO = new SerializedObject(gravity);
            gravitySO.FindProperty("_gridComponent").objectReferenceValue = grid;
            gravitySO.FindProperty("_spawnComponent").objectReferenceValue = spawn;
            gravitySO.ApplyModifiedProperties();

            // === 2. INPUT ===
            var inputGO = new GameObject("InputManager");
            Undo.RegisterCreatedObjectUndo(inputGO, "Create InputManager");

            var input = inputGO.AddComponent<InputComponent>();
            var mainCamera = Camera.main;

            if (mainCamera == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                mainCamera = camGO.AddComponent<Camera>();
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5;
                mainCamera.transform.position = new Vector3(4, 4, -10);
                Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
            }

            var inputSO = new SerializedObject(input);
            inputSO.FindProperty("_grid").objectReferenceValue = grid;
            inputSO.FindProperty("_camera").objectReferenceValue = mainCamera;
            inputSO.ApplyModifiedProperties();

            // === 3. GAME MANAGER ===
            var gameManagerGO = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gameManagerGO, "Create GameManager");

            var gameLoop = gameManagerGO.AddComponent<GameLoopComponent>();

            var gameLoopSO = new SerializedObject(gameLoop);
            gameLoopSO.FindProperty("_grid").objectReferenceValue = grid;
            gameLoopSO.FindProperty("_spawn").objectReferenceValue = spawn;
            gameLoopSO.FindProperty("_swap").objectReferenceValue = swap;
            gameLoopSO.FindProperty("_matchDetection").objectReferenceValue = matchDetection;
            gameLoopSO.FindProperty("_destruction").objectReferenceValue = destruction;
            gameLoopSO.FindProperty("_gravity").objectReferenceValue = gravity;
            gameLoopSO.FindProperty("_input").objectReferenceValue = input;
            gameLoopSO.ApplyModifiedProperties();

            // Настраиваем камеру для сетки 8x8
            SetupCamera(mainCamera);

            Selection.activeGameObject = gameManagerGO;

            Debug.Log("<color=green>[Match3] Scene setup complete!</color>\n" +
                      "Hierarchy:\n" +
                      "  - Grid (GridComponent, SpawnComponent, SwapComponent, etc.)\n" +
                      "  - InputManager (InputComponent)\n" +
                      "  - GameManager (GameLoopComponent)\n\n" +
                      "Press Play to start the game!");
        }

        [MenuItem("Match3/Clear Scene", false, 2)]
        public static void ClearScene()
        {
            CleanupOldObjects();
            Debug.Log("[Match3] Scene cleared.");
        }

        private static void CleanupOldObjects()
        {
            var names = new[] { "Grid", "InputManager", "GameManager" };

            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go != null)
                    Undo.DestroyObjectImmediate(go);
            }
        }

        private static void SetupCamera(Camera camera)
        {
            // Сетка 8x8 с cellSize=1, origin=(0,0)
            // Центр сетки: (4, 4)
            camera.transform.position = new Vector3(4f, 4f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        }
    }
}
