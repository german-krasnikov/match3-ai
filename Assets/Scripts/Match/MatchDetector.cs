using System.Collections.Generic;

public class MatchDetector
{
    private const int MinMatchLength = 3;

    public List<MatchData> FindMatches(GridComponent grid)
    {
        var horizontalMatches = FindHorizontalMatches(grid);
        var verticalMatches = FindVerticalMatches(grid);

        var allMatches = new List<MatchData>();
        allMatches.AddRange(horizontalMatches);
        allMatches.AddRange(verticalMatches);

        return MergeIntersectingMatches(allMatches);
    }

    private List<MatchData> FindHorizontalMatches(GridComponent grid)
    {
        var matches = new List<MatchData>();

        for (int y = 0; y < grid.Height; y++)
        {
            int x = 0;
            while (x < grid.Width)
            {
                var startCell = grid.GetCell(x, y);

                if (startCell.IsEmpty)
                {
                    x++;
                    continue;
                }

                var type = startCell.Element.Type;
                var matchCells = new List<Cell> { startCell };

                int nextX = x + 1;
                while (nextX < grid.Width)
                {
                    var nextCell = grid.GetCell(nextX, y);
                    if (nextCell.IsEmpty || nextCell.Element.Type != type)
                        break;

                    matchCells.Add(nextCell);
                    nextX++;
                }

                if (matchCells.Count >= MinMatchLength)
                {
                    var match = new MatchData(type);
                    match.AddCells(matchCells);
                    match.SetHorizontal();
                    matches.Add(match);
                }

                x = nextX;
            }
        }

        return matches;
    }

    private List<MatchData> FindVerticalMatches(GridComponent grid)
    {
        var matches = new List<MatchData>();

        for (int x = 0; x < grid.Width; x++)
        {
            int y = 0;
            while (y < grid.Height)
            {
                var startCell = grid.GetCell(x, y);

                if (startCell.IsEmpty)
                {
                    y++;
                    continue;
                }

                var type = startCell.Element.Type;
                var matchCells = new List<Cell> { startCell };

                int nextY = y + 1;
                while (nextY < grid.Height)
                {
                    var nextCell = grid.GetCell(x, nextY);
                    if (nextCell.IsEmpty || nextCell.Element.Type != type)
                        break;

                    matchCells.Add(nextCell);
                    nextY++;
                }

                if (matchCells.Count >= MinMatchLength)
                {
                    var match = new MatchData(type);
                    match.AddCells(matchCells);
                    match.SetVertical();
                    matches.Add(match);
                }

                y = nextY;
            }
        }

        return matches;
    }

    private List<MatchData> MergeIntersectingMatches(List<MatchData> matches)
    {
        if (matches.Count <= 1) return matches;

        var merged = new List<MatchData>();
        var used = new bool[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            if (used[i]) continue;

            var current = matches[i];

            for (int j = i + 1; j < matches.Count; j++)
            {
                if (used[j]) continue;

                if (current.TryMerge(matches[j]))
                    used[j] = true;
            }

            merged.Add(current);
        }

        return merged;
    }

    public bool HasAnyMatch(GridComponent grid)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x <= grid.Width - MinMatchLength; x++)
            {
                if (IsHorizontalMatch(grid, x, y))
                    return true;
            }
        }

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y <= grid.Height - MinMatchLength; y++)
            {
                if (IsVerticalMatch(grid, x, y))
                    return true;
            }
        }

        return false;
    }

    private bool IsHorizontalMatch(GridComponent grid, int startX, int y)
    {
        var first = grid.GetCell(startX, y);
        if (first.IsEmpty) return false;

        var type = first.Element.Type;

        for (int i = 1; i < MinMatchLength; i++)
        {
            var cell = grid.GetCell(startX + i, y);
            if (cell.IsEmpty || cell.Element.Type != type)
                return false;
        }

        return true;
    }

    private bool IsVerticalMatch(GridComponent grid, int x, int startY)
    {
        var first = grid.GetCell(x, startY);
        if (first.IsEmpty) return false;

        var type = first.Element.Type;

        for (int i = 1; i < MinMatchLength; i++)
        {
            var cell = grid.GetCell(x, startY + i);
            if (cell.IsEmpty || cell.Element.Type != type)
                return false;
        }

        return true;
    }
}
