using UnityEngine;
using Match3.Data;

namespace Match3.Interfaces
{
    public interface ICell
    {
        Vector2Int GridPosition { get; }
        CellType CellType { get; }
        bool CanHoldTile { get; }
        bool IsEmpty { get; }
    }
}
