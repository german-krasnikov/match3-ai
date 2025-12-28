using System;
using UnityEngine;
using Match3.Core;
using Match3.Grid;

namespace Match3.Swap
{
    public class InputComponent : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapRequested;
        public event Action<Vector2Int> OnCellSelected;
        public event Action OnSelectionCleared;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private Camera _camera;

        private Vector2Int? _selectedCell;
        private bool _inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled) ClearSelection();
        }

        public void ClearSelection()
        {
            if (_selectedCell == null) return;
            _selectedCell = null;
            OnSelectionCleared?.Invoke();
        }

        private void Update()
        {
            if (!_inputEnabled) return;
            if (!Input.GetMouseButtonDown(0)) return;

            HandleClick();
        }

        private void HandleClick()
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = _grid.WorldToGrid(worldPos);

            if (!_grid.IsValidPosition(gridPos)) return;
            if (_grid.GetElementAt(gridPos) == null) return;

            ProcessSelection(gridPos);
        }

        private void ProcessSelection(Vector2Int gridPos)
        {
            if (_selectedCell == null)
            {
                _selectedCell = gridPos;
                OnCellSelected?.Invoke(gridPos);
            }
            else if (_selectedCell == gridPos)
            {
                ClearSelection();
            }
            else
            {
                OnSwapRequested?.Invoke(_selectedCell.Value, gridPos);
                ClearSelection();
            }
        }
    }
}
