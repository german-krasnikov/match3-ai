using Match3.Grid;
using Match3.Match;
using Match3.Spawn;
using UnityEngine;

namespace Match3.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GridView _gridView;
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private MatchController _matchController;

        private GridData _gridData;

        private void Start()
        {
            var config = _gridView.Config;
            _gridData = new GridData(config.Width, config.Height);

            _gridView.CreateVisualGrid();

            _spawnController.Initialize(_gridData);
            _matchController.Initialize(_gridData);

            _spawnController.OnFillComplete += OnGridFilled;
            _spawnController.FillGrid();
        }

        private void OnGridFilled()
        {
            var matches = _matchController.CheckAll();
            Debug.Log($"[Match3] Grid filled. Initial matches: {matches.Count}");
        }
    }
}
