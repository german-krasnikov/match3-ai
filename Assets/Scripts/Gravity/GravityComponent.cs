using System;
using System.Collections.Generic;
using UnityEngine;

public class GravityComponent : MonoBehaviour
{
    public event Action<List<FallData>> OnGravityCalculated;

    [SerializeField] private GridComponent _grid;

    public List<FallData> ProcessGravity()
    {
        var fallData = new List<FallData>();

        for (int x = 0; x < _grid.Width; x++)
        {
            ProcessColumn(x, fallData);
        }

        OnGravityCalculated?.Invoke(fallData);
        return fallData;
    }

    private void ProcessColumn(int x, List<FallData> fallData)
    {
        int writeIndex = 0;

        for (int y = 0; y < _grid.Height; y++)
        {
            var cell = _grid.GetCell(x, y);
            if (cell.IsEmpty) continue;

            if (y != writeIndex)
            {
                var element = cell.Element;
                var targetCell = _grid.GetCell(x, writeIndex);

                cell.Clear();
                targetCell.Element = element;
                element.SetGridPosition(x, writeIndex);

                fallData.Add(new FallData(element, y, writeIndex, x));
            }

            writeIndex++;
        }
    }
}
