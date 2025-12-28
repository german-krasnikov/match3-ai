using System;
using UnityEngine;
using Match3.Core;
using Match3.Grid;

namespace Match3.Swap
{
    public class InputComponent : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapRequested;
        public event Action<Vector2Int> OnDragStarted;
        public event Action OnDragCanceled;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private Camera _camera;

        [Header("Settings")]
        [SerializeField] private float _minDragDistance = 0.3f;

        private Vector2Int? _dragStartCell;
        private Vector3 _dragStartWorldPos;
        private bool _isDragging;
        private bool _inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled) CancelDrag();
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            if (Input.GetMouseButtonDown(0))
                HandleDragStart();
            else if (Input.GetMouseButtonUp(0) && _isDragging)
                HandleDragEnd();
        }

        private void HandleDragStart()
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = _grid.WorldToGrid(worldPos);

            if (!_grid.IsValidPosition(gridPos)) return;
            if (_grid.GetElementAt(gridPos) == null) return;

            _dragStartCell = gridPos;
            _dragStartWorldPos = worldPos;
            _isDragging = true;
            OnDragStarted?.Invoke(gridPos);
        }

        private void HandleDragEnd()
        {
            if (_dragStartCell == null)
            {
                CancelDrag();
                return;
            }

            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 delta = worldPos - _dragStartWorldPos;

            if (delta.magnitude < _minDragDistance)
            {
                CancelDrag();
                return;
            }

            Vector2Int direction = GetSwipeDirection(delta);
            Vector2Int targetCell = _dragStartCell.Value + direction;

            if (_grid.IsValidPosition(targetCell) && _grid.GetElementAt(targetCell) != null)
            {
                OnSwapRequested?.Invoke(_dragStartCell.Value, targetCell);
            }

            CancelDrag();
        }

        private Vector2Int GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        private void CancelDrag()
        {
            if (_isDragging)
                OnDragCanceled?.Invoke();

            _dragStartCell = null;
            _isDragging = false;
        }
    }
}
