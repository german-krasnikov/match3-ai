using UnityEngine;
using DG.Tweening;

namespace Match3.Components.Visual
{
    public class SelectionVisualComponent : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDuration = 0.3f;

        [Header("Dependencies")]
        [SerializeField] private SpriteRenderer _renderer;

        private Tweener _pulseTween;

        public void Show(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);

            _pulseTween?.Kill();
            transform.localScale = Vector3.one;
            _pulseTween = transform
                .DOScale(_pulseScale, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void Hide()
        {
            _pulseTween?.Kill();
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _pulseTween?.Kill();
        }
    }
}
