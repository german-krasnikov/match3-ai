using UnityEngine;
using Match3.Grid;

namespace Match3.Elements
{
    /// <summary>
    /// Temporary test spawner. Delete after Stage 3 implementation.
    /// </summary>
    public class ElementSpawnerTest : MonoBehaviour
    {
        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementDatabase _database;
        [SerializeField] private ElementComponent _elementPrefab;

        private void Start()
        {
            if (_grid == null || _database == null || _elementPrefab == null)
            {
                Debug.LogError("[ElementSpawnerTest] Missing references!");
                return;
            }

            SpawnAll();
        }

        private void SpawnAll()
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var worldPos = _grid.GridToWorld(pos);
                    var data = _database.GetRandom();

                    var element = Instantiate(_elementPrefab, worldPos, Quaternion.identity, transform);
                    element.Initialize(data, pos);
                }
            }
        }
    }
}
