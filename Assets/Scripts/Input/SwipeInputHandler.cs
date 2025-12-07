using System;
using Match3.Core;
using Match3.Grid;
using UnityEngine;

namespace Match3.Input
{
    public class SwipeInputHandler : MonoBehaviour, IInputHandler
    {
        public event Action<GridPosition, GridPosition> OnSwipeDetected;

        [SerializeField] private InputConfig _config;
        [SerializeField] private GridView _gridView;
        [SerializeField] private Camera _camera;

        private bool _isEnabled = true;
        private bool _isSwiping;
        private Vector2 _swipeStart;
        private float _swipeStartTime;
        private GridPosition _startGridPos;

        public bool IsEnabled => _isEnabled;

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled) _isSwiping = false;
        }

        private void Update()
        {
            if (!_isEnabled) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
                TryStartSwipe(UnityEngine.Input.mousePosition);
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isSwiping)
                TryCompleteSwipe(UnityEngine.Input.mousePosition);
        }

        private void HandleTouchInput()
        {
            if (UnityEngine.Input.touchCount == 0) return;

            var touch = UnityEngine.Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartSwipe(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isSwiping) TryCompleteSwipe(touch.position);
                    break;
            }
        }

        private void TryStartSwipe(Vector2 screenPos)
        {
            var worldPos = _camera.ScreenToWorldPoint(screenPos);
            var gridPos = _gridView.PositionConverter.WorldToGrid(worldPos);

            if (!IsValidGridPosition(gridPos)) return;

            _isSwiping = true;
            _swipeStart = screenPos;
            _swipeStartTime = Time.time;
            _startGridPos = gridPos;
        }

        private void TryCompleteSwipe(Vector2 screenPos)
        {
            var elapsed = Time.time - _swipeStartTime;
            var delta = screenPos - _swipeStart;
            var distance = delta.magnitude;

            _isSwiping = false;

            if (elapsed > _config.MaxSwipeTime)
            {
                Debug.Log($"[Input] Swipe too slow: {elapsed:F2}s > {_config.MaxSwipeTime}s");
                return;
            }
            if (distance < _config.MinSwipeDistance)
            {
                Debug.Log($"[Input] Swipe too short: {distance:F0}px < {_config.MinSwipeDistance}px");
                return;
            }

            var direction = GetSwipeDirection(delta);
            Debug.Log($"[Input] Swipe from {_startGridPos} dir={direction}");
            OnSwipeDetected?.Invoke(_startGridPos, direction);
        }

        private GridPosition GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? GridPosition.Right : GridPosition.Left;
            return delta.y > 0 ? GridPosition.Up : GridPosition.Down;
        }

        private bool IsValidGridPosition(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < _gridView.Config.Width &&
                   pos.Y >= 0 && pos.Y < _gridView.Config.Height;
        }
    }
}
