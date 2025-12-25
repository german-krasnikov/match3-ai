using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Match3.Fall
{
    public class FallAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _fallSpeed = 12f;
        [SerializeField] private float _minFallDuration = 0.1f;
        [SerializeField] private float _maxFallDuration = 0.5f;
        [SerializeField] private float _staggerDelay = 0.02f;

        [Header("Effects")]
        [SerializeField] private Ease _fallEase = Ease.InQuad;
        [SerializeField] private float _bounceStrength = 0.15f;
        [SerializeField] private float _bounceDuration = 0.15f;

        private Sequence _currentSequence;

        public void AnimateFalls(List<FallData> falls, List<Vector3> targetPositions, Action onComplete)
        {
            KillCurrentAnimation();

            if (falls == null || falls.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentSequence = DOTween.Sequence();

            var columnDelays = new Dictionary<int, float>();
            float currentDelay = 0f;

            for (int i = 0; i < falls.Count; i++)
            {
                var fall = falls[i];
                var targetPos = targetPositions[i];

                if (fall.Element == null) continue;

                int column = fall.From.x;
                if (!columnDelays.TryGetValue(column, out float delay))
                {
                    delay = currentDelay;
                    columnDelays[column] = delay;
                    currentDelay += _staggerDelay;
                }

                var elementSequence = CreateElementFallSequence(fall, targetPos);
                _currentSequence.Insert(delay, elementSequence);
            }

            _currentSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Sequence CreateElementFallSequence(FallData fall, Vector3 targetPos)
        {
            var transform = fall.Element.transform;

            float duration = fall.Distance / _fallSpeed;
            duration = Mathf.Clamp(duration, _minFallDuration, _maxFallDuration);

            var seq = DOTween.Sequence();

            seq.Append(transform.DOMove(targetPos, duration).SetEase(_fallEase));

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
