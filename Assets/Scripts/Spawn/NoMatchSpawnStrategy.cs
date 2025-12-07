using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;
using UnityEngine;

namespace Match3.Spawn
{
    public class NoMatchSpawnStrategy : ISpawnStrategy
    {
        private readonly List<ElementType> _availableTypes = new();

        public ElementType GetElementType(GridPosition position, GridData grid, GridConfig config)
        {
            _availableTypes.Clear();

            foreach (var type in config.ElementTypes)
            {
                if (!WouldCreateMatch(position, type, grid))
                {
                    _availableTypes.Add(type);
                }
            }

            if (_availableTypes.Count == 0)
            {
                return config.ElementTypes[Random.Range(0, config.ElementTypes.Count)];
            }

            return _availableTypes[Random.Range(0, _availableTypes.Count)];
        }

        private bool WouldCreateMatch(GridPosition pos, ElementType type, GridData grid)
        {
            return CheckHorizontalMatch(pos, type, grid) || CheckVerticalMatch(pos, type, grid);
        }

        private bool CheckHorizontalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            var left1 = grid.GetElement(pos + GridPosition.Left);
            var left2 = grid.GetElement(pos + GridPosition.Left + GridPosition.Left);

            return left1 != null && left2 != null && left1.Type == type && left2.Type == type;
        }

        private bool CheckVerticalMatch(GridPosition pos, ElementType type, GridData grid)
        {
            var down1 = grid.GetElement(pos + GridPosition.Down);
            var down2 = grid.GetElement(pos + GridPosition.Down + GridPosition.Down);

            return down1 != null && down2 != null && down1.Type == type && down2.Type == type;
        }
    }
}
