using UnityEngine;
using UnityEditor;
using Match3.Core;
using Match3.Pieces;

namespace Match3.Editor
{
    public static class Step2_PiecesSetup
    {
        [MenuItem("Match3/Setup/Step 2 - Create Pieces Assets")]
        public static void Setup()
        {
            CreateFolders();
            CreateSortingLayers();
            var config = CreatePieceConfig();
            CreatePiecePrefab(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Step 2 Setup Complete!\n" +
                      "- PieceConfig.asset created\n" +
                      "- Piece.prefab created\n" +
                      "- Sorting Layers added");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
                AssetDatabase.CreateFolder("Assets", "Configs");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Pieces"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Pieces");
        }

        private static void CreateSortingLayers()
        {
            // Sorting Layers через SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var sortingLayers = tagManager.FindProperty("m_SortingLayers");

            string[] layersToAdd = { "Background", "Grid", "Pieces", "Effects", "UI" };

            foreach (var layerName in layersToAdd)
            {
                if (!SortingLayerExists(sortingLayers, layerName))
                {
                    AddSortingLayer(sortingLayers, layerName);
                }
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
            newLayer.FindPropertyRelative("uniqueID").intValue = (int)System.DateTime.Now.Ticks + layers.arraySize;
        }

        private static PieceConfig CreatePieceConfig()
        {
            const string path = "Assets/Configs/PieceConfig.asset";

            var existing = AssetDatabase.LoadAssetAtPath<PieceConfig>(path);
            if (existing != null)
            {
                Debug.Log("PieceConfig.asset already exists, skipping...");
                return existing;
            }

            var config = ScriptableObject.CreateInstance<PieceConfig>();

            // Используем reflection для заполнения приватного массива
            var piecesField = typeof(PieceConfig).GetField("_pieces",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var pieces = new PieceConfig.PieceVisualData[]
            {
                new() { Type = PieceType.Red,    Color = new Color(1f, 0.27f, 0.27f) },
                new() { Type = PieceType.Blue,   Color = new Color(0.27f, 0.5f, 1f) },
                new() { Type = PieceType.Green,  Color = new Color(0.27f, 0.9f, 0.27f) },
                new() { Type = PieceType.Yellow, Color = new Color(1f, 0.95f, 0.3f) },
                new() { Type = PieceType.Purple, Color = new Color(0.7f, 0.3f, 1f) },
                new() { Type = PieceType.Orange, Color = new Color(1f, 0.6f, 0.2f) }
            };

            piecesField?.SetValue(config, pieces);

            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static void CreatePiecePrefab(PieceConfig config)
        {
            const string path = "Assets/Prefabs/Pieces/Piece.prefab";

            // Удаляем старый если есть
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            // Создаём GameObject
            var go = new GameObject("Piece");

            // SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.sortingLayerName = "Pieces";

            // PieceView
            var view = go.AddComponent<PieceView>();
            var viewField = typeof(PieceView).GetField("_spriteRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            viewField?.SetValue(view, sr);

            // PieceComponent
            var piece = go.AddComponent<PieceComponent>();
            var pieceViewField = typeof(PieceComponent).GetField("_view",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pieceViewField?.SetValue(piece, view);

            // Сохраняем как префаб
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static Sprite GetDefaultSprite()
        {
            // Сначала пробуем наш спрайт
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/WhiteSquare.png");
            if (sprite != null) return sprite;

            // Fallback на встроенный
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        [MenuItem("Match3/Setup/Step 2 - Test Piece in Scene")]
        public static void TestPieceInScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pieces/Piece.prefab");
            var config = AssetDatabase.LoadAssetAtPath<PieceConfig>("Assets/Configs/PieceConfig.asset");

            if (prefab == null || config == null)
            {
                Debug.LogError("Run 'Step 2 - Create Pieces Assets' first!");
                return;
            }

            // Создаём 6 фишек в ряд для теста
            float startX = -2.5f;
            var types = new[] { PieceType.Red, PieceType.Blue, PieceType.Green,
                               PieceType.Yellow, PieceType.Purple, PieceType.Orange };

            for (int i = 0; i < types.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = $"Piece_{types[i]}";
                go.transform.position = new Vector3(startX + i, 0, 0);

                var piece = go.GetComponent<PieceComponent>();
                piece.Initialize(types[i], config);

                Undo.RegisterCreatedObjectUndo(go, "Create Test Piece");
            }

            Debug.Log("✅ Created 6 test pieces in scene");
        }
    }
}
