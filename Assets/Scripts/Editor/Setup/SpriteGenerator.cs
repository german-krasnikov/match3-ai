// Assets/Scripts/Editor/Setup/SpriteGenerator.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using Configs;

namespace Editor.Setup
{
    public static class SpriteGenerator
    {
        private const string SpritesFolder = "Assets/Sprites/Elements";
        private const int SpriteSize = 128;

        private static readonly Color[] ElementColors =
        {
            new Color(0.9f, 0.2f, 0.2f),  // Red
            new Color(0.2f, 0.4f, 0.9f),  // Blue
            new Color(0.2f, 0.8f, 0.3f),  // Green
            new Color(0.95f, 0.8f, 0.1f), // Yellow
            new Color(0.7f, 0.3f, 0.9f)   // Purple
        };

        private static readonly string[] ElementNames = { "Red", "Blue", "Green", "Yellow", "Purple" };

        [MenuItem("Setup/Generate Element Sprites")]
        public static void GenerateSprites()
        {
            CreateFolders();
            var sprites = new Sprite[5];

            for (int i = 0; i < 5; i++)
            {
                var path = $"{SpritesFolder}/{ElementNames[i]}.png";
                CreateCircleTexture(path, ElementColors[i]);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            AssignSpritesToConfig(sprites);
            Debug.Log($"[SpriteGenerator] Created 5 element sprites at {SpritesFolder}");
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(SpritesFolder))
                AssetDatabase.CreateFolder("Assets/Sprites", "Elements");
        }

        private static void CreateCircleTexture(string path, Color color)
        {
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            var center = SpriteSize / 2f;
            var radius = SpriteSize / 2f - 4;

            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist < radius - 2)
                    {
                        // Inner gradient for 3D effect
                        float t = dist / radius;
                        var c = Color.Lerp(color * 1.3f, color * 0.7f, t);
                        c.a = 1f;
                        texture.SetPixel(x, y, c);
                    }
                    else if (dist < radius)
                    {
                        // Anti-aliased edge
                        float alpha = 1f - (dist - (radius - 2)) / 2f;
                        var c = color * 0.7f;
                        c.a = alpha;
                        texture.SetPixel(x, y, c);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            ConfigureTextureAsSprite(path);
        }

        private static void ConfigureTextureAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void AssignSpritesToConfig(Sprite[] sprites)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Configs/GameConfig.asset");
            if (config == null)
            {
                Debug.LogWarning("[SpriteGenerator] GameConfig not found. Run Setup/Step 2 first.");
                return;
            }

            config.ElementSprites = sprites;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[SpriteGenerator] Assigned sprites to GameConfig.ElementSprites");
        }

        [MenuItem("Setup/Generate Element Sprites", validate = true)]
        private static bool ValidateGenerateSprites() => !EditorApplication.isPlaying;
    }
}
