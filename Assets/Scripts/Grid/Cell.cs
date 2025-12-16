using System;

public class Cell
{
    public int X { get; }
    public int Y { get; }

    private ElementComponent _element;

    public event Action<ElementComponent> OnElementChanged;

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    public ElementComponent Element
    {
        get => _element;
        set
        {
            if (_element != value)
            {
                _element = value;
                OnElementChanged?.Invoke(_element);
            }
        }
    }

    public bool IsEmpty => _element == null;

    public void Clear() => Element = null;

    public override string ToString() => $"Cell({X}, {Y})";
}
