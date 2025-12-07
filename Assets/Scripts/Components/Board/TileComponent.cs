using System;
using UnityEngine;
using Match3.Data;
using Match3.Interfaces;

namespace Match3.Components.Board
{
    public class TileComponent : MonoBehaviour, ITile
    {
        public event Action<TileComponent> OnDestroyed;
        event Action<ITile> ITile.OnDestroyed
        {
            add => OnDestroyed += t => value?.Invoke(t);
            remove { }
        }
        public event Action<Vector2Int, Vector2Int> OnMoved;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        private TileType _type;
        private Vector2Int _gridPosition;
        private bool _isMatched;
        private bool _isMoving;

        public TileType Type => _type;
        public Vector2Int GridPosition => _gridPosition;
        public bool IsMatched { get => _isMatched; set => _isMatched = value; }
        public bool IsMoving => _isMoving;
        public bool IsMatchable => _type != TileType.None;

        public void Initialize(TileType type, TileData data, Vector2Int position)
        {
            _type = type;
            _gridPosition = position;
            _isMatched = false;
            _isMoving = false;

            if (_spriteRenderer != null && data != null)
            {
                _spriteRenderer.sprite = data.sprite;
                _spriteRenderer.color = data.color;
            }
        }

        public void SetGridPosition(Vector2Int newPosition)
        {
            var oldPosition = _gridPosition;
            _gridPosition = newPosition;
            OnMoved?.Invoke(oldPosition, newPosition);
        }

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetMoving(bool moving) => _isMoving = moving;

        public void DestroySelf()
        {
            OnDestroyed?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
