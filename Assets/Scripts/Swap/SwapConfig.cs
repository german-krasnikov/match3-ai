using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "SwapConfig", menuName = "Match3/SwapConfig")]
public class SwapConfig : ScriptableObject
{
    [Header("Input")]
    [SerializeField, Range(0.1f, 1f)] private float _minSwipeDistance = 0.3f;

    [Header("Animation")]
    [SerializeField, Range(0.05f, 0.5f)] private float _swapDuration = 0.15f;
    [SerializeField] private Ease _swapEase = Ease.OutQuad;

    public float MinSwipeDistance => _minSwipeDistance;
    public float SwapDuration => _swapDuration;
    public Ease SwapEase => _swapEase;
}
