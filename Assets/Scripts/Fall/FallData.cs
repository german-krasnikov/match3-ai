using UnityEngine;
using Match3.Elements;

namespace Match3.Fall
{
    public readonly struct FallData
    {
        public ElementComponent Element { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public int Distance { get; }

        public FallData(ElementComponent element, Vector2Int from, Vector2Int to)
        {
            Element = element;
            From = from;
            To = to;
            Distance = from.y - to.y;
        }

        public override string ToString()
            => $"Fall: {Element?.Type} from {From} to {To} (dist={Distance})";
    }
}
