using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;
using Match3.Elements;
using Match3.Board;

namespace Match3.Spawn
{
    public class InitialBoardSpawner : MonoBehaviour
    {
        public event Action OnSpawnCompleted;

        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactory _factory;
        [SerializeField] private BoardComponent _board;
        [SerializeField] private bool _spawnOnStart = true;

        private ElementComponent[,] _spawnedElements;

        public ElementComponent[,] SpawnedElements => _spawnedElements;

        private void Start()
        {
            if (_spawnOnStart)
                SpawnInitialBoard();
        }

        public void SpawnInitialBoard()
        {
            _spawnedElements = new ElementComponent[_grid.Width, _grid.Height];

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    SpawnAt(x, y);
                }
            }

            if (_board != null)
                _board.Initialize(_spawnedElements);

            OnSpawnCompleted?.Invoke();
        }

        private void SpawnAt(int x, int y)
        {
            var gridPos = new Vector2Int(x, y);
            var worldPos = _grid.GridToWorld(gridPos);
            var excluded = GetExcludedTypes(x, y);

            var element = excluded.Count > 0
                ? _factory.CreateRandomExcluding(worldPos, gridPos, excluded.ToArray())
                : _factory.CreateRandom(worldPos, gridPos);

            _spawnedElements[x, y] = element;
        }

        private List<ElementType> GetExcludedTypes(int x, int y)
        {
            var excluded = new List<ElementType>(2);

            // Check 2 left: if same type, exclude it
            if (x >= 2)
            {
                var left1 = _spawnedElements[x - 1, y];
                var left2 = _spawnedElements[x - 2, y];
                if (left1 != null && left2 != null && left1.Type == left2.Type)
                    excluded.Add(left1.Type);
            }

            // Check 2 below: if same type, exclude it
            if (y >= 2)
            {
                var down1 = _spawnedElements[x, y - 1];
                var down2 = _spawnedElements[x, y - 2];
                if (down1 != null && down2 != null && down1.Type == down2.Type)
                    excluded.Add(down1.Type);
            }

            return excluded;
        }

#if UNITY_EDITOR
        [ContextMenu("Validate No Matches")]
        private void ValidateNoMatches()
        {
            if (_spawnedElements == null)
            {
                Debug.LogWarning("No spawned elements to validate");
                return;
            }

            int matchCount = 0;
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    var current = _spawnedElements[x, y];
                    if (current == null) continue;

                    if (x >= 2)
                    {
                        var a = _spawnedElements[x - 1, y];
                        var b = _spawnedElements[x - 2, y];
                        if (a?.Type == current.Type && b?.Type == current.Type)
                        {
                            Debug.LogError($"Horizontal match at ({x},{y}): {current.Type}");
                            matchCount++;
                        }
                    }

                    if (y >= 2)
                    {
                        var a = _spawnedElements[x, y - 1];
                        var b = _spawnedElements[x, y - 2];
                        if (a?.Type == current.Type && b?.Type == current.Type)
                        {
                            Debug.LogError($"Vertical match at ({x},{y}): {current.Type}");
                            matchCount++;
                        }
                    }
                }
            }

            Debug.Log(matchCount == 0 ? "Validation passed: no matches" : $"Found {matchCount} matches!");
        }
#endif
    }
}
