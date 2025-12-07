using UnityEngine;

namespace Match3.Input
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Match3/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [SerializeField] private float _minSwipeDistance = 30f;
        [SerializeField] private float _maxSwipeTime = 0.5f;

        public float MinSwipeDistance => _minSwipeDistance;
        public float MaxSwipeTime => _maxSwipeTime;
    }
}
