using UnityEngine;

namespace Match3.Grid
{
    public readonly struct Cell
    {
        public Vector2Int Position { get; }

        public Cell(Vector2Int position)
        {
            Position = position;
        }

        public Cell(int x, int y) : this(new Vector2Int(x, y)) { }
    }
}
