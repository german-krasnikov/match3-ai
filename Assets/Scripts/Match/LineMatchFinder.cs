using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Match
{
    public class LineMatchFinder : IMatchFinder
    {
        private const int MinMatchLength = 3;

        private readonly HashSet<GridPosition> _visitedH = new();
        private readonly HashSet<GridPosition> _visitedV = new();
        private readonly List<GridPosition> _buffer = new();

        public List<MatchData> FindAllMatches(GridData grid)
        {
            var matches = new List<MatchData>();
            _visitedH.Clear();
            _visitedV.Clear();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    CheckPosition(grid, new GridPosition(x, y), matches);
                }
            }

            return matches;
        }

        public List<MatchData> FindMatchesAt(GridData grid, IEnumerable<GridPosition> positions)
        {
            var matches = new List<MatchData>();
            _visitedH.Clear();
            _visitedV.Clear();

            foreach (var pos in positions)
            {
                CheckPosition(grid, pos, matches);
            }

            return matches;
        }

        private void CheckPosition(GridData grid, GridPosition pos, List<MatchData> matches)
        {
            var element = grid.GetElement(pos);
            if (element == null) return;

            // Horizontal
            if (!_visitedH.Contains(pos))
            {
                var line = GetLine(grid, pos, GridPosition.Left, GridPosition.Right, element.Type);
                if (line.Count >= MinMatchLength)
                {
                    matches.Add(new MatchData(line.ToArray(), element.Type, true));
                    MarkVisited(_visitedH, line);
                }
            }

            // Vertical
            if (!_visitedV.Contains(pos))
            {
                var line = GetLine(grid, pos, GridPosition.Down, GridPosition.Up, element.Type);
                if (line.Count >= MinMatchLength)
                {
                    matches.Add(new MatchData(line.ToArray(), element.Type, false));
                    MarkVisited(_visitedV, line);
                }
            }
        }

        private List<GridPosition> GetLine(GridData grid, GridPosition start, GridPosition negDir, GridPosition posDir, ElementType type)
        {
            _buffer.Clear();
            _buffer.Add(start);

            Extend(grid, start, negDir, type);
            Extend(grid, start, posDir, type);

            return _buffer;
        }

        private void Extend(GridData grid, GridPosition start, GridPosition dir, ElementType type)
        {
            var current = start + dir;
            while (grid.IsValidPosition(current))
            {
                var el = grid.GetElement(current);
                if (el == null || el.Type != type) break;
                _buffer.Add(current);
                current = current + dir;
            }
        }

        private void MarkVisited(HashSet<GridPosition> visited, List<GridPosition> positions)
        {
            foreach (var pos in positions)
                visited.Add(pos);
        }
    }
}
