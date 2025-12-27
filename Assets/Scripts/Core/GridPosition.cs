using System;

namespace Match3.Core
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridPosition Up => new(X, Y + 1);
        public GridPosition Down => new(X, Y - 1);
        public GridPosition Left => new(X - 1, Y);
        public GridPosition Right => new(X + 1, Y);

        public bool IsAdjacentTo(GridPosition other)
        {
            int dx = Math.Abs(X - other.X);
            int dy = Math.Abs(Y - other.Y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Y})";
    }
}
