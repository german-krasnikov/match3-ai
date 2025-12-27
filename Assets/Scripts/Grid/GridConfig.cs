using UnityEngine;

namespace Match3.Grid
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Size")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _spacing = 0.1f;

        [Header("Visuals")]
        [SerializeField] private Color _cellColorA = new(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _cellColorB = new(0.8f, 0.8f, 0.8f);

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public float Spacing => _spacing;
        public float TotalCellSize => _cellSize + _spacing;
        public Color CellColorA => _cellColorA;
        public Color CellColorB => _cellColorB;

        public Vector3 GetGridOffset()
        {
            float offsetX = (_width - 1) * TotalCellSize / 2f;
            float offsetY = (_height - 1) * TotalCellSize / 2f;
            return new Vector3(-offsetX, -offsetY, 0);
        }
    }
}
