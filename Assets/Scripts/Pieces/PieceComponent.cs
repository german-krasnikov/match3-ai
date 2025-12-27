using System;
using UnityEngine;
using Match3.Core;

namespace Match3.Pieces
{
    /// <summary>
    /// Основной компонент игровой фишки. Реализует IPiece.
    /// </summary>
    public class PieceComponent : MonoBehaviour, IPiece
    {
        public event Action<IPiece> OnDestroyed;

        [SerializeField] private PieceView _view;

        private PieceType _type;
        private GridPosition _position;

        // === IPiece ===
        public PieceType Type => _type;
        public GridPosition Position
        {
            get => _position;
            set => _position = value;
        }
        public GameObject GameObject => gameObject;

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }

        // === Public API ===
        public void Initialize(PieceType type, PieceConfig config)
        {
            _type = type;
            _view.Setup(config.GetSprite(type), config.GetColor(type));
        }

        public void Initialize(PieceType type, Sprite sprite, Color color)
        {
            _type = type;
            _view.Setup(sprite, color);
        }

        public void ResetForPool()
        {
            _type = PieceType.None;
            _position = default;
            gameObject.SetActive(false);
        }

        public PieceView View => _view;

        // === Unity ===
        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
    }
}
