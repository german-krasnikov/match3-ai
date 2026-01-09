// Assets/Scripts/Features/Board/Step6Initializer.cs
using UnityEngine;
using Configs;
using Features.Board.Views;
using Features.Board.Models;
using Features.Board.Presenters;
using Features.Board.Services;
using Common;

namespace Features.Board
{
    public class Step6Initializer : MonoBehaviour
    {
        public GameConfig Config;

        private BoardModel _model;
        private BoardPresenter _presenter;
        private InputService _inputService;
        private MatchService _matchService;

        private void Start()
        {
            if (Config == null)
            {
                Debug.LogError("[Step6Initializer] GameConfig is not assigned!");
                return;
            }

            var view = GetComponent<BoardView>();
            if (view == null)
            {
                Debug.LogError("[Step6Initializer] BoardView component not found!");
                return;
            }

            InitializeBoard(view);
        }

        private void InitializeBoard(BoardView view)
        {
            _model = new BoardModel(Config.GridWidth, Config.GridHeight);
            _inputService = new InputService();
            _matchService = new MatchService();

            _presenter = new BoardPresenter(
                _model,
                view,
                Config.CellSize,
                _matchService,
                _inputService);

            view.OnSwapAttempted += HandleSwapAttempted;
            FillBoardWithoutMatches();
            SetupTestScenarios();

            Debug.Log("[Step6] Board ready. Test swaps: (2,0)<->(3,0) or (0,1)<->(1,1)");
        }

        private void HandleSwapAttempted(GridPosition from, GridPosition to)
        {
            _inputService.RequestSwap(from, to);
        }

        private void FillBoardWithoutMatches()
        {
            // Use all colors with pattern that avoids initial matches
            for (int y = 0; y < Config.GridHeight; y++)
            {
                for (int x = 0; x < Config.GridWidth; x++)
                {
                    var pos = new GridPosition(x, y);
                    var type = (ElementType)((x + y) % Config.ElementTypeCount + 1);
                    _model.SetElement(pos, new Element(type));
                }
            }
        }

        private void SetupTestScenarios()
        {
            // Horizontal match: swap (2,0) <-> (3,0) creates Red-Red-Red
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(1, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(2, 0), new Element(ElementType.Blue));
            _model.SetElement(new GridPosition(3, 0), new Element(ElementType.Red));

            // Vertical match: swap (0,1) <-> (1,1) creates Red column
            _model.SetElement(new GridPosition(0, 1), new Element(ElementType.Blue));
            _model.SetElement(new GridPosition(0, 2), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(0, 3), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(1, 1), new Element(ElementType.Red));
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            var view = GetComponent<BoardView>();
            if (view != null)
                view.OnSwapAttempted -= HandleSwapAttempted;
        }
    }
}
