using System;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Swap
{
    /// <summary>
    /// Animates element swapping with bouncy casual style.
    /// </summary>
    public class SwapAnimator : MonoBehaviour
    {
        public event Action OnSwapAnimationComplete;

        [Header("Settings")]
        [SerializeField] private float _swapDuration = 0.25f;
        [SerializeField] private Ease _swapEase = Ease.OutBack;
        [SerializeField] private float _overshoot = 1.5f;

        private Sequence _currentSequence;

        public void AnimateSwap(ElementComponent elementA, ElementComponent elementB,
            Vector3 targetPosA, Vector3 targetPosB, Action onComplete = null)
        {
            KillCurrentAnimation();

            _currentSequence = DOTween.Sequence();

            _currentSequence.Join(
                elementA.transform.DOMove(targetPosA, _swapDuration)
                    .SetEase(_swapEase, _overshoot)
            );

            _currentSequence.Join(
                elementB.transform.DOMove(targetPosB, _swapDuration)
                    .SetEase(_swapEase, _overshoot)
            );

            _currentSequence.OnComplete(() =>
            {
                OnSwapAnimationComplete?.Invoke();
                onComplete?.Invoke();
            });
        }

        public void AnimateRevert(ElementComponent elementA, ElementComponent elementB,
            Vector3 originalPosA, Vector3 originalPosB, Action onComplete = null)
        {
            KillCurrentAnimation();

            _currentSequence = DOTween.Sequence();

            _currentSequence.Join(
                elementA.transform.DOMove(originalPosA, _swapDuration)
                    .SetEase(Ease.OutQuad)
            );

            _currentSequence.Join(
                elementB.transform.DOMove(originalPosB, _swapDuration)
                    .SetEase(Ease.OutQuad)
            );

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        public void KillCurrentAnimation()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}
