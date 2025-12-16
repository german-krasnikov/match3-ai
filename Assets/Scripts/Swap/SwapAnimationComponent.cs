using System;
using UnityEngine;
using DG.Tweening;

public class SwapAnimationComponent : MonoBehaviour
{
    [SerializeField] private SwapConfig _config;
    [SerializeField] private GridComponent _grid;

    private Sequence _currentSequence;

    public void AnimateSwap(ElementComponent elementA, ElementComponent elementB, Action onComplete)
    {
        _currentSequence?.Kill();

        Vector3 targetPosA = _grid.GridToWorld(elementA.X, elementA.Y);
        Vector3 targetPosB = _grid.GridToWorld(elementB.X, elementB.Y);

        _currentSequence = DOTween.Sequence();

        _currentSequence.Join(
            elementA.transform.DOMove(targetPosA, _config.SwapDuration).SetEase(_config.SwapEase)
        );

        _currentSequence.Join(
            elementB.transform.DOMove(targetPosB, _config.SwapDuration).SetEase(_config.SwapEase)
        );

        _currentSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
