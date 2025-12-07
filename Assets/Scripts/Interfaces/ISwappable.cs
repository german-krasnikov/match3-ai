using UnityEngine;

namespace Match3.Interfaces
{
    public interface ISwappable
    {
        Vector2Int GridPosition { get; }
        bool CanSwap { get; }
        void SetGridPosition(Vector2Int position);
    }
}
