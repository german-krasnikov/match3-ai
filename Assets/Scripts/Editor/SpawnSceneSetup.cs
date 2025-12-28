using UnityEngine;
using UnityEditor;
using Match3.Spawn;

namespace Match3.Editor
{
    /// <summary>
    /// Editor utility для настройки тестовой сцены Spawn System.
    /// </summary>
    public static class SpawnSceneSetup
    {
        [MenuItem("Match3/Setup/3. Setup Spawn System")]
        public static void SetupSpawnSystem()
        {
            // Находим Grid
            var grid = Object.FindFirstObjectByType<Grid.GridComponent>();
            if (grid == null)
            {
                Debug.LogError("GridComponent not found! Run 'Match3/Setup/1. Setup Grid' first.");
                return;
            }

            // Находим Factory
            var factory = Object.FindFirstObjectByType<Elements.ElementFactoryComponent>();
            if (factory == null)
            {
                Debug.LogError("ElementFactoryComponent not found! Run 'Match3/Setup/2. Setup Elements' first.");
                return;
            }

            // Создаём SpawnComponent на том же объекте что и Grid
            var spawn = grid.gameObject.GetComponent<SpawnComponent>();
            if (spawn == null)
            {
                spawn = grid.gameObject.AddComponent<SpawnComponent>();
            }

            // Связываем зависимости через SerializedObject
            var so = new SerializedObject(spawn);
            so.FindProperty("_gridComponent").objectReferenceValue = grid;
            so.FindProperty("_factoryComponent").objectReferenceValue = factory;
            so.ApplyModifiedProperties();

            // Добавляем тестовый компонент
            var tester = grid.gameObject.GetComponent<SpawnTester>();
            if (tester == null)
            {
                tester = grid.gameObject.AddComponent<SpawnTester>();
            }

            var testerSo = new SerializedObject(tester);
            testerSo.FindProperty("_spawn").objectReferenceValue = spawn;
            testerSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(grid.gameObject);

            Debug.Log("Spawn System setup complete! Press Play and press Space to fill grid.");
        }
    }
}
