using System;
using UnityEngine;

public class GridComponent : MonoBehaviour
{
    public event Action OnGridCreated;

    [SerializeField] private GridConfig _config;

    private Cell[,] _cells;

    public int Width => _config.Width;
    public int Height => _config.Height;
    public GridConfig Config => _config;

    private void Awake()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        _cells = new Cell[_config.Width, _config.Height];

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                _cells[x, y] = new Cell(x, y);
            }
        }

        OnGridCreated?.Invoke();
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return _cells[x, y];
    }

    public Cell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < _config.Width && y >= 0 && y < _config.Height;
    }

    public Vector3 GridToWorld(int x, int y)
    {
        float worldX = x * _config.CellSize + _config.OriginOffset.x;
        float worldY = y * _config.CellSize + _config.OriginOffset.y;
        return new Vector3(worldX, worldY, 0f);
    }

    public Vector3 GridToWorld(Vector2Int pos) => GridToWorld(pos.x, pos.y);

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - _config.OriginOffset.x) / _config.CellSize);
        int y = Mathf.RoundToInt((worldPos.y - _config.OriginOffset.y) / _config.CellSize);
        return new Vector2Int(x, y);
    }

    public Cell GetNeighbor(Cell cell, Vector2Int direction)
    {
        return GetCell(cell.X + direction.x, cell.Y + direction.y);
    }

    public Cell GetNeighbor(int x, int y, Vector2Int direction)
    {
        return GetCell(x + direction.x, y + direction.y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_config == null) return;

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        DrawGrid();
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;

        Gizmos.color = Color.cyan;
        DrawGrid();

        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                UnityEditor.Handles.Label(pos + Vector3.down * 0.3f, $"{x},{y}");
            }
        }
    }

    private void DrawGrid()
    {
        for (int x = 0; x < _config.Width; x++)
        {
            for (int y = 0; y < _config.Height; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                Gizmos.DrawWireCube(pos, Vector3.one * _config.CellSize * 0.95f);
            }
        }
    }
#endif
}
