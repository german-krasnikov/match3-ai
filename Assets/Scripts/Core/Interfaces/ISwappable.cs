namespace Match3.Core
{
    public interface ISwappable
    {
        bool CanSwap { get; }
        GridPosition Position { get; }
    }
}
