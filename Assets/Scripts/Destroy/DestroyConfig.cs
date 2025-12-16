using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "DestroyConfig", menuName = "Match3/DestroyConfig")]
public class DestroyConfig : ScriptableObject
{
    [Header("Animation")]
    [SerializeField, Range(0.05f, 0.5f)] private float _duration = 0.2f;
    [SerializeField] private Ease _scaleEase = Ease.InBack;
    [SerializeField] private Ease _fadeEase = Ease.Linear;

    [Header("Scale")]
    [SerializeField] private Vector3 _targetScale = Vector3.zero;

    [Header("Timing")]
    [SerializeField, Range(0f, 0.1f)] private float _staggerDelay = 0.02f;

    public float Duration => _duration;
    public Ease ScaleEase => _scaleEase;
    public Ease FadeEase => _fadeEase;
    public Vector3 TargetScale => _targetScale;
    public float StaggerDelay => _staggerDelay;
}
