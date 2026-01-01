using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;

namespace Match3.Destroy
{
    /// <summary>
    /// Animates gem destruction using DOTween.
    /// Scale to zero with cascade delay between gems.
    /// </summary>
    public class DestroyAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _scaleDuration = 0.2f;
        [SerializeField] private float _cascadeDelay = 0.05f;
        [SerializeField] private Ease _scaleEase = Ease.InBack;

        /// <summary>
        /// Fires when all destruction animations complete.
        /// </summary>
        public event Action OnDestroyComplete;

        /// <summary>
        /// Duration of single gem destruction animation.
        /// </summary>
        public float ScaleDuration => _scaleDuration;

        /// <summary>
        /// Delay between each gem's animation start.
        /// </summary>
        public float CascadeDelay => _cascadeDelay;

        /// <summary>
        /// Animates destruction of gems with cascade effect.
        /// </summary>
        /// <param name="gems">List of GemViews to animate</param>
        /// <returns>Sequence tween (can be used for chaining)</returns>
        public Tween AnimateDestroy(List<GemView> gems)
        {
            return AnimateDestroy(gems, _cascadeDelay);
        }

        /// <summary>
        /// Animates destruction of gems with custom cascade delay.
        /// </summary>
        /// <param name="gems">List of GemViews to animate</param>
        /// <param name="cascadeDelay">Delay between each gem's animation</param>
        /// <returns>Sequence tween (can be used for chaining)</returns>
        public Tween AnimateDestroy(List<GemView> gems, float cascadeDelay)
        {
            if (gems == null || gems.Count == 0)
            {
                OnDestroyComplete?.Invoke();
                return null;
            }

            var sequence = DOTween.Sequence();

            for (int i = 0; i < gems.Count; i++)
            {
                var gem = gems[i];
                if (gem == null) continue;

                float delay = i * cascadeDelay;

                // Scale to zero with delay
                sequence.Insert(
                    delay,
                    gem.transform
                        .DOScale(Vector3.zero, _scaleDuration)
                        .SetEase(_scaleEase)
                );
            }

            sequence.OnComplete(() => OnDestroyComplete?.Invoke());

            return sequence;
        }

        /// <summary>
        /// Calculates total animation duration for given gem count.
        /// </summary>
        public float GetTotalDuration(int gemCount)
        {
            if (gemCount <= 0) return 0f;
            return _scaleDuration + (gemCount - 1) * _cascadeDelay;
        }

        /// <summary>
        /// Calculates total animation duration with custom cascade delay.
        /// </summary>
        public float GetTotalDuration(int gemCount, float cascadeDelay)
        {
            if (gemCount <= 0) return 0f;
            return _scaleDuration + (gemCount - 1) * cascadeDelay;
        }
    }
}
