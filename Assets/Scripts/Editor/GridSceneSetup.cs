#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Match3.Grid;

namespace Match3.Editor
{
    public static class GridSceneSetup
    {
        private const string GridDataPath = "Assets/Data/Grid/DefaultGridData.asset";
        private const string GridObjectName = "Grid";

        [MenuItem("Match3/Setup Scene/Stage 1 - Grid System")]
        public static void SetupGridScene()
        {
            var gridData = GetOrCreateGridData();
            var gridComponent = GetOrCreateGridObject(gridData);
            SetupCamera(gridData);

            Selection.activeGameObject = gridComponent.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[Match3] Grid System setup complete!");
        }

        private static GridData GetOrCreateGridData()
        {
            var gridData = AssetDatabase.LoadAssetAtPath<GridData>(GridDataPath);

            if (gridData == null)
            {
                gridData = ScriptableObject.CreateInstance<GridData>();

                // Создаём папку если нет
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                    AssetDatabase.CreateFolder("Assets", "Data");
                if (!AssetDatabase.IsValidFolder("Assets/Data/Grid"))
                    AssetDatabase.CreateFolder("Assets/Data", "Grid");

                AssetDatabase.CreateAsset(gridData, GridDataPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[Match3] Created GridData at {GridDataPath}");
            }

            return gridData;
        }

        private static GridComponent GetOrCreateGridObject(GridData gridData)
        {
            var existingGrid = Object.FindFirstObjectByType<GridComponent>();

            if (existingGrid != null)
            {
                // Обновляем существующий
                var so = new SerializedObject(existingGrid);
                so.FindProperty("_gridData").objectReferenceValue = gridData;
                so.ApplyModifiedProperties();

                Debug.Log("[Match3] Updated existing Grid object");
                return existingGrid;
            }

            // Создаём новый
            var gridObject = new GameObject(GridObjectName);
            gridObject.transform.position = Vector3.zero;

            var gridComponent = gridObject.AddComponent<GridComponent>();

            var serializedObject = new SerializedObject(gridComponent);
            serializedObject.FindProperty("_gridData").objectReferenceValue = gridData;
            serializedObject.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(gridObject, "Create Grid");

            Debug.Log("[Match3] Created Grid object");
            return gridComponent;
        }

        private static void SetupCamera(GridData gridData)
        {
            var camera = Camera.main;
            if (camera == null) return;

            float step = gridData.Step;

            // Центр сетки
            float centerX = gridData.Width * step * 0.5f - gridData.Spacing * 0.5f;
            float centerY = gridData.Height * step * 0.5f - gridData.Spacing * 0.5f;

            camera.transform.position = new Vector3(centerX, centerY, -10f);

            // Ортографический размер чтобы сетка помещалась
            if (camera.orthographic)
            {
                float gridHeight = gridData.Height * step;
                camera.orthographicSize = gridHeight * 0.6f; // небольшой отступ
            }

            Debug.Log("[Match3] Camera positioned to grid center");
        }
    }
}
#endif
