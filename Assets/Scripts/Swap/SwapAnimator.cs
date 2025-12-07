using System;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Swap
{
    public class SwapAnimator : MonoBehaviour
    {
        [SerializeField] private GridConfig _config;
        [SerializeField] private GridView _gridView;

        private Sequence _currentSequence;

        public void AnimateSwap(IElement elementA, IElement elementB, GridPosition targetA, GridPosition targetB, Action onComplete)
        {
            KillCurrent();

            var worldA = _gridView.PositionConverter.GridToWorld(targetA);
            var worldB = _gridView.PositionConverter.GridToWorld(targetB);
            var duration = _config.SwapDuration;

            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(elementA.Transform.DOMove(worldA, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(worldB, duration).SetEase(Ease.OutQuad));
            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        public void AnimateInvalidSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            KillCurrent();

            var posA = elementA.Transform.position;
            var posB = elementB.Transform.position;
            var duration = _config.SwapDuration;

            _currentSequence = DOTween.Sequence();
            _currentSequence.Append(elementA.Transform.DOMove(posB, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(posA, duration).SetEase(Ease.OutQuad));
            _currentSequence.Append(elementA.Transform.DOMove(posA, duration).SetEase(Ease.OutQuad));
            _currentSequence.Join(elementB.Transform.DOMove(posB, duration).SetEase(Ease.OutQuad));
            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void KillCurrent()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDisable() => KillCurrent();
    }
}
