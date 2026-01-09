// Assets/Scripts/Core/GameEntryPoint.cs
using UnityEngine;
using Configs;
using Common;
using Features.Board.Models;
using Features.Board.Views;
using Features.Board.Services;
using Gameplay;

namespace Core
{
    /// <summary>
    /// Game bootstrap - creates and wires all components.
    /// Script Execution Order: -100
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameEntryPoint : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;

        [Header("Views")]
        [SerializeField] private BoardView _boardView;

        private ServiceLocator _locator;
        private BoardModel _boardModel;
        private GameplayCoordinator _coordinator;
        private IInputService _inputService;
        private IMatchService _matchService;
        private IFallService _fallService;
        private ISpawnService _spawnService;

        private void Awake()
        {
            SetupServices();
            SetupGame();
        }

        private void SetupServices()
        {
            _locator = new ServiceLocator();
            Services.Initialize(_locator);

            _spawnService = new SpawnService(_config.ElementTypeCount);
            _matchService = new MatchService();
            _fallService = new FallService();
            _inputService = new InputService();

            _locator.Register<ISpawnService>(_spawnService);
            _locator.Register<IMatchService>(_matchService);
            _locator.Register<IFallService>(_fallService);
            _locator.Register<IInputService>(_inputService);
            _locator.Register<GameConfig>(_config);
        }

        private void SetupGame()
        {
            // Create model
            _boardModel = new BoardModel(_config.GridWidth, _config.GridHeight);

            // Initialize view
            _boardView.SetConfig(_config);
            _boardView.Initialize(_config.GridWidth, _config.GridHeight, _config.CellSize);

            // Wire view events to input service
            _boardView.OnSwapAttempted += (from, to) => _inputService.RequestSwap(from, to);

            // Create coordinator
            _coordinator = new GameplayCoordinator(
                _boardModel,
                _boardView,
                _inputService,
                _matchService,
                _fallService,
                _spawnService);

            // Fill initial board
            FillBoard();
        }

        private void FillBoard()
        {
            for (int y = 0; y < _config.GridHeight; y++)
            {
                for (int x = 0; x < _config.GridWidth; x++)
                {
                    var pos = new GridPosition(x, y);
                    var type = GetSafeElementType(pos);
                    var element = new Element(type);
                    _boardModel.SetElement(pos, element);
                    _boardView.CreateElement(pos, type);
                }
            }
        }

        private ElementType GetSafeElementType(GridPosition pos)
        {
            // Avoid initial matches
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var type = _spawnService.GetRandomElement();
                if (!_matchService.WouldCreateMatch(_boardModel, pos, type))
                    return type;
            }
            return _spawnService.GetRandomElement();
        }

        private void OnDestroy()
        {
            _coordinator?.Dispose();
            Services.Reset();
        }
    }
}
