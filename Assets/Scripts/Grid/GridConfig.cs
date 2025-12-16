using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/GridConfig")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Size")]
    [SerializeField, Range(5, 12)] private int _width = 8;
    [SerializeField, Range(5, 12)] private int _height = 8;

    [Header("Cell Settings")]
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _originOffset = Vector2.zero;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public Vector2 OriginOffset => _originOffset;
}
