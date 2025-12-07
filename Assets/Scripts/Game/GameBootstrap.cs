using Match3.Destruction;
using Match3.GameLoop;
using Match3.Gravity;
using Match3.Grid;
using Match3.Match;
using Match3.Spawn;
using Match3.Swap;
using UnityEngine;

namespace Match3.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private GridView _gridView;

        [Header("Controllers")]
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private MatchController _matchController;
        [SerializeField] private SwapController _swapController;
        [SerializeField] private DestructionController _destructionController;
        [SerializeField] private GravityController _gravityController;
        [SerializeField] private GameStateMachine _stateMachine;

        private GridData _gridData;

        private void Start()
        {
            InitializeGrid();
            InitializeControllers();
            StartGame();
        }

        private void InitializeGrid()
        {
            var config = _gridView.Config;
            _gridData = new GridData(config.Width, config.Height);
            _gridView.CreateVisualGrid();
        }

        private void InitializeControllers()
        {
            _spawnController.Initialize(_gridData);
            _matchController.Initialize(_gridData);
            _swapController.Initialize(_gridData);
            _destructionController.Initialize(_gridData);
            _gravityController.Initialize(_gridData);
            _stateMachine.Initialize(_gridData);

            _stateMachine.OnGameOver += OnGameOver;
        }

        private void StartGame()
        {
            _spawnController.FillGrid();
            Debug.Log("[Match3] Game started!");
        }

        private void OnGameOver()
        {
            Debug.Log("[Match3] GAME OVER - No possible moves!");
        }
    }
}
