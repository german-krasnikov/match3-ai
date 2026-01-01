using System;
using UnityEngine;
using Match3.Grid;

namespace Match3.Input
{
    public class SwipeDetector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _minSwipeDistance = 0.3f;
        [SerializeField] private Camera _camera;

        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;

        private GridData _gridData;
        private bool _isDragging;
        private Vector3 _startWorldPos;
        private Vector2Int _startGridPos;

        /// <summary>
        /// Fires when valid swipe detected.
        /// from = starting cell, to = target cell (adjacent)
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnSwipeDetected;

        /// <summary>
        /// Enables/disables input processing.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Start()
        {
            if (_gridView != null)
                _gridData = _gridView.Data;
        }

        /// <summary>
        /// Sets GridData reference (for runtime initialization).
        /// </summary>
        public void Initialize(GridData gridData)
        {
            _gridData = gridData;
        }

        private void Update()
        {
            if (!IsEnabled || _gridData == null)
                return;

            HandleInput();
        }

        private void HandleInput()
        {
            // Support both mouse and touch
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandlePointerDown(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isDragging)
            {
                HandlePointerUp(UnityEngine.Input.mousePosition);
            }

            // Touch input (mobile)
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandlePointerDown(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_isDragging)
                            HandlePointerUp(touch.position);
                        break;
                }
            }
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector2Int gridPos = _gridData.WorldToGrid(worldPos);

            // Only start drag if on valid grid cell
            if (!_gridData.IsValidPosition(gridPos))
                return;

            _isDragging = true;
            _startWorldPos = worldPos;
            _startGridPos = gridPos;
        }

        private void HandlePointerUp(Vector2 screenPos)
        {
            _isDragging = false;

            Vector3 endWorldPos = ScreenToWorld(screenPos);
            Vector2 delta = endWorldPos - _startWorldPos;

            // Check minimum swipe distance
            if (delta.magnitude < _minSwipeDistance)
                return;

            // Determine direction (4-directional: up, down, left, right)
            Vector2Int direction = GetSwipeDirection(delta);
            if (direction == Vector2Int.zero)
                return;

            Vector2Int targetPos = _startGridPos + direction;

            // Validate target is on grid
            if (!_gridData.IsValidPosition(targetPos))
                return;

            // Fire event
            OnSwipeDetected?.Invoke(_startGridPos, targetPos);
        }

        private Vector2Int GetSwipeDirection(Vector2 delta)
        {
            // Determine primary direction based on larger axis
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipe
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                // Vertical swipe
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }
    }
}
