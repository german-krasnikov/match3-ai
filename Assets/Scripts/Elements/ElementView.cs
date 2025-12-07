using System;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Elements
{
    public class ElementView : MonoBehaviour, IElement
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private ElementType _type;
        private GridPosition _position;
        private Tween _currentTween;

        public ElementType Type => _type;
        public GridPosition Position { get => _position; set => _position = value; }
        public Transform Transform => transform;

        public void Initialize(ElementType type, GridPosition position)
        {
            _type = type;
            _position = position;
            _spriteRenderer.sprite = type.Sprite;
            _spriteRenderer.color = type.Color;
            transform.localScale = Vector3.one;
        }

        public void MoveTo(Vector3 worldPosition, float duration, Action onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOMove(worldPosition, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayDestroyAnimation(Action onComplete)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InBack)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnDisable()
        {
            _currentTween?.Kill();
        }
    }
}
