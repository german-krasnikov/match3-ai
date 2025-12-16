using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "GravityConfig", menuName = "Match3/GravityConfig")]
public class GravityConfig : ScriptableObject
{
    [Header("Fall Animation")]
    [SerializeField, Range(0.05f, 0.3f)] private float _fallDurationPerCell = 0.08f;
    [SerializeField] private Ease _fallEase = Ease.OutBounce;

    [Header("Wave Effect")]
    [SerializeField, Range(0f, 0.05f)] private float _columnDelay = 0.02f;

    [Header("New Elements")]
    [SerializeField, Range(0.5f, 3f)] private float _spawnHeightOffset = 1f;

    public float FallDurationPerCell => _fallDurationPerCell;
    public Ease FallEase => _fallEase;
    public float ColumnDelay => _columnDelay;
    public float SpawnHeightOffset => _spawnHeightOffset;
}
