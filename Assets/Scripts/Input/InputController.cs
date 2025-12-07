using System;
using UnityEngine;
using Match3.Common;
using Match3.Components.Board;

namespace Match3.Input
{
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2Int> OnTilePressed;
        public event Action<Vector2Int, SwipeDirection> OnSwipe;
        public event Action OnInputCancelled;

        [Header("Settings")]
        [SerializeField] private float _swipeThreshold = 0.3f;
        [SerializeField] private LayerMask _tileLayer;

        [Header("Dependencies")]
        [SerializeField] private Camera _camera;
        [SerializeField] private GridComponent _grid;

        private InputState _state = InputState.Idle;
        private Vector2 _pressStartPosition;
        private Vector2Int _selectedGridPosition;
        private readonly AndCondition _inputCondition = new();

        public bool IsBlocked => _state == InputState.Blocked;

        public void AddCondition(Func<bool> condition) => _inputCondition.AddCondition(condition);
        public void RemoveCondition(Func<bool> condition) => _inputCondition.RemoveCondition(condition);

        public void SetBlocked(bool blocked)
        {
            _state = blocked ? InputState.Blocked : InputState.Idle;
        }

        private void Update()
        {
            if (_state == InputState.Blocked) return;
            if (!_inputCondition.IsTrue()) return;

            HandleInput();
        }

        private void HandleInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnPointerDown(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0) && _state == InputState.TileSelected)
            {
                OnPointerDrag(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnPointerUp();
            }
        }

        private void OnPointerDown(Vector2 screenPosition)
        {
            Vector2 worldPos = _camera.ScreenToWorldPoint(screenPosition);

            if (!_grid.TryWorldToGrid(worldPos, out Vector2Int gridPos))
                return;

            var cell = _grid.GetCell(gridPos);
            if (cell == null || cell.CurrentTile == null || !cell.CurrentTile.CanSwap)
                return;

            _pressStartPosition = worldPos;
            _selectedGridPosition = gridPos;
            _state = InputState.TileSelected;

            OnTilePressed?.Invoke(gridPos);
        }

        private void OnPointerDrag(Vector2 screenPosition)
        {
            Vector2 worldPos = _camera.ScreenToWorldPoint(screenPosition);
            Vector2 delta = worldPos - _pressStartPosition;

            if (delta.magnitude < _swipeThreshold)
                return;

            SwipeDirection direction = GetSwipeDirection(delta);

            if (direction != SwipeDirection.None)
            {
                _state = InputState.Idle;
                OnSwipe?.Invoke(_selectedGridPosition, direction);
            }
        }

        private void OnPointerUp()
        {
            if (_state == InputState.TileSelected)
            {
                OnInputCancelled?.Invoke();
            }
            _state = InputState.Idle;
        }

        private SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
