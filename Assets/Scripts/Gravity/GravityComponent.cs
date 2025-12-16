using System;
using System.Collections.Generic;
using UnityEngine;

public class GravityComponent : MonoBehaviour
{
    public event Action<List<FallData>> OnGravityCalculated;

    [SerializeField] private GridComponent _grid;

    public GridComponent Grid => _grid;

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

#if UNITY_EDITOR
    [ContextMenu("Test Gravity (delete random elements)")]
    private void TestGravity()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play mode");
            return;
        }

        var refill = GetComponent<RefillComponent>();
        var fallAnim = GetComponent<FallAnimationComponent>();

        // Delete 3 random elements
        for (int i = 0; i < 3; i++)
        {
            int x = UnityEngine.Random.Range(0, _grid.Width);
            int y = UnityEngine.Random.Range(0, _grid.Height - 2);
            var cell = _grid.GetCell(x, y);
            if (cell?.Element != null)
            {
                Destroy(cell.Element.gameObject);
                cell.Clear();
            }
        }

        // Process gravity
        var falls = ProcessGravity();
        var refills = refill != null ? refill.SpawnNewElements() : new List<FallData>();

        falls.AddRange(refills);

        if (fallAnim != null)
            fallAnim.AnimateFalls(falls, () => Debug.Log("Gravity test complete!"));
        else
            Debug.Log($"Gravity calculated: {falls.Count} falls");
    }
#endif
}
