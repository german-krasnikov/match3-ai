using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class FallAnimationComponent : MonoBehaviour
{
    public event Action OnFallComplete;

    [SerializeField] private GravityConfig _config;
    [SerializeField] private GridComponent _grid;

    private Sequence _currentSequence;

    public void AnimateFalls(List<FallData> fallData, Action onComplete)
    {
        _currentSequence?.Kill();

        if (fallData == null || fallData.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _currentSequence = DOTween.Sequence();

        var byColumn = fallData.GroupBy(f => f.Column).OrderBy(g => g.Key);

        int columnIndex = 0;
        foreach (var columnGroup in byColumn)
        {
            float columnDelay = columnIndex * _config.ColumnDelay;

            foreach (var fall in columnGroup)
            {
                Vector3 targetPos = _grid.GridToWorld(fall.Column, fall.ToY);
                float duration = fall.Distance * _config.FallDurationPerCell;

                _currentSequence.Insert(columnDelay,
                    fall.Element.transform.DOMove(targetPos, duration)
                        .SetEase(_config.FallEase));
            }

            columnIndex++;
        }

        _currentSequence.OnComplete(() =>
        {
            OnFallComplete?.Invoke();
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
