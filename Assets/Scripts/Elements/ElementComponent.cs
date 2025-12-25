using System;
using UnityEngine;

namespace Match3.Elements
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ElementComponent : MonoBehaviour
    {
        public event Action<ElementComponent> OnDestroyed;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        private ElementType _type;
        private Vector2Int _gridPosition;

        public ElementType Type => _type;
        public Vector2Int GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public void Initialize(ElementData data, Vector2Int gridPos)
        {
            _type = data.Type;
            _gridPosition = gridPos;

            _spriteRenderer.sprite = data.Sprite;
            _spriteRenderer.color = data.Color;

            gameObject.name = $"Element_{data.Type}_{gridPos.x}_{gridPos.y}";
        }

        public void ResetElement()
        {
            _type = ElementType.None;
            _gridPosition = Vector2Int.zero;
            _spriteRenderer.sprite = null;
        }

        public void DestroyElement()
        {
            OnDestroyed?.Invoke(this);
        }

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
