using UnityEngine;

public class GridData
{
    private readonly int _width;
    private readonly int _height;
    private readonly float _cellSize;
    private readonly Vector3 _origin;

    public Vector2Int Size => new Vector2Int(_width, _height);
    public int Width => _width;
    public int Height => _height;

    public GridData(GridConfig config)
    {
        _width = config.Width;
        _height = config.Height;
        _cellSize = config.CellSize;
        _origin = config.Origin;
    }

    public GridData(int width, int height, float cellSize, Vector3 origin)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _origin = origin;
    }

    /// <summary>
    /// Converts grid position to world position (center of cell).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = _origin.x + gridPos.x * _cellSize + _cellSize * 0.5f;
        float y = _origin.y + gridPos.y * _cellSize + _cellSize * 0.5f;
        return new Vector3(x, y, _origin.z);
    }

    /// <summary>
    /// Converts world position to grid position.
    /// Returns nearest cell, may be outside grid bounds.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - _origin.x) / _cellSize);
        int y = Mathf.FloorToInt((worldPos.y - _origin.y) / _cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Checks if grid position is within bounds.
    /// </summary>
    public bool IsValidPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _width &&
               gridPos.y >= 0 && gridPos.y < _height;
    }

    /// <summary>
    /// Gets world position above the grid for spawning.
    /// </summary>
    public Vector3 GetSpawnPosition(int column, int rowsAbove = 1)
    {
        return GridToWorld(new Vector2Int(column, _height - 1 + rowsAbove));
    }
}
