using UnityEngine;

public class SpawnComponent : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactory _factory;

    [Header("Spawn Settings")]
    [Tooltip("Смещение по Y для спауна (для анимации падения)")]
    [SerializeField] private float _spawnHeightOffset = 5f;

    public ElementComponent SpawnAt(int x, int y, ElementType type, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
            worldPos.y += _spawnHeightOffset;

        var element = _factory.Create(type, worldPos, x, y);
        cell.Element = element;

        return element;
    }

    public ElementComponent SpawnRandomAt(int x, int y, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
            worldPos.y += _spawnHeightOffset;

        var element = _factory.CreateRandom(worldPos, x, y);
        cell.Element = element;

        return element;
    }

    public ElementComponent SpawnRandomExcludingAt(int x, int y, ElementType[] excluded, bool useSpawnOffset = false)
    {
        var cell = _grid.GetCell(x, y);
        if (cell == null) return null;

        Vector3 worldPos = _grid.GridToWorld(x, y);
        if (useSpawnOffset)
            worldPos.y += _spawnHeightOffset;

        var element = _factory.CreateRandomExcluding(worldPos, x, y, excluded);
        cell.Element = element;

        return element;
    }

    public Vector3 GetSpawnPosition(int x)
    {
        return _grid.GridToWorld(x, _grid.Height) + Vector3.up * _spawnHeightOffset;
    }
}
