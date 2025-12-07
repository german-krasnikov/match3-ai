using System.Collections.Generic;
using Match3.Core;
using Match3.Grid;

namespace Match3.Gravity
{
    public class GravityCalculator
    {
        private readonly List<FallData> _falls = new();

        public List<FallData> CalculateFalls(GridData grid, IEnumerable<int> affectedColumns)
        {
            _falls.Clear();

            foreach (int column in affectedColumns)
                ProcessColumn(grid, column);

            return _falls;
        }

        private void ProcessColumn(GridData grid, int column)
        {
            int writeY = 0;

            for (int readY = 0; readY < grid.Height; readY++)
            {
                var pos = new GridPosition(column, readY);
                var element = grid.GetElement(pos);

                if (element == null) continue;

                if (readY != writeY)
                {
                    var newPos = new GridPosition(column, writeY);
                    _falls.Add(new FallData(element, pos, newPos));
                }

                writeY++;
            }
        }

        public int CountEmptyInColumn(GridData grid, int column)
        {
            int empty = 0;
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.GetElement(new GridPosition(column, y)) == null)
                    empty++;
            }
            return empty;
        }
    }
}
