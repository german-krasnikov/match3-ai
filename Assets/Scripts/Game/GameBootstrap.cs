using Match3.Core;
using Match3.Grid;
using Match3.Match;
using Match3.Spawn;
using Match3.Swap;
using UnityEngine;

namespace Match3.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GridView _gridView;
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private MatchController _matchController;
        [SerializeField] private SwapController _swapController;

        private GridData _gridData;

        private void Start()
        {
            var config = _gridView.Config;
            _gridData = new GridData(config.Width, config.Height);

            _gridView.CreateVisualGrid();

            _spawnController.Initialize(_gridData);
            _matchController.Initialize(_gridData);
            _swapController.Initialize(_gridData);

            _spawnController.OnFillComplete += OnGridFilled;
            _swapController.OnSwapComplete += OnSwapComplete;

            _spawnController.FillGrid();
        }

        private void OnGridFilled()
        {
            var matches = _matchController.CheckAll();
            Debug.Log($"[Match3] Grid ready! Initial matches: {matches.Count}");
        }

        private void OnSwapComplete(GridPosition a, GridPosition b)
        {
            var matches = _matchController.CheckAt(a, b);
            Debug.Log($"[Match3] Swap {a} ↔ {b}, matches: {matches.Count}");
            _swapController.EnableInput();
        }
    }
}
