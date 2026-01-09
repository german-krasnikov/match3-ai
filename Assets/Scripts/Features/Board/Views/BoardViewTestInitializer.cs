// Assets/Scripts/Features/Board/Views/BoardViewTestInitializer.cs
using UnityEngine;
using Configs;
using Features.Board.Models;
using Features.Board.Presenters;
using Common;

namespace Features.Board.Views
{
    /// <summary>
    /// Runtime initializer for testing BoardView.
    /// Attach to Board GameObject and run Play mode.
    /// </summary>
    public class BoardViewTestInitializer : MonoBehaviour
    {
        public GameConfig Config;

        private BoardModel _model;
        private BoardPresenter _presenter;

        private void Start()
        {
            if (Config == null)
            {
                Debug.LogError("[BoardViewTestInitializer] GameConfig is not assigned!");
                return;
            }

            var view = GetComponent<BoardView>();
            if (view == null)
            {
                Debug.LogError("[BoardViewTestInitializer] BoardView component not found!");
                return;
            }

            InitializeBoard(view);
        }

        private void InitializeBoard(BoardView view)
        {
            _model = new BoardModel(Config.GridWidth, Config.GridHeight);
            _presenter = new BoardPresenter(_model, view, Config.CellSize);

            FillBoardWithTestElements();

            Debug.Log($"[BoardViewTestInitializer] Board initialized: {Config.GridWidth}x{Config.GridHeight}");
        }

        private void FillBoardWithTestElements()
        {
            for (int x = 0; x < Config.GridWidth; x++)
            {
                for (int y = 0; y < Config.GridHeight; y++)
                {
                    var pos = new GridPosition(x, y);
                    // Cycle through element types for visual testing
                    var type = (ElementType)((x + y) % Config.ElementTypeCount + 1);
                    _model.SetElement(pos, new Element(type));
                }
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
