using UnityEngine;

namespace Match3.Input
{
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public static class SwipeDirectionExtensions
    {
        public static Vector2Int ToGridOffset(this SwipeDirection direction)
        {
            return direction switch
            {
                SwipeDirection.Up => Vector2Int.up,
                SwipeDirection.Down => Vector2Int.down,
                SwipeDirection.Left => Vector2Int.left,
                SwipeDirection.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };
        }

        public static SwipeDirection FromDelta(Vector2 delta, float threshold)
        {
            if (delta.magnitude < threshold)
                return SwipeDirection.None;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
