using UnityEngine;
using UnityEditor;
using Match3.Grid;

namespace Match3.Editor
{
    public static class GridSceneSetup
    {
        [MenuItem("Match3/Setup Step 2 - Grid")]
        public static void SetupGridScene()
        {
            // Find or create Grid
            var gridGO = GameObject.Find("Grid");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid");
                Undo.RegisterCreatedObjectUndo(gridGO, "Create Grid");
            }

            // Add GridComponent
            if (!gridGO.TryGetComponent(out GridComponent _))
            {
                Undo.AddComponent<GridComponent>(gridGO);
            }

            // Center camera on 8x8 grid (cellSize=1, origin=0)
            var cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam.transform, "Center Camera");
                cam.transform.position = new Vector3(4f, 4f, -10f);
                cam.orthographicSize = 5f;
            }

            Selection.activeGameObject = gridGO;
            Debug.Log("Step 2 Grid setup complete. Check Gizmos in Scene View.");
        }
    }
}
