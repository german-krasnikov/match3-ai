using System;
using UnityEngine;
using Match3.Grid;
using Match3.Board;
using UnityInput = UnityEngine.Input;

namespace Match3.Input
{
    /// <summary>
    /// Handles player input and publishes swap requests.
    /// Supports both click-click and click-swipe patterns.
    /// </summary>
    public class InputDetector : MonoBehaviour
    {
        // === EVENTS ===

        public event Action<Vector2Int> OnElementSelected;
        public event Action OnSelectionCancelled;
        public event Action<Vector2Int, Vector2Int> OnSwapRequested;

        // === SETTINGS ===

        [Header("Settings")]
        [SerializeField] private float _swipeThreshold = 0.5f;
        [SerializeField] private Camera _camera;

        // === DEPENDENCIES ===

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private BoardComponent _board;
        [SerializeField] private InputBlocker _inputBlocker;

        // === PRIVATE FIELDS ===

        private Vector2Int? _selectedPosition;
        private Vector3 _pointerDownPosition;
        private bool _isDragging;

        // === PUBLIC PROPERTIES ===

        public Vector2Int? SelectedPosition => _selectedPosition;
        public bool HasSelection => _selectedPosition.HasValue;

        // === UNITY CALLBACKS ===

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            if (_inputBlocker != null && _inputBlocker.IsBlocked)
                return;

            HandleInput();
        }

        // === PUBLIC METHODS ===

        public void ClearSelection()
        {
            if (!_selectedPosition.HasValue) return;

            _selectedPosition = null;
            OnSelectionCancelled?.Invoke();
        }

        // === PRIVATE METHODS ===

        private void HandleInput()
        {
            if (UnityInput.GetMouseButtonDown(0))
            {
                _pointerDownPosition = GetPointerWorldPosition();
                _isDragging = false;

                var gridPos = _grid.WorldToGrid(_pointerDownPosition);

                if (!_grid.IsValidPosition(gridPos))
                {
                    ClearSelection();
                    return;
                }

                if (_board.IsEmpty(gridPos))
                    return;

                HandlePointerDown(gridPos);
            }

            if (UnityInput.GetMouseButton(0) && _selectedPosition.HasValue && !_isDragging)
            {
                Vector3 currentPos = GetPointerWorldPosition();
                Vector2 delta = currentPos - _pointerDownPosition;

                var direction = SwipeDirectionExtensions.FromDelta(delta, _swipeThreshold);

                if (direction != SwipeDirection.None)
                {
                    _isDragging = true;
                    HandleSwipe(direction);
                }
            }
        }

        private void HandlePointerDown(Vector2Int gridPos)
        {
            if (!_selectedPosition.HasValue)
            {
                SelectElement(gridPos);
                return;
            }

            if (_selectedPosition.Value == gridPos)
            {
                ClearSelection();
                return;
            }

            if (IsAdjacent(_selectedPosition.Value, gridPos))
            {
                RequestSwap(_selectedPosition.Value, gridPos);
                return;
            }

            SelectElement(gridPos);
        }

        private void HandleSwipe(SwipeDirection direction)
        {
            if (!_selectedPosition.HasValue)
                return;

            Vector2Int targetPos = _selectedPosition.Value + direction.ToGridOffset();

            if (!_grid.IsValidPosition(targetPos))
                return;

            if (_board.IsEmpty(targetPos))
                return;

            RequestSwap(_selectedPosition.Value, targetPos);
        }

        private void SelectElement(Vector2Int gridPos)
        {
            _selectedPosition = gridPos;
            OnElementSelected?.Invoke(gridPos);
        }

        private void RequestSwap(Vector2Int from, Vector2Int to)
        {
            ClearSelection();
            OnSwapRequested?.Invoke(from, to);
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private Vector3 GetPointerWorldPosition()
        {
            Vector3 mousePos = UnityInput.mousePosition;
            mousePos.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(mousePos);
        }
    }
}
