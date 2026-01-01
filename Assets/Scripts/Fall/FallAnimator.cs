using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;
using Match3.Grid;

namespace Match3.Fall
{
    public class FallAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _fallSpeed = 8f;
        [SerializeField] private float _minDuration = 0.1f;
        [SerializeField] private Ease _fallEase = Ease.InQuad;

        [Header("Dependencies")]
        [SerializeField] private GridView _gridView;

        private int _activeTweens;
        private GridData _gridData;

        /// <summary>
        /// Fires when ALL fall animations complete.
        /// </summary>
        public event Action OnAllFallsComplete;

        private void Awake()
        {
            if (_gridView != null)
                _gridData = _gridView.Data;
        }

        /// <summary>
        /// Sets GridData reference (for runtime initialization).
        /// </summary>
        public void Initialize(GridData gridData)
        {
            _gridData = gridData;
        }

        /// <summary>
        /// Animates single gem fall to target grid position.
        /// </summary>
        /// <param name="gem">GemView to animate</param>
        /// <param name="targetGridPos">Target grid position</param>
        /// <returns>Tween for chaining</returns>
        public Tween AnimateFall(GemView gem, Vector2Int targetGridPos)
        {
            if (_gridData == null)
            {
                Debug.LogError("FallAnimator: GridData not set!");
                return null;
            }

            Vector3 targetWorldPos = _gridData.GridToWorld(targetGridPos);
            float distance = Mathf.Abs(gem.transform.position.y - targetWorldPos.y);
            float duration = Mathf.Max(distance / _fallSpeed, _minDuration);

            return gem.transform
                .DOMove(targetWorldPos, duration)
                .SetEase(_fallEase);
        }

        /// <summary>
        /// Animates gem fall from world position to target grid position.
        /// Used for newly spawned gems above grid.
        /// </summary>
        public Tween AnimateFallFromPosition(GemView gem, Vector3 startPos, Vector2Int targetGridPos)
        {
            if (_gridData == null)
            {
                Debug.LogError("FallAnimator: GridData not set!");
                return null;
            }

            gem.transform.position = startPos;
            Vector3 targetWorldPos = _gridData.GridToWorld(targetGridPos);
            float distance = Mathf.Abs(startPos.y - targetWorldPos.y);
            float duration = Mathf.Max(distance / _fallSpeed, _minDuration);

            return gem.transform
                .DOMove(targetWorldPos, duration)
                .SetEase(_fallEase);
        }

        /// <summary>
        /// Animates multiple falls. Fires OnAllFallsComplete when done.
        /// </summary>
        /// <param name="gems">List of (GemView, targetGridPos) pairs</param>
        public void AnimateFalls(List<(GemView gem, Vector2Int targetPos)> falls)
        {
            if (falls == null || falls.Count == 0)
            {
                OnAllFallsComplete?.Invoke();
                return;
            }

            _activeTweens = falls.Count;

            foreach (var (gem, targetPos) in falls)
            {
                var tween = AnimateFall(gem, targetPos);
                if (tween != null)
                {
                    tween.OnComplete(HandleTweenComplete);
                }
                else
                {
                    HandleTweenComplete();
                }
            }
        }

        /// <summary>
        /// Animates falls for existing gems + newly spawned gems.
        /// </summary>
        /// <param name="existingFalls">Existing gems falling down</param>
        /// <param name="newGems">New gems spawning from above</param>
        public void AnimateAllFalls(
            List<(GemView gem, Vector2Int targetPos)> existingFalls,
            List<(GemView gem, Vector3 startPos, Vector2Int targetPos)> newGems)
        {
            int totalCount =
                (existingFalls?.Count ?? 0) +
                (newGems?.Count ?? 0);

            if (totalCount == 0)
            {
                OnAllFallsComplete?.Invoke();
                return;
            }

            _activeTweens = totalCount;

            // Animate existing gems
            if (existingFalls != null)
            {
                foreach (var (gem, targetPos) in existingFalls)
                {
                    var tween = AnimateFall(gem, targetPos);
                    if (tween != null)
                        tween.OnComplete(HandleTweenComplete);
                    else
                        HandleTweenComplete();
                }
            }

            // Animate new gems from spawn position
            if (newGems != null)
            {
                foreach (var (gem, startPos, targetPos) in newGems)
                {
                    var tween = AnimateFallFromPosition(gem, startPos, targetPos);
                    if (tween != null)
                        tween.OnComplete(HandleTweenComplete);
                    else
                        HandleTweenComplete();
                }
            }
        }

        /// <summary>
        /// Kills all active fall tweens.
        /// </summary>
        public void StopAll()
        {
            DOTween.Kill(transform);
            _activeTweens = 0;
        }

        // --- Private Helpers ---

        private void HandleTweenComplete()
        {
            _activeTweens--;
            if (_activeTweens <= 0)
            {
                _activeTweens = 0;
                OnAllFallsComplete?.Invoke();
            }
        }
    }
}
