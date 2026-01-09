// Assets/Scripts/Features/Board/Step9Initializer.cs
using UnityEngine;
using Configs;
using Features.Board.Views;
using Features.Board.Models;
using Features.Board.Presenters;
using Features.Board.Services;
using Common;

namespace Features.Board
{
    public class Step9Initializer : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        private BoardModel _model;
        private BoardPresenter _presenter;
        private InputService _inputService;
        private MatchService _matchService;
        private FallService _fallService;
        private SpawnService _spawnService;

        private void Start()
        {
            _config = Resources.FindObjectsOfTypeAll<GameConfig>()[0];
            var view = GetComponent<BoardView>();

            if (_config == null || view == null)
            {
                Debug.LogError("[Step9] Missing config or view!");
                return;
            }

            Initialize(view);
        }

        private void Initialize(BoardView view)
        {
            _model = new BoardModel(_config.GridWidth, _config.GridHeight);
            _inputService = new InputService();
            _matchService = new MatchService();
            _fallService = new FallService();
            _spawnService = new SpawnService(_config.ElementTypeCount);

            _presenter = new BoardPresenter(
                _model, view, 1f,
                _matchService, _inputService, _fallService, _spawnService);

            _presenter.OnMatchesDestroyed += () => Debug.Log("[Cascade] Matches destroyed");
            _presenter.OnFallComplete += () => Debug.Log("[Cascade] Fall complete");
            _presenter.OnRefillComplete += () => Debug.Log("[Cascade] Refill complete");
            _presenter.OnCascadeComplete += () => Debug.Log("[Cascade] Complete - ready for input");

            view.OnSwapAttempted += (from, to) => _inputService.RequestSwap(from, to);

            FillBoard();
            Debug.Log("[Step9] Ready. Swap (2,0)<->(3,0) for cascade test.");
        }

        private void FillBoard()
        {
            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var pos = new GridPosition(x, y);
                    var type = (ElementType)((x + y * 2) % _config.ElementTypeCount + 1);
                    _model.SetElement(pos, new Element(type));
                }
            }

            // Setup cascade test scenario: R R B R at bottom
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(1, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(2, 0), new Element(ElementType.Blue));
            _model.SetElement(new GridPosition(3, 0), new Element(ElementType.Red));
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
