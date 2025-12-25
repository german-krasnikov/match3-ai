using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;

namespace Match3.Refill
{
    public static class RefillCalculator
    {
        private static readonly List<RefillData> _refillsBuffer = new(64);
        private static readonly Dictionary<int, int> _columnCounters = new(8);

        public static List<RefillData> CalculateRefills(BoardComponent board, GridComponent grid)
        {
            _refillsBuffer.Clear();
            _columnCounters.Clear();

            // Scan bottom to top - lower positions fill first (natural falling)
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var pos = new Vector2Int(x, y);

                    if (board.IsEmpty(pos))
                    {
                        var refillData = CreateRefillData(pos, x, grid, board.Height);
                        _refillsBuffer.Add(refillData);
                    }
                }
            }

            return new List<RefillData>(_refillsBuffer);
        }

        private static RefillData CreateRefillData(
            Vector2Int targetPos,
            int column,
            GridComponent grid,
            int gridHeight)
        {
            if (!_columnCounters.TryGetValue(column, out int spawnIndex))
                spawnIndex = 0;

            _columnCounters[column] = spawnIndex + 1;

            var spawnPos = new Vector2Int(column, gridHeight + spawnIndex);
            var spawnWorldPos = grid.GridToWorld(spawnPos);
            var targetWorldPos = grid.GridToWorld(targetPos);

            return new RefillData(targetPos, spawnPos, spawnWorldPos, targetWorldPos);
        }
    }
}
