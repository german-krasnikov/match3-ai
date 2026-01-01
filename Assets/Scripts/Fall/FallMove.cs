using UnityEngine;

namespace Match3.Fall
{
    /// <summary>
    /// Represents a single gem fall movement.
    /// </summary>
    public readonly struct FallMove
    {
        /// <summary>
        /// Starting grid position.
        /// </summary>
        public Vector2Int From { get; }

        /// <summary>
        /// Target grid position.
        /// </summary>
        public Vector2Int To { get; }

        /// <summary>
        /// Distance in cells (for animation timing).
        /// </summary>
        public int Distance => From.y - To.y;

        public FallMove(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"Fall({From} -> {To}, dist={Distance})";
        }
    }
}
