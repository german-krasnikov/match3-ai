using System;
using System.Collections.Generic;
using UnityEngine;

public class RefillComponent : MonoBehaviour
{
    public event Action<List<FallData>> OnRefillCalculated;

    [SerializeField] private GridComponent _grid;
    [SerializeField] private SpawnComponent _spawn;
    [SerializeField] private GravityConfig _config;

    public List<FallData> SpawnNewElements()
    {
        var fallData = new List<FallData>();

        for (int x = 0; x < _grid.Width; x++)
        {
            SpawnColumnElements(x, fallData);
        }

        OnRefillCalculated?.Invoke(fallData);
        return fallData;
    }

    private void SpawnColumnElements(int x, List<FallData> fallData)
    {
        var emptyIndices = GetEmptyIndicesFromTop(x);
        if (emptyIndices.Count == 0) return;

        for (int i = 0; i < emptyIndices.Count; i++)
        {
            int targetY = emptyIndices[i];
            int spawnOffset = emptyIndices.Count - i;

            var element = _spawn.SpawnRandomAt(x, targetY, useSpawnOffset: false);

            Vector3 spawnPos = _grid.GridToWorld(x, targetY);
            spawnPos.y += (spawnOffset + _config.SpawnHeightOffset) * _grid.Config.CellSize;
            element.transform.position = spawnPos;

            int fromY = targetY + spawnOffset + Mathf.RoundToInt(_config.SpawnHeightOffset);
            fallData.Add(new FallData(element, fromY, targetY, x, isNew: true));
        }
    }

    private List<int> GetEmptyIndicesFromTop(int x)
    {
        var emptyIndices = new List<int>();

        for (int y = _grid.Height - 1; y >= 0; y--)
        {
            if (_grid.GetCell(x, y).IsEmpty)
                emptyIndices.Add(y);
            else
                break;
        }

        return emptyIndices;
    }
}
