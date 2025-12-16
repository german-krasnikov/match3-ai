using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DestroyAnimationComponent : MonoBehaviour
{
    [SerializeField] private DestroyConfig _config;

    private Sequence _currentSequence;

    public Tween AnimateDestroy(ElementComponent element)
    {
        var spriteRenderer = element.GetComponent<SpriteRenderer>();

        var seq = DOTween.Sequence();
        seq.Join(element.transform.DOScale(_config.TargetScale, _config.Duration)
            .SetEase(_config.ScaleEase));

        if (spriteRenderer != null)
        {
            seq.Join(spriteRenderer.DOFade(0f, _config.Duration)
                .SetEase(_config.FadeEase));
        }

        return seq;
    }

    public void AnimateDestroyGroup(List<ElementComponent> elements, Action onComplete)
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();

        if (elements.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var sorted = SortFromCenter(elements);

        for (int i = 0; i < sorted.Count; i++)
        {
            float delay = i * _config.StaggerDelay;
            _currentSequence.Insert(delay, AnimateDestroy(sorted[i]));
        }

        _currentSequence.OnComplete(() => onComplete?.Invoke());
    }

    private List<ElementComponent> SortFromCenter(List<ElementComponent> elements)
    {
        float centerX = 0f, centerY = 0f;
        foreach (var e in elements)
        {
            centerX += e.X;
            centerY += e.Y;
        }
        centerX /= elements.Count;
        centerY /= elements.Count;

        var sorted = new List<ElementComponent>(elements);
        sorted.Sort((a, b) =>
        {
            float distA = (a.X - centerX) * (a.X - centerX) + (a.Y - centerY) * (a.Y - centerY);
            float distB = (b.X - centerX) * (b.X - centerX) + (b.Y - centerY) * (b.Y - centerY);
            return distA.CompareTo(distB);
        });

        return sorted;
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
