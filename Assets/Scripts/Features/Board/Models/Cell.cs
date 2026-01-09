// Assets/Scripts/Features/Board/Models/Cell.cs
using Common;

namespace Features.Board.Models
{
    public class Cell
    {
        public GridPosition Position { get; }
        public Element Element { get; private set; }

        public Cell(GridPosition position)
        {
            Position = position;
        }

        public bool IsEmpty => Element == null;

        public void SetElement(Element element)
        {
            Element = element;
        }

        public Element RemoveElement()
        {
            var element = Element;
            Element = null;
            return element;
        }
    }
}
