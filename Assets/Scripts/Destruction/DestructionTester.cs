using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;
using Match3.Spawn;

namespace Match3.Destruction
{
    public class DestructionTester : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DestructionComponent _destruction;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SpawnComponent _spawn;

        [Header("Test Settings")]
        [SerializeField] private List<Vector2Int> _testPositions = new()
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 1),
            new Vector2Int(2, 2),
            new Vector2Int(3, 3),
            new Vector2Int(4, 4)
        };

        private void OnEnable()
        {
            if (_destruction != null)
            {
                _destruction.OnDestructionStarted += OnDestructionStarted;
                _destruction.OnDestructionCompleted += OnDestructionCompleted;
            }
        }

        private void OnDisable()
        {
            if (_destruction != null)
            {
                _destruction.OnDestructionStarted -= OnDestructionStarted;
                _destruction.OnDestructionCompleted -= OnDestructionCompleted;
            }
        }

        [ContextMenu("1. Fill Grid")]
        private void FillGrid()
        {
            if (_spawn == null)
            {
                Debug.LogError("[DestructionTester] SpawnComponent not assigned!");
                return;
            }

            _spawn.FillGrid();
            Debug.Log("[DestructionTester] Grid filled");
        }

        [ContextMenu("2. Destroy Test Positions")]
        private async void DestroyTestPositions()
        {
            if (_destruction == null)
            {
                Debug.LogError("[DestructionTester] DestructionComponent not assigned!");
                return;
            }

            Debug.Log($"[DestructionTester] Destroying {_testPositions.Count} elements...");
            await _destruction.DestroyElements(_testPositions);
            Debug.Log("[DestructionTester] Done");
        }

        [ContextMenu("3. Destroy Random Row")]
        private async void DestroyRandomRow()
        {
            if (_destruction == null || _grid == null)
                return;

            int row = Random.Range(0, _grid.Height);
            var positions = new List<Vector2Int>();

            for (int x = 0; x < _grid.Width; x++)
            {
                positions.Add(new Vector2Int(x, row));
            }

            Debug.Log($"[DestructionTester] Destroying row {row}...");
            await _destruction.DestroyElements(positions);
        }

        [ContextMenu("4. Destroy Random Column")]
        private async void DestroyRandomColumn()
        {
            if (_destruction == null || _grid == null)
                return;

            int col = Random.Range(0, _grid.Width);
            var positions = new List<Vector2Int>();

            for (int y = 0; y < _grid.Height; y++)
            {
                positions.Add(new Vector2Int(col, y));
            }

            Debug.Log($"[DestructionTester] Destroying column {col}...");
            await _destruction.DestroyElements(positions);
        }

        private void OnDestructionStarted(List<Vector2Int> positions)
        {
            Debug.Log($"[Event] Destruction started: {positions.Count} elements");
        }

        private void OnDestructionCompleted(List<Vector2Int> positions)
        {
            Debug.Log($"[Event] Destruction completed: {positions.Count} elements");
        }
    }
}
