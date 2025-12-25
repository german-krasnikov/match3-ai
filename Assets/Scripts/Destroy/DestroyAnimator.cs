using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Destroy
{
    public class DestroyAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _punchDuration = 0.1f;
        [SerializeField] private float _shrinkDuration = 0.2f;
        [SerializeField] private float _staggerDelay = 0.02f;

        [Header("Effects")]
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private Ease _shrinkEase = Ease.InBack;
        [SerializeField] private float _shrinkOvershoot = 2f;

        private Sequence _currentSequence;

        public void AnimateDestroy(List<ElementComponent> elements, Action onComplete)
        {
            KillCurrentAnimation();

            if (elements == null || elements.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;

                float delay = i * _staggerDelay;
                var elementSequence = CreateElementSequence(element);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementSequence(ElementComponent element)
        {
            var t = element.transform;
            var sr = element.SpriteRenderer;
            var originalScale = t.localScale;

            var seq = DOTween.Sequence();

            seq.Append(t.DOScale(originalScale * _punchScale, _punchDuration)
                .SetEase(Ease.OutQuad));

            seq.Append(t.DOScale(Vector3.zero, _shrinkDuration)
                .SetEase(_shrinkEase, _shrinkOvershoot));

            seq.Join(sr.DOFade(0f, _shrinkDuration)
                .SetEase(Ease.OutQuad));

            seq.OnComplete(() =>
            {
                t.localScale = originalScale;
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 1f);
            });

            return seq;
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
