using UnityEngine;

namespace Match3.Core
{
    public interface IGrid
    {
        int Width { get; }
        int Height { get; }

        Vector3 GridToWorld(GridPosition position);
        GridPosition WorldToGrid(Vector3 worldPosition);
        bool IsValidPosition(GridPosition position);
    }
}
