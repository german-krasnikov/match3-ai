using Match3.Core;
using Match3.Elements;

namespace Match3.Gravity
{
    public readonly struct FallData
    {
        public IElement Element { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public int Distance { get; }

        public FallData(IElement element, GridPosition from, GridPosition to)
        {
            Element = element;
            From = from;
            To = to;
            Distance = from.Y - to.Y;
        }
    }
}
