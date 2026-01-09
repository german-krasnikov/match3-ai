// Assets/Scripts/Features/Board/Models/Element.cs
using Common;

namespace Features.Board.Models
{
    public class Element
    {
        public ElementType Type { get; }

        public Element(ElementType type)
        {
            Type = type;
        }

        public bool CanMatch => Type != ElementType.None;

        public bool Matches(Element other)
        {
            return other != null && Type == other.Type && CanMatch;
        }
    }
}
