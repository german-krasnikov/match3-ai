using System;
using Match3.Data;
using UnityEngine;

namespace Match3.Grid
{
    public class GridView : MonoBehaviour
    {
        public event Action OnGridReady;

        [SerializeField] private GridConfig _config;
        [SerializeField] private SpriteRenderer _cellPrefab;
        [SerializeField] private Transform _cellsParent;

        private GridPositionConverter _positionConverter;

        public GridConfig Config => _config;

        public GridPositionConverter PositionConverter
        {
            get
            {
                EnsureInitialized();
                return _positionConverter;
            }
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (_positionConverter != null) return;
            var origin = CalculateGridOrigin();
            _positionConverter = new GridPositionConverter(_config.CellSize, origin);
        }

        public void CreateVisualGrid()
        {
            EnsureInitialized();

            if (_cellPrefab == null)
            {
                OnGridReady?.Invoke();
                return;
            }

            for (int x = 0; x < _config.Width; x++)
            {
                for (int y = 0; y < _config.Height; y++)
                {
                    var pos = new Core.GridPosition(x, y);
                    var cell = Instantiate(_cellPrefab, _cellsParent);
                    cell.transform.position = _positionConverter.GridToWorld(pos);
                }
            }
            OnGridReady?.Invoke();
        }

        private Vector2 CalculateGridOrigin()
        {
            float offsetX = (_config.Width - 1) * _config.CellSize * 0.5f;
            float offsetY = (_config.Height - 1) * _config.CellSize * 0.5f;
            return new Vector2(transform.position.x - offsetX, transform.position.y - offsetY);
        }
    }
}
