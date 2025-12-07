using System;
using DG.Tweening;
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

        public void AnimateSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            var posA = _gridView.PositionConverter.GridToWorld(elementA.Position);
            var posB = _gridView.PositionConverter.GridToWorld(elementB.Position);
            var duration = _config.SwapDuration;

            int completed = 0;
            void OnOneComplete()
            {
                completed++;
                if (completed == 2) onComplete?.Invoke();
            }

            elementA.MoveTo(posB, duration, OnOneComplete);
            elementB.MoveTo(posA, duration, OnOneComplete);
        }

        public void AnimateInvalidSwap(IElement elementA, IElement elementB, Action onComplete)
        {
            var posA = elementA.Transform.position;
            var posB = elementB.Transform.position;
            var duration = _config.SwapDuration * 0.5f;

            var midA = Vector3.Lerp(posA, posB, 0.3f);
            var midB = Vector3.Lerp(posB, posA, 0.3f);

            var sequence = DOTween.Sequence();
            sequence.Append(elementA.Transform.DOMove(midA, duration).SetEase(Ease.OutQuad));
            sequence.Join(elementB.Transform.DOMove(midB, duration).SetEase(Ease.OutQuad));
            sequence.Append(elementA.Transform.DOMove(posA, duration).SetEase(Ease.OutBack));
            sequence.Join(elementB.Transform.DOMove(posB, duration).SetEase(Ease.OutBack));
            sequence.OnComplete(() => onComplete?.Invoke());
        }
    }
}
