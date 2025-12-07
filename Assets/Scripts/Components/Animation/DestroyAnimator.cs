using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Components.Board;

namespace Match3.Components.Animation
{
    public class DestroyAnimator : MonoBehaviour
    {
        public event Action OnDestroyComplete;

        [Header("Settings")]
        [SerializeField] private float _destroyDuration = 0.2f;
        [SerializeField] private float _delayBetweenTiles = 0.02f;
        [SerializeField] private Ease _destroyEase = Ease.InBack;

        [Header("Effects")]
        [SerializeField] private bool _scaleDown = true;
        [SerializeField] private bool _fadeOut = true;
        [SerializeField] private float _punchScale = 0.2f;

        private Sequence _currentSequence;

        public void AnimateDestroy(List<TileComponent> tiles, Action onComplete = null)
        {
            _currentSequence?.Kill();

            if (tiles == null || tiles.Count == 0)
            {
                onComplete?.Invoke();
                OnDestroyComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (tile == null) continue;

                float delay = i * _delayBetweenTiles;

                _currentSequence.Insert(delay, tile.transform
                    .DOPunchScale(Vector3.one * _punchScale, _destroyDuration * 0.3f));

                if (_scaleDown)
                {
                    _currentSequence.Insert(delay + _destroyDuration * 0.3f, tile.transform
                        .DOScale(Vector3.zero, _destroyDuration * 0.7f)
                        .SetEase(_destroyEase));
                }

                if (_fadeOut && tile.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    _currentSequence.Insert(delay, sr
                        .DOFade(0f, _destroyDuration)
                        .SetEase(Ease.Linear));
                }
            }

            _currentSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                OnDestroyComplete?.Invoke();
            });
        }

        public void AnimateDestroySingle(TileComponent tile, Action onComplete = null)
        {
            AnimateDestroy(new List<TileComponent> { tile }, onComplete);
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();
        }
    }
}
