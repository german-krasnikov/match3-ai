using UnityEngine;

namespace Match3.Gem
{
    public readonly struct GemData
    {
        public GemType Type { get; }
        public Vector2Int Position { get; }

        public GemData(GemType type, Vector2Int position)
        {
            Type = type;
            Position = position;
        }

        public GemData WithPosition(Vector2Int newPosition)
        {
            return new GemData(Type, newPosition);
        }

        public override string ToString()
        {
            return $"Gem({Type}, {Position})";
        }
    }
}
