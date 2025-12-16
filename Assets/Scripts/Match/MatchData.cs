using System.Collections.Generic;

public class MatchData
{
    public List<Cell> Cells { get; }
    public ElementType Type { get; }

    public int Length => Cells.Count;
    public bool IsHorizontal { get; private set; }
    public bool IsVertical { get; private set; }
    public bool IsSpecial => IsHorizontal && IsVertical;

    public MatchData(ElementType type)
    {
        Type = type;
        Cells = new List<Cell>();
    }

    public void AddCell(Cell cell)
    {
        if (!Cells.Contains(cell))
            Cells.Add(cell);
    }

    public void AddCells(IEnumerable<Cell> cells)
    {
        foreach (var cell in cells)
            AddCell(cell);
    }

    public bool ContainsCell(Cell cell) => Cells.Contains(cell);

    public bool ContainsCell(int x, int y)
    {
        foreach (var cell in Cells)
            if (cell.X == x && cell.Y == y) return true;
        return false;
    }

    public void SetHorizontal() => IsHorizontal = true;
    public void SetVertical() => IsVertical = true;

    public bool TryMerge(MatchData other)
    {
        if (other.Type != Type) return false;

        bool hasIntersection = false;
        foreach (var cell in other.Cells)
        {
            if (ContainsCell(cell))
            {
                hasIntersection = true;
                break;
            }
        }

        if (!hasIntersection) return false;

        AddCells(other.Cells);
        if (other.IsHorizontal) SetHorizontal();
        if (other.IsVertical) SetVertical();

        return true;
    }

    public override string ToString()
    {
        string dir = IsSpecial ? "L/T" : (IsHorizontal ? "H" : "V");
        return $"Match({Type}, {Length}, {dir})";
    }
}
