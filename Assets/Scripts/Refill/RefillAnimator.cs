using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Elements;

namespace Match3.Refill
{
    public class RefillAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _fallSpeed = 12f;
        [SerializeField] private float _minFallDuration = 0.1f;
        [SerializeField] private float _maxFallDuration = 0.6f;
        [SerializeField] private float _staggerDelay = 0.03f;

        [Header("Effects")]
        [SerializeField] private Ease _fallEase = Ease.InQuad;
        [SerializeField] private float _bounceStrength = 0.15f;
        [SerializeField] private float _bounceDuration = 0.15f;

        [Header("Spawn Effect")]
        [SerializeField] private Vector3 _targetScale = new(5.5f, 5.5f, 1f);
        [SerializeField] private float _spawnScaleMultiplier = 0.5f;
        [SerializeField] private float _scaleUpDuration = 0.1f;

        private Sequence _currentSequence;

        public void AnimateRefills(
            List<RefillData> refills,
            List<ElementComponent> elements,
            Action onComplete)
        {
            KillCurrentAnimation();

            if (refills == null || refills.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            var columnDelays = new Dictionary<int, float>();
            float maxDelay = 0f;

            for (int i = 0; i < refills.Count; i++)
            {
                var refill = refills[i];
                var element = elements[i];

                if (element == null) continue;

                int column = refill.TargetPosition.x;
                if (!columnDelays.TryGetValue(column, out float delay))
                {
                    delay = maxDelay;
                    columnDelays[column] = delay;
                    maxDelay += _staggerDelay;
                }
                else
                {
                    delay = columnDelays[column] + _staggerDelay;
                    columnDelays[column] = delay;
                }

                var elementSequence = CreateElementRefillSequence(refill, element);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementRefillSequence(RefillData refill, ElementComponent element)
        {
            var transform = element.transform;
            transform.localScale = _targetScale * _spawnScaleMultiplier;

            float duration = refill.FallDistance / _fallSpeed;
            duration = Mathf.Clamp(duration, _minFallDuration, _maxFallDuration);

            var seq = DOTween.Sequence();

            seq.Append(transform.DOScale(_targetScale, _scaleUpDuration).SetEase(Ease.OutBack));
            seq.Join(transform.DOMove(refill.TargetWorldPosition, duration).SetEase(_fallEase));

            seq.Append(transform.DOPunchScale(
                new Vector3(_bounceStrength, -_bounceStrength, 0),
                _bounceDuration,
                1,
                0.5f
            ));

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
