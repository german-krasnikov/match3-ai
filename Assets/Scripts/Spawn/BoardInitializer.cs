using System.Collections.Generic;
using UnityEngine;

public class BoardInitializer : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawner;

    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                SpawnWithoutMatch(x, y);
            }
        }
    }

    private void SpawnWithoutMatch(int x, int y)
    {
        var excluded = GetExcludedTypes(x, y);

        if (excluded.Count > 0)
            _spawner.SpawnRandomExcludingAt(x, y, excluded.ToArray());
        else
            _spawner.SpawnRandomAt(x, y);
    }

    private List<ElementType> GetExcludedTypes(int x, int y)
    {
        var excluded = new List<ElementType>();

        var leftType = GetMatchTypeHorizontal(x, y);
        if (leftType.HasValue)
            excluded.Add(leftType.Value);

        var bottomType = GetMatchTypeVertical(x, y);
        if (bottomType.HasValue && !excluded.Contains(bottomType.Value))
            excluded.Add(bottomType.Value);

        return excluded;
    }

    private ElementType? GetMatchTypeHorizontal(int x, int y)
    {
        if (x < 2) return null;

        var cell1 = _grid.GetCell(x - 1, y);
        var cell2 = _grid.GetCell(x - 2, y);

        if (cell1?.Element == null || cell2?.Element == null) return null;

        if (cell1.Element.Type == cell2.Element.Type)
            return cell1.Element.Type;

        return null;
    }

    private ElementType? GetMatchTypeVertical(int x, int y)
    {
        if (y < 2) return null;

        var cell1 = _grid.GetCell(x, y - 1);
        var cell2 = _grid.GetCell(x, y - 2);

        if (cell1?.Element == null || cell2?.Element == null) return null;

        if (cell1.Element.Type == cell2.Element.Type)
            return cell1.Element.Type;

        return null;
    }

    public void ClearBoard()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var cell = _grid.GetCell(x, y);
                if (cell?.Element != null)
                {
                    Destroy(cell.Element.gameObject);
                    cell.Clear();
                }
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Create Test Match (Horizontal)")]
    private void CreateTestMatchHorizontal()
    {
        ClearBoard();
        _spawner.SpawnAt(0, 0, ElementType.Red);
        _spawner.SpawnAt(1, 0, ElementType.Red);
        _spawner.SpawnAt(2, 0, ElementType.Red);
        Debug.Log("Created horizontal Red match at row 0");
    }

    [ContextMenu("Create Test Match (L-Shape)")]
    private void CreateTestMatchLShape()
    {
        ClearBoard();
        _spawner.SpawnAt(0, 0, ElementType.Blue);
        _spawner.SpawnAt(1, 0, ElementType.Blue);
        _spawner.SpawnAt(2, 0, ElementType.Blue);
        _spawner.SpawnAt(0, 1, ElementType.Blue);
        _spawner.SpawnAt(0, 2, ElementType.Blue);
        Debug.Log("Created L-shape Blue match");
    }
#endif
}
