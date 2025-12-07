using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Data;

namespace Match3.Components.Board
{
    public class TileSpawner : MonoBehaviour
    {
        public event Action<TileComponent> OnTileSpawned;

        [Header("Settings")]
        [SerializeField] private TileData[] _availableTiles;
        [SerializeField] private TileComponent _tilePrefab;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private Transform _tileContainer;

        public void SetAvailableTiles(TileData[] tiles) => _availableTiles = tiles;

        public TileComponent SpawnTile(Vector2Int position)
        {
            var data = GetRandomTileData();
            return SpawnTile(position, data);
        }

        public TileComponent SpawnTile(Vector2Int position, TileData data)
        {
            var worldPos = _grid.GridToWorld(position);
            var tile = Instantiate(_tilePrefab, worldPos, Quaternion.identity, _tileContainer);

            tile.Initialize(data.type, data, position);
            tile.name = $"Tile_{data.type}_{position.x}_{position.y}";

            OnTileSpawned?.Invoke(tile);
            return tile;
        }

        public TileComponent SpawnTileWithoutMatch(Vector2Int position, Func<Vector2Int, TileType, bool> wouldMatch)
        {
            var availableTypes = new List<TileData>();

            foreach (var data in _availableTiles)
            {
                if (!wouldMatch(position, data.type))
                    availableTypes.Add(data);
            }

            if (availableTypes.Count == 0)
                return SpawnTile(position);

            var randomData = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
            return SpawnTile(position, randomData);
        }

        private TileData GetRandomTileData()
        {
            return _availableTiles[UnityEngine.Random.Range(0, _availableTiles.Length)];
        }

        public TileData GetTileData(TileType type)
        {
            foreach (var data in _availableTiles)
            {
                if (data.type == type)
                    return data;
            }
            return _availableTiles.Length > 0 ? _availableTiles[0] : null;
        }
    }
}
