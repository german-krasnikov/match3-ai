// Assets/Scripts/Features/Board/Views/ElementView.cs
using System;
using Common;
using UnityEngine;

namespace Features.Board.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ElementView : MonoBehaviour, IElementView
    {
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;
        private Camera _mainCamera;

        private bool _isDragging;
        private Vector3 _dragStartWorldPos;
        private const float DragThreshold = 0.3f;

        public GridPosition Position { get; private set; }
        public ElementType Type { get; private set; }

        public event Action<GridPosition> OnClicked;
        public event Action<GridPosition> OnDragStart;
        public event Action<GridPosition> OnDragEnd;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _mainCamera = Camera.main;
        }

        public void Initialize(GridPosition pos, ElementType type, Sprite sprite)
        {
            Position = pos;
            Type = type;
            SetSprite(sprite);
            gameObject.name = $"Element_{pos.X}_{pos.Y}_{type}";

            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider2D>();

            _collider.size = Vector2.one * 0.9f;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _spriteRenderer.sprite = sprite;
        }

        public void UpdatePosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void Destroy()
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private void OnMouseDown()
        {
            _isDragging = true;
            _dragStartWorldPos = GetMouseWorldPosition();
            OnDragStart?.Invoke(Position);
        }

        private void OnMouseUp()
        {
            if (!_isDragging) return;

            _isDragging = false;

            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 delta = mousePos - _dragStartWorldPos;

            if (delta.magnitude < DragThreshold)
            {
                OnClicked?.Invoke(Position);
                OnDragEnd?.Invoke(Position);
            }
            else
            {
                GridPosition targetPos = GetTargetPositionFromDrag(delta);
                OnDragEnd?.Invoke(targetPos);
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(mousePos);
        }

        private GridPosition GetTargetPositionFromDrag(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                int dx = delta.x > 0 ? 1 : -1;
                return new GridPosition(Position.X + dx, Position.Y);
            }
            else
            {
                int dy = delta.y > 0 ? 1 : -1;
                return new GridPosition(Position.X, Position.Y + dy);
            }
        }
    }
}
