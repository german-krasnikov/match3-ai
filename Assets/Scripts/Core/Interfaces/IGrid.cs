using System;
using UnityEngine;

namespace Match3.Core
{
    public interface IGrid
    {
        int Width { get; }
        int Height { get; }
        float CellSize { get; }

        Vector3 GridToWorld(Vector2Int gridPos);
        Vector2Int WorldToGrid(Vector3 worldPos);
        bool IsValidPosition(Vector2Int pos);

        IGridElement GetElementAt(Vector2Int pos);
        void SetElementAt(Vector2Int pos, IGridElement element);
        void ClearCell(Vector2Int pos);

        event Action<Vector2Int, IGridElement> OnElementPlaced;
        event Action<Vector2Int> OnCellCleared;
    }
}
