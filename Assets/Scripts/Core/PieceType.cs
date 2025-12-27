namespace Match3.Core
{
    public enum PieceType
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6
    }

    public static class PieceTypeExtensions
    {
        public const int PlayableCount = 6;

        public static bool IsPlayable(this PieceType type) => type != PieceType.None;
    }
}
