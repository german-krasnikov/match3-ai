using UnityEngine;

public static class GridDirections
{
    public static readonly Vector2Int Up = new(0, 1);
    public static readonly Vector2Int Down = new(0, -1);
    public static readonly Vector2Int Left = new(-1, 0);
    public static readonly Vector2Int Right = new(1, 0);

    public static readonly Vector2Int[] All = { Up, Down, Left, Right };
    public static readonly Vector2Int[] Horizontal = { Left, Right };
    public static readonly Vector2Int[] Vertical = { Up, Down };
}
