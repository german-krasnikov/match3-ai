using UnityEngine;

namespace Match3.Core
{
    public interface IGridElement
    {
        Vector2Int GridPosition { get; set; }
        ElementType Type { get; }
        GameObject GameObject { get; }
    }
}
