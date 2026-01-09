// MatchService.cs
using System.Collections.Generic;
using Features.Board.Models;
using Common;

namespace Features.Board.Services
{
    /// <summary>
    /// Stateless service for detecting matches on the board.
    /// Pure C# - testable without Unity.
    /// </summary>
    public class MatchService : IMatchService
    {
        private const int MinMatchLength = 3;

        public List<GridPosition> FindAllMatches(BoardModel board)
        {
            var matches = new HashSet<GridPosition>();

            // Check horizontal matches
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width - MinMatchLength + 1; x++)
                {
                    var line = GetHorizontalLine(board, x, y);
                    if (line.Count >= MinMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        x += line.Count - 1; // Skip matched positions
                    }
                }
            }

            // Check vertical matches
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height - MinMatchLength + 1; y++)
                {
                    var line = GetVerticalLine(board, x, y);
                    if (line.Count >= MinMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        y += line.Count - 1; // Skip matched positions
                    }
                }
            }

            return new List<GridPosition>(matches);
        }

        public List<GridPosition> FindMatchesAt(BoardModel board, GridPosition pos)
        {
            var matches = new HashSet<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return new List<GridPosition>();

            // Check horizontal line through position
            var horizontal = GetHorizontalLineThrough(board, pos);
            if (horizontal.Count >= MinMatchLength)
            {
                foreach (var p in horizontal)
                    matches.Add(p);
            }

            // Check vertical line through position
            var vertical = GetVerticalLineThrough(board, pos);
            if (vertical.Count >= MinMatchLength)
            {
                foreach (var p in vertical)
                    matches.Add(p);
            }

            return new List<GridPosition>(matches);
        }

        public bool WouldCreateMatch(BoardModel board, GridPosition pos, ElementType type)
        {
            if (type == ElementType.None)
                return false;

            // Check horizontal: count same type to left and right
            int horizontalCount = 1;
            horizontalCount += CountInDirection(board, pos, -1, 0, type);
            horizontalCount += CountInDirection(board, pos, 1, 0, type);

            if (horizontalCount >= MinMatchLength)
                return true;

            // Check vertical: count same type up and down
            int verticalCount = 1;
            verticalCount += CountInDirection(board, pos, 0, -1, type);
            verticalCount += CountInDirection(board, pos, 0, 1, type);

            return verticalCount >= MinMatchLength;
        }

        private List<GridPosition> GetHorizontalLine(BoardModel board, int startX, int y)
        {
            var line = new List<GridPosition>();
            var startPos = new GridPosition(startX, y);
            var element = board.GetElement(startPos);

            if (element == null || !element.CanMatch)
                return line;

            line.Add(startPos);

            for (int x = startX + 1; x < board.Width; x++)
            {
                var pos = new GridPosition(x, y);
                var current = board.GetElement(pos);

                if (current != null && element.Matches(current))
                    line.Add(pos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetVerticalLine(BoardModel board, int x, int startY)
        {
            var line = new List<GridPosition>();
            var startPos = new GridPosition(x, startY);
            var element = board.GetElement(startPos);

            if (element == null || !element.CanMatch)
                return line;

            line.Add(startPos);

            for (int y = startY + 1; y < board.Height; y++)
            {
                var pos = new GridPosition(x, y);
                var current = board.GetElement(pos);

                if (current != null && element.Matches(current))
                    line.Add(pos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetHorizontalLineThrough(BoardModel board, GridPosition pos)
        {
            var line = new List<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return line;

            // Find leftmost position of the line
            int left = pos.X;
            while (left > 0)
            {
                var leftPos = new GridPosition(left - 1, pos.Y);
                var leftElement = board.GetElement(leftPos);
                if (leftElement != null && element.Matches(leftElement))
                    left--;
                else
                    break;
            }

            // Collect all positions from left to right
            for (int x = left; x < board.Width; x++)
            {
                var currentPos = new GridPosition(x, pos.Y);
                var current = board.GetElement(currentPos);

                if (current != null && element.Matches(current))
                    line.Add(currentPos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetVerticalLineThrough(BoardModel board, GridPosition pos)
        {
            var line = new List<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return line;

            // Find topmost position of the line (lowest Y)
            int top = pos.Y;
            while (top > 0)
            {
                var topPos = new GridPosition(pos.X, top - 1);
                var topElement = board.GetElement(topPos);
                if (topElement != null && element.Matches(topElement))
                    top--;
                else
                    break;
            }

            // Collect all positions from top to bottom
            for (int y = top; y < board.Height; y++)
            {
                var currentPos = new GridPosition(pos.X, y);
                var current = board.GetElement(currentPos);

                if (current != null && element.Matches(current))
                    line.Add(currentPos);
                else
                    break;
            }

            return line;
        }

        private int CountInDirection(BoardModel board, GridPosition start, int dx, int dy, ElementType type)
        {
            int count = 0;
            int x = start.X + dx;
            int y = start.Y + dy;

            while (board.IsValidPosition(new GridPosition(x, y)))
            {
                var element = board.GetElement(new GridPosition(x, y));
                if (element != null && element.Type == type)
                {
                    count++;
                    x += dx;
                    y += dy;
                }
                else
                {
                    break;
                }
            }

            return count;
        }
    }
}
