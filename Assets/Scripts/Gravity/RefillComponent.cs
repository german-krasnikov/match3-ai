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
        int emptyCount = CountEmptyFromTop(x);
        if (emptyCount == 0) return;

        Debug.Log($"[Refill] Column {x}: {emptyCount} empty cells");

        int topY = _grid.Height - 1;

        for (int i = 0; i < emptyCount; i++)
        {
            int targetY = topY - i;
            int spawnIndex = i + 1;

            Debug.Log($"[Refill] Spawning at x={x}, targetY={targetY}");

            var element = _spawn.SpawnRandomAt(x, targetY, useSpawnOffset: false);
            if (element == null)
            {
                Debug.LogError($"[Refill] SpawnRandomAt returned null!");
                continue;
            }

            Vector3 spawnPos = _grid.GridToWorld(x, _grid.Height);
            spawnPos.y += (spawnIndex - 1 + _config.SpawnHeightOffset) * _grid.Config.CellSize;
            element.transform.position = spawnPos;

            Debug.Log($"[Refill] Element spawned at world pos {spawnPos}, will fall to y={targetY}");

            int visualFromY = _grid.Height + spawnIndex - 1;
            fallData.Add(new FallData(element, visualFromY, targetY, x, isNew: true));
        }
    }

    private int CountEmptyFromTop(int x)
    {
        int count = 0;
        for (int y = _grid.Height - 1; y >= 0; y--)
        {
            if (_grid.GetCell(x, y).IsEmpty)
                count++;
            else
                break;
        }
        return count;
    }
}
