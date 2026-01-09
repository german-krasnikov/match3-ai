// Assets/Scripts/Features/Board/Step5Initializer.cs
using UnityEngine;
using Configs;
using Features.Board.Views;
using Features.Board.Models;
using Features.Board.Presenters;
using Features.Board.Services;
using Common;

namespace Features.Board
{
    public class Step5Initializer : MonoBehaviour
    {
        public GameConfig Config;

        private BoardModel _model;
        private BoardPresenter _presenter;
        private InputService _inputService;

        private void Start()
        {
            if (Config == null)
            {
                Debug.LogError("[Step5] GameConfig is not assigned!");
                return;
            }

            var view = GetComponent<BoardView>();
            if (view == null)
            {
                Debug.LogError("[Step5] BoardView component not found!");
                return;
            }

            InitializeBoard(view);
        }

        private void InitializeBoard(BoardView view)
        {
            _model = new BoardModel(Config.GridWidth, Config.GridHeight);
            _presenter = new BoardPresenter(_model, view, Config.CellSize);
            _inputService = new InputService();

            view.OnSwapAttempted += HandleSwapAttempted;
            _inputService.OnSwapRequested += HandleSwapRequested;

            FillBoardWithTestElements();

            Debug.Log($"[Step5] Board initialized {Config.GridWidth}x{Config.GridHeight}. Drag elements to test!");
        }

        private void HandleSwapAttempted(GridPosition from, GridPosition to)
        {
            Debug.Log($"[BoardView] Swap attempted: ({from.X},{from.Y}) -> ({to.X},{to.Y})");
            _inputService.RequestSwap(from, to);
        }

        private void HandleSwapRequested(GridPosition from, GridPosition to)
        {
            Debug.Log($"<color=green>[InputService] VALID SWAP: ({from.X},{from.Y}) -> ({to.X},{to.Y})</color>");
        }

        private void FillBoardWithTestElements()
        {
            for (int x = 0; x < Config.GridWidth; x++)
            {
                for (int y = 0; y < Config.GridHeight; y++)
                {
                    var pos = new GridPosition(x, y);
                    var type = (ElementType)((x + y) % Config.ElementTypeCount + 1);
                    _model.SetElement(pos, new Element(type));
                }
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();

            var view = GetComponent<BoardView>();
            if (view != null)
                view.OnSwapAttempted -= HandleSwapAttempted;

            if (_inputService != null)
                _inputService.OnSwapRequested -= HandleSwapRequested;
        }
    }
}
