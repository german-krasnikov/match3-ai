// Assets/Scripts/Features/Board/Step8Initializer.cs
using UnityEngine;
using Configs;
using Features.Board.Views;
using Features.Board.Models;
using Features.Board.Presenters;
using Features.Board.Services;
using Common;

namespace Features.Board
{
    /// <summary>
    /// Runtime initializer for testing Fall Logic.
    /// Creates a board where swaps create matches that destroy and elements fall.
    /// </summary>
    public class Step8Initializer : MonoBehaviour
    {
        public GameConfig Config;

        private BoardModel _model;
        private BoardPresenter _presenter;
        private InputService _inputService;
        private MatchService _matchService;
        private FallService _fallService;

        private void Start()
        {
            if (Config == null)
            {
                Debug.LogError("[Step8Initializer] GameConfig is not assigned!");
                return;
            }

            var view = GetComponent<BoardView>();
            if (view == null)
            {
                Debug.LogError("[Step8Initializer] BoardView component not found!");
                return;
            }

            InitializeBoard(view);
        }

        private void InitializeBoard(BoardView view)
        {
            _model = new BoardModel(Config.GridWidth, Config.GridHeight);
            _inputService = new InputService();
            _matchService = new MatchService();
            _fallService = new FallService();

            _presenter = new BoardPresenter(
                _model,
                view,
                1f,
                _matchService,
                _inputService,
                _fallService);

            _presenter.OnMatchesDestroyed += HandleMatchesDestroyed;
            _presenter.OnFallComplete += HandleFallComplete;

            view.OnSwapAttempted += HandleSwapAttempted;

            FillBoardForTesting();

            Debug.Log("[Step8Initializer] Board initialized for fall testing.");
            Debug.Log("[Step8Initializer] Swap elements to create matches.");
            Debug.Log("[Step8Initializer] Watch elements fall after destruction.");
        }

        private void HandleSwapAttempted(GridPosition from, GridPosition to)
        {
            Debug.Log($"[View] Swap attempted: {from} -> {to}");
            _inputService.RequestSwap(from, to);
        }

        private void HandleMatchesDestroyed()
        {
            Debug.Log("[Step8Initializer] Matches destroyed! Elements will fall...");
        }

        private void HandleFallComplete()
        {
            Debug.Log("[Step8Initializer] Fall complete!");
        }

        private void FillBoardForTesting()
        {
            for (int x = 0; x < Config.GridWidth; x++)
            {
                for (int y = 0; y < Config.GridHeight; y++)
                {
                    var pos = new GridPosition(x, y);
                    var type = (ElementType)(((x + y) % 2) + 1);
                    _model.SetElement(pos, new Element(type));
                }
            }

            // Setup: Row 0: Red, Red, Blue, Red
            // Swap (2,0) <-> (3,0) -> Red, Red, Red, Blue -> Match!
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(1, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(2, 0), new Element(ElementType.Blue));
            _model.SetElement(new GridPosition(3, 0), new Element(ElementType.Red));

            // Elements above that will fall
            _model.SetElement(new GridPosition(0, 1), new Element(ElementType.Green));
            _model.SetElement(new GridPosition(0, 2), new Element(ElementType.Yellow));
            _model.SetElement(new GridPosition(1, 1), new Element(ElementType.Purple));
            _model.SetElement(new GridPosition(1, 2), new Element(ElementType.Green));
            _model.SetElement(new GridPosition(2, 1), new Element(ElementType.Yellow));
            _model.SetElement(new GridPosition(2, 2), new Element(ElementType.Purple));

            Debug.Log("[Step8] Test: Swap (2,0) <-> (3,0) to create match and see fall");
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.OnMatchesDestroyed -= HandleMatchesDestroyed;
                _presenter.OnFallComplete -= HandleFallComplete;
                _presenter.Dispose();
            }

            var view = GetComponent<BoardView>();
            if (view != null)
            {
                view.OnSwapAttempted -= HandleSwapAttempted;
            }
        }
    }
}
