using UnityEngine;
using Match3.Core;

namespace Match3.Elements
{
    public class ElementComponent : MonoBehaviour, IGridElement
    {
        [Header("Dependencies")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Runtime (Debug)")]
        [SerializeField] private ElementType _type;
        [SerializeField] private Vector2Int _gridPosition;

        private ElementColorConfig _colorConfig;

        public Vector2Int GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }

        public ElementType Type => _type;
        public GameObject GameObject => gameObject;

        public void Initialize(ElementType type, ElementColorConfig colorConfig)
        {
            _type = type;
            _colorConfig = colorConfig;
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_spriteRenderer == null || _colorConfig == null)
                return;

            _spriteRenderer.color = _colorConfig.GetColor(_type);
        }
    }
}
