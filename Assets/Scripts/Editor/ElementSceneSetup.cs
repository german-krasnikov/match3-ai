using UnityEngine;
using UnityEditor;
using Match3.Core;
using Match3.Elements;
using System.IO;

namespace Match3.Editor
{
    public static class ElementSceneSetup
    {
        private const string ScriptableObjectsPath = "Assets/ScriptableObjects";
        private const string PrefabsPath = "Assets/Prefabs";
        private const string ColorConfigPath = "Assets/ScriptableObjects/ElementColors.asset";
        private const string ElementPrefabPath = "Assets/Prefabs/Element.prefab";

        [MenuItem("Match3/Setup Step 3 - Elements")]
        public static void SetupElementsScene()
        {
            CreateFolders();
            var colorConfig = CreateOrGetColorConfig();
            var prefab = CreateOrGetElementPrefab();
            CreateElementFactory(colorConfig, prefab);

            Debug.Log("Step 3 Elements setup complete.");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder(ScriptableObjectsPath))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static ElementColorConfig CreateOrGetColorConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ElementColorConfig>(ColorConfigPath);
            if (config != null)
            {
                Debug.Log("ElementColorConfig already exists.");
                return config;
            }

            config = ScriptableObject.CreateInstance<ElementColorConfig>();

            // Set colors via SerializedObject
            AssetDatabase.CreateAsset(config, ColorConfigPath);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(config);
            var colorsProp = so.FindProperty("_colors");
            colorsProp.arraySize = 5;

            SetColor(colorsProp.GetArrayElementAtIndex(0), ElementType.Red, new Color(1f, 0.27f, 0.27f));
            SetColor(colorsProp.GetArrayElementAtIndex(1), ElementType.Green, new Color(0.27f, 1f, 0.27f));
            SetColor(colorsProp.GetArrayElementAtIndex(2), ElementType.Blue, new Color(0.27f, 0.27f, 1f));
            SetColor(colorsProp.GetArrayElementAtIndex(3), ElementType.Yellow, new Color(1f, 1f, 0.27f));
            SetColor(colorsProp.GetArrayElementAtIndex(4), ElementType.Purple, new Color(0.67f, 0.27f, 1f));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("Created ElementColorConfig with 5 colors.");
            return config;
        }

        private static void SetColor(SerializedProperty element, ElementType type, Color color)
        {
            element.FindPropertyRelative("type").enumValueIndex = (int)type;
            element.FindPropertyRelative("color").colorValue = color;
        }

        private static ElementComponent CreateOrGetElementPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ElementPrefabPath);
            if (existing != null)
            {
                Debug.Log("Element prefab already exists.");
                return existing.GetComponent<ElementComponent>();
            }

            // Create GameObject
            var go = new GameObject("Element");

            // Add SpriteRenderer with white square
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSquareSprite();
            sr.color = Color.white;

            // Add ElementComponent
            var element = go.AddComponent<ElementComponent>();

            // Link SpriteRenderer via SerializedObject
            var so = new SerializedObject(element);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ElementPrefabPath);
            Object.DestroyImmediate(go);

            Debug.Log("Created Element prefab.");
            return prefab.GetComponent<ElementComponent>();
        }

        private static Sprite CreateWhiteSquareSprite()
        {
            // Try to find existing
            var spritePath = "Assets/Sprites/WhiteSquare.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null) return existing;

            // Create Sprites folder
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }

            // Create 64x64 white texture
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            // Save as PNG
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(spritePath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.Refresh();

            // Configure as sprite
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        private static void CreateElementFactory(ElementColorConfig colorConfig, ElementComponent prefab)
        {
            var factoryGO = GameObject.Find("ElementFactory");
            if (factoryGO == null)
            {
                factoryGO = new GameObject("ElementFactory");
                Undo.RegisterCreatedObjectUndo(factoryGO, "Create ElementFactory");
            }

            ElementFactoryComponent factory;
            if (!factoryGO.TryGetComponent(out factory))
            {
                factory = Undo.AddComponent<ElementFactoryComponent>(factoryGO);
            }

            // Link prefab and config
            var so = new SerializedObject(factory);
            so.FindProperty("_elementPrefab").objectReferenceValue = prefab;
            so.FindProperty("_colorConfig").objectReferenceValue = colorConfig;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Create Elements parent
            var elementsParent = GameObject.Find("Elements");
            if (elementsParent == null)
            {
                elementsParent = new GameObject("Elements");
                Undo.RegisterCreatedObjectUndo(elementsParent, "Create Elements Parent");
            }

            so = new SerializedObject(factory);
            so.FindProperty("_elementsParent").objectReferenceValue = elementsParent.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = factoryGO;
        }
    }
}
