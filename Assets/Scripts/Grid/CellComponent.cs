using UnityEngine;
using Match3.Core;

namespace Match3.Grid
{
    /// <summary>
    /// Отдельная ячейка сетки. Хранит позицию и ссылку на фишку.
    /// </summary>
    public class CellComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private GridPosition _position;
        private IPiece _currentPiece;

        public GridPosition Position => _position;
        public IPiece CurrentPiece => _currentPiece;
        public bool IsEmpty => _currentPiece == null;

        public void Initialize(GridPosition position, Color color)
        {
            _position = position;
            _spriteRenderer.color = color;
            gameObject.name = $"Cell_{position.X}_{position.Y}";
        }

        public void SetPiece(IPiece piece)
        {
            _currentPiece = piece;
        }

        public void Clear()
        {
            _currentPiece = null;
        }

        public IPiece RemovePiece()
        {
            var piece = _currentPiece;
            _currentPiece = null;
            return piece;
        }
    }
}
