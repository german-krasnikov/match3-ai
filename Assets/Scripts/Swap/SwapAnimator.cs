using System;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;

namespace Match3.Swap
{
    public class SwapAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private Ease _swapEase = Ease.OutQuad;
        [SerializeField] private float _swapBackDuration = 0.15f;
        [SerializeField] private Ease _swapBackEase = Ease.InOutQuad;

        private Sequence _currentSequence;

        /// <summary>
        /// Fires when swap animation completes.
        /// </summary>
        public event Action OnSwapComplete;

        /// <summary>
        /// Fires when swap-back animation completes.
        /// </summary>
        public event Action OnSwapBackComplete;

        /// <summary>
        /// Animates two gems swapping positions.
        /// Returns Tween for chaining or null if invalid.
        /// </summary>
        public Tween AnimateSwap(GemView a, GemView b)
        {
            if (a == null || b == null)
            {
                OnSwapComplete?.Invoke();
                return null;
            }

            // Kill any running animation
            KillCurrentAnimation();

            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;

            _currentSequence = DOTween.Sequence();

            // Move both gems simultaneously
            _currentSequence.Join(
                a.transform.DOMove(posB, _swapDuration).SetEase(_swapEase)
            );
            _currentSequence.Join(
                b.transform.DOMove(posA, _swapDuration).SetEase(_swapEase)
            );

            _currentSequence.OnComplete(() =>
            {
                _currentSequence = null;
                OnSwapComplete?.Invoke();
            });

            return _currentSequence;
        }

        /// <summary>
        /// Animates two gems swapping back to original positions.
        /// Used when swap doesn't result in a match.
        /// </summary>
        public Tween AnimateSwapBack(GemView a, GemView b)
        {
            if (a == null || b == null)
            {
                OnSwapBackComplete?.Invoke();
                return null;
            }

            // Kill any running animation
            KillCurrentAnimation();

            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;

            _currentSequence = DOTween.Sequence();

            // Add small delay before swap back for visual feedback
            _currentSequence.AppendInterval(0.05f);

            // Move both gems back
            _currentSequence.Join(
                a.transform.DOMove(posB, _swapBackDuration).SetEase(_swapBackEase)
            );
            _currentSequence.Join(
                b.transform.DOMove(posA, _swapBackDuration).SetEase(_swapBackEase)
            );

            _currentSequence.OnComplete(() =>
            {
                _currentSequence = null;
                OnSwapBackComplete?.Invoke();
            });

            return _currentSequence;
        }

        /// <summary>
        /// Returns true if animation is currently playing.
        /// </summary>
        public bool IsAnimating => _currentSequence != null && _currentSequence.IsPlaying();

        /// <summary>
        /// Kills current animation immediately.
        /// </summary>
        public void KillCurrentAnimation()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
