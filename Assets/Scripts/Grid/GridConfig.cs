using UnityEngine;

namespace Match3.Grid
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 1f;

        [Header("Position")]
        [Tooltip("World position of bottom-left cell (0,0)")]
        [SerializeField] private Vector3 _origin = Vector3.zero;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Vector3 Origin => _origin;
        public Vector2Int Size => new Vector2Int(_width, _height);
    }
}
