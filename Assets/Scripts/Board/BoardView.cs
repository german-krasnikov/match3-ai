using System;
using UnityEngine;
using Match3.Gem;
using Match3.Grid;
using Match3.Spawn;

namespace Match3.Board
{
    public class BoardView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private GemConfig _gemConfig;
        [SerializeField] private GemView _gemPrefab;

        [Header("Parents")]
        [SerializeField] private Transform _gemsParent;

        private BoardData _boardData;
        private SpawnSystem _spawnSystem;
        private GemView[,] _views;
        private GridData _gridData;

        public BoardData Data => _boardData;

        /// <summary>
        /// Fires when initial board fill is complete.
        /// </summary>
        public event Action OnBoardReady;

        private void Start()
        {
            Initialize();
            FillBoard();
        }

        private void OnEnable()
        {
            if (_boardData != null)
            {
                _boardData.OnGemAdded += HandleGemAdded;
                _boardData.OnGemRemoved += HandleGemRemoved;
            }
        }

        private void OnDisable()
        {
            if (_boardData != null)
            {
                _boardData.OnGemAdded -= HandleGemAdded;
                _boardData.OnGemRemoved -= HandleGemRemoved;
            }
        }

        private void Initialize()
        {
            _gridData = _gridView.Data;
            var size = _gridData.Size;

            _boardData = new BoardData(size);
            _spawnSystem = new SpawnSystem(_gemConfig);
            _views = new GemView[size.x, size.y];

            // Subscribe after creating BoardData
            _boardData.OnGemAdded += HandleGemAdded;
            _boardData.OnGemRemoved += HandleGemRemoved;
        }

        /// <summary>
        /// Fills entire board with gems (initial fill).
        /// </summary>
        private void FillBoard()
        {
            var size = _gridData.Size;

            // Fill from bottom-left to top-right
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var pos = new Vector2Int(x, y);
                    SpawnGemAt(pos);
                }
            }

            OnBoardReady?.Invoke();
        }

        /// <summary>
        /// Spawns gem at position using SpawnSystem for type.
        /// </summary>
        public void SpawnGemAt(Vector2Int pos)
        {
            var type = _spawnSystem.GenerateType(pos, _boardData);
            var gem = new GemData(type, pos);
            _boardData.SetGem(pos, gem);
        }

        /// <summary>
        /// Returns GemView at position or null.
        /// </summary>
        public GemView GetView(Vector2Int pos)
        {
            if (!_boardData.IsValidPosition(pos))
                return null;
            return _views[pos.x, pos.y];
        }

        /// <summary>
        /// Creates GemView at position (called manually, not via event).
        /// Used for fall system when spawning from above.
        /// </summary>
        public void CreateGem(Vector2Int pos, GemData gem)
        {
            var worldPos = _gridData.GridToWorld(pos);
            CreateGemView(pos, gem, worldPos);
        }

        /// <summary>
        /// Creates GemView at spawn position above grid, then BoardView tracks it.
        /// Used for fall system.
        /// </summary>
        public GemView CreateGemAbove(int column, int rowsAbove, GemData gem)
        {
            var spawnPos = _gridData.GetSpawnPosition(column, rowsAbove);
            var view = InstantiateGemView(gem, spawnPos);
            // Note: Don't add to _views array - will be added when gem lands
            return view;
        }

        /// <summary>
        /// Destroys GemView at position (called manually, not via event).
        /// </summary>
        public void DestroyGem(Vector2Int pos)
        {
            var view = GetView(pos);
            if (view != null)
            {
                _views[pos.x, pos.y] = null;
                Destroy(view.gameObject);
            }
        }

        /// <summary>
        /// Updates view reference when gem moves to new position.
        /// </summary>
        public void UpdateViewPosition(Vector2Int from, Vector2Int to)
        {
            var view = _views[from.x, from.y];
            if (view == null)
                return;

            _views[from.x, from.y] = null;
            _views[to.x, to.y] = view;
            view.SetGridPosition(to);
        }

        /// <summary>
        /// Registers view at position (for gems spawned above grid).
        /// </summary>
        public void RegisterView(Vector2Int pos, GemView view)
        {
            if (_boardData.IsValidPosition(pos))
            {
                _views[pos.x, pos.y] = view;
                view.SetGridPosition(pos);
            }
        }

        /// <summary>
        /// Swaps view references at two positions.
        /// </summary>
        public void SwapViews(Vector2Int a, Vector2Int b)
        {
            var viewA = _views[a.x, a.y];
            var viewB = _views[b.x, b.y];

            _views[a.x, a.y] = viewB;
            _views[b.x, b.y] = viewA;

            if (viewA != null) viewA.SetGridPosition(b);
            if (viewB != null) viewB.SetGridPosition(a);
        }

        // --- Event Handlers ---

        private void HandleGemAdded(Vector2Int pos, GemData gem)
        {
            var worldPos = _gridData.GridToWorld(pos);
            CreateGemView(pos, gem, worldPos);
        }

        private void HandleGemRemoved(Vector2Int pos)
        {
            DestroyGem(pos);
        }

        // --- Private Helpers ---

        private void CreateGemView(Vector2Int pos, GemData gem, Vector3 worldPos)
        {
            var view = InstantiateGemView(gem, worldPos);
            view.SetGridPosition(pos);
            _views[pos.x, pos.y] = view;
        }

        private GemView InstantiateGemView(GemData gem, Vector3 worldPos)
        {
            Transform parent = _gemsParent != null ? _gemsParent : transform;
            var view = Instantiate(_gemPrefab, worldPos, Quaternion.identity, parent);
            view.Setup(gem.Type, _gemConfig);
            view.name = $"Gem_{gem.Type}_{gem.Position.x}_{gem.Position.y}";
            return view;
        }
    }
}
