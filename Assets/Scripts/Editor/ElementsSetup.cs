using UnityEngine;
using UnityEditor;
using Match3.Elements;
using System.IO;

namespace Match3.Editor
{
    public static class ElementsSetup
    {
        private static readonly (ElementType type, Color color)[] ElementConfigs =
        {
            (ElementType.Red, new Color(1f, 0.27f, 0.27f)),      // #FF4444
            (ElementType.Blue, new Color(0.27f, 0.53f, 1f)),     // #4488FF
            (ElementType.Green, new Color(0.27f, 0.87f, 0.27f)), // #44DD44
            (ElementType.Yellow, new Color(1f, 0.87f, 0.27f)),   // #FFDD44
            (ElementType.Purple, new Color(0.67f, 0.27f, 1f))    // #AA44FF
        };

        [MenuItem("Match3/Setup/Create Element Assets")]
        public static void CreateElementAssets()
        {
            EnsureDirectories();

            var elementDatas = new ElementData[ElementConfigs.Length];

            for (int i = 0; i < ElementConfigs.Length; i++)
            {
                elementDatas[i] = CreateElementData(ElementConfigs[i].type, ElementConfigs[i].color);
            }

            CreateDatabase(elementDatas);
            CreatePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Match3] Element assets created successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Elements"))
                AssetDatabase.CreateFolder("Assets/Data", "Elements");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static ElementData CreateElementData(ElementType type, Color color)
        {
            string path = $"Assets/Data/Elements/{type}Element.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            if (existing != null)
            {
                Debug.Log($"[Match3] {type}Element already exists, skipping...");
                return existing;
            }

            var data = ScriptableObject.CreateInstance<ElementData>();

            var so = new SerializedObject(data);
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[Match3] Created {type}Element");

            return data;
        }

        private static void CreateDatabase(ElementData[] elements)
        {
            string path = "Assets/Data/Elements/ElementDatabase.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ElementDatabase>(path);
            if (existing != null)
            {
                Debug.Log("[Match3] ElementDatabase already exists, updating...");
                var so = new SerializedObject(existing);
                var list = so.FindProperty("_elements");
                list.ClearArray();
                for (int i = 0; i < elements.Length; i++)
                {
                    list.InsertArrayElementAtIndex(i);
                    list.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return;
            }

            var database = ScriptableObject.CreateInstance<ElementDatabase>();
            var serialized = new SerializedObject(database);
            var elementsList = serialized.FindProperty("_elements");

            for (int i = 0; i < elements.Length; i++)
            {
                elementsList.InsertArrayElementAtIndex(i);
                elementsList.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(database, path);
            Debug.Log("[Match3] Created ElementDatabase");
        }

        private static void CreatePrefab()
        {
            string path = "Assets/Prefabs/Element.prefab";

            if (File.Exists(path))
            {
                Debug.Log("[Match3] Element.prefab already exists, skipping...");
                return;
            }

            var go = new GameObject("Element");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = SortingLayerExists("Elements") ? "Elements" : "Default";

            go.AddComponent<ElementComponent>();

            // Link SpriteRenderer via SerializedObject
            var element = go.GetComponent<ElementComponent>();
            var so = new SerializedObject(element);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.Log("[Match3] Created Element.prefab");
        }

        private static bool SortingLayerExists(string layerName)
        {
            foreach (var layer in SortingLayer.layers)
            {
                if (layer.name == layerName) return true;
            }
            return false;
        }

        [MenuItem("Match3/Setup/Create Sorting Layers")]
        public static void CreateSortingLayers()
        {
            var layers = new[] { "Board", "Elements", "Effects" };

            var tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var sortingLayers = tagManager.FindProperty("m_SortingLayers");

            foreach (var layerName in layers)
            {
                bool exists = false;
                for (int i = 0; i < sortingLayers.arraySize; i++)
                {
                    if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                    var newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
                    newLayer.FindPropertyRelative("name").stringValue = layerName;
                    newLayer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();
                    Debug.Log($"[Match3] Created Sorting Layer: {layerName}");
                }
            }

            tagManager.ApplyModifiedProperties();
            Debug.Log("[Match3] Sorting layers setup complete!");
        }
    }
}
