using System;
using UnityEngine;
using DG.Tweening;

namespace Match3.Components.Animation
{
    public class SwapAnimator : MonoBehaviour
    {
        public event Action OnSwapComplete;
        public event Action OnRevertComplete;

        [Header("Settings")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private Ease _swapEase = Ease.OutQuad;
        [SerializeField] private float _revertDuration = 0.15f;
        [SerializeField] private Ease _revertEase = Ease.InQuad;

        private Sequence _currentSequence;

        public void AnimateSwap(Transform tileA, Transform tileB, bool isRevert = false)
        {
            _currentSequence?.Kill();

            float duration = isRevert ? _revertDuration : _swapDuration;
            Ease ease = isRevert ? _revertEase : _swapEase;

            Vector3 posA = tileA.position;
            Vector3 posB = tileB.position;

            _currentSequence = DOTween.Sequence();
            _currentSequence.Join(tileA.DOMove(posB, duration).SetEase(ease));
            _currentSequence.Join(tileB.DOMove(posA, duration).SetEase(ease));

            _currentSequence.OnComplete(() =>
            {
                if (isRevert)
                    OnRevertComplete?.Invoke();
                else
                    OnSwapComplete?.Invoke();
            });
        }

        public void Kill()
        {
            _currentSequence?.Kill();
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();
        }
    }
}
