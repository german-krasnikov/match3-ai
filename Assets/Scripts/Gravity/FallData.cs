public class FallData
{
    public ElementComponent Element { get; }
    public int FromY { get; }
    public int ToY { get; }
    public int Column { get; }
    public bool IsNewElement { get; }

    public int Distance => FromY - ToY;

    public FallData(ElementComponent element, int fromY, int toY, int column, bool isNew = false)
    {
        Element = element;
        FromY = fromY;
        ToY = toY;
        Column = column;
        IsNewElement = isNew;
    }
}
