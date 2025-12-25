using UnityEngine;

namespace Match3.Grid
{
    [CreateAssetMenu(fileName = "GridData", menuName = "Match3/Grid Data")]
    public class GridData : ScriptableObject
    {
        [Header("Dimensions")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _spacing = 0.1f;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public float Spacing => _spacing;
        public float Step => _cellSize + _spacing;

        private void OnValidate()
        {
            _width = Mathf.Max(1, _width);
            _height = Mathf.Max(1, _height);
            _cellSize = Mathf.Max(0.1f, _cellSize);
            _spacing = Mathf.Max(0f, _spacing);
        }
    }
}
