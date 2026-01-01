using UnityEngine;

namespace Match3.Grid
{
    public class GridView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GridConfig _config;

        [Header("Visuals (Optional)")]
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private Transform _cellsParent;

        private GridData _gridData;
        private GameObject[,] _cellVisuals;

        public GridData Data => _gridData;
        public GridConfig Config => _config;

        private void Awake()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            _gridData = new GridData(_config);

            if (_cellPrefab != null)
            {
                CreateCellVisuals();
            }
        }

        private void CreateCellVisuals()
        {
            Transform parent = _cellsParent != null ? _cellsParent : transform;
            _cellVisuals = new GameObject[_config.Width, _config.Height];

            for (int x = 0; x < _config.Width; x++)
            {
                for (int y = 0; y < _config.Height; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = _gridData.GridToWorld(gridPos);

                    GameObject cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, parent);
                    cell.name = $"Cell_{x}_{y}";
                    _cellVisuals[x, y] = cell;
                }
            }
        }

        /// <summary>
        /// Returns visual cell at position (if exists).
        /// </summary>
        public GameObject GetCellVisual(Vector2Int pos)
        {
            if (_cellVisuals == null || !_gridData.IsValidPosition(pos))
                return null;
            return _cellVisuals[pos.x, pos.y];
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gizmoColor = new Color(1f, 1f, 1f, 0.3f);

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _config == null) return;

            Gizmos.color = _gizmoColor;

            // Draw grid cells
            for (int x = 0; x < _config.Width; x++)
            {
                for (int y = 0; y < _config.Height; y++)
                {
                    Vector3 center = GetGizmoCenter(x, y);
                    Vector3 size = Vector3.one * _config.CellSize * 0.95f;
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }

        private Vector3 GetGizmoCenter(int x, int y)
        {
            float posX = _config.Origin.x + x * _config.CellSize + _config.CellSize * 0.5f;
            float posY = _config.Origin.y + y * _config.CellSize + _config.CellSize * 0.5f;
            return new Vector3(posX, posY, _config.Origin.z);
        }
#endif
    }
}
