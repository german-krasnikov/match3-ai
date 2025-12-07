using System;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Spawn
{
    public class SpawnController : MonoBehaviour
    {
        public event Action OnFillComplete;
        public event Action<int> OnSpawnedInColumn;

        [SerializeField] private GridView _gridView;
        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private GridConfig _config;
        private GridPositionConverter _converter;
        private ISpawnStrategy _strategy;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _config = _gridView.Config;
            _converter = _gridView.PositionConverter;
            _strategy = new NoMatchSpawnStrategy();
        }

        public void SetStrategy(ISpawnStrategy strategy)
        {
            _strategy = strategy ?? new NoMatchSpawnStrategy();
        }

        public void FillGrid()
        {
            for (int y = 0; y < _config.Height; y++)
            {
                for (int x = 0; x < _config.Width; x++)
                {
                    var pos = new GridPosition(x, y);
                    if (_grid.GetElement(pos) != null) continue;
                    SpawnElement(pos);
                }
            }

            OnFillComplete?.Invoke();
        }

        public IElement SpawnAtTop(int column, int offsetAboveGrid = 1)
        {
            var gridPos = new GridPosition(column, _config.Height - 1);
            var spawnWorldPos = _converter.GridToWorld(
                new GridPosition(column, _config.Height - 1 + offsetAboveGrid)
            );

            var type = _strategy.GetElementType(gridPos, _grid, _config);
            var element = _factory.CreateElement(type, gridPos, spawnWorldPos);

            OnSpawnedInColumn?.Invoke(column);
            return element;
        }

        private void SpawnElement(GridPosition pos)
        {
            var worldPos = _converter.GridToWorld(pos);
            var type = _strategy.GetElementType(pos, _grid, _config);
            var element = _factory.CreateElement(type, pos, worldPos);
            _grid.SetElement(pos, element);
        }
    }
}
