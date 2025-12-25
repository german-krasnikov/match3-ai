using UnityEngine;
using Match3.Board;
using DG.Tweening;

namespace Match3.Input
{
    /// <summary>
    /// Visually highlights the selected element with a pulsing animation.
    /// </summary>
    public class SelectionHighlighter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputDetector _inputDetector;
        [SerializeField] private BoardComponent _board;

        [Header("Settings")]
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDuration = 0.3f;

        private Tween _currentTween;
        private Transform _selectedTransform;
        private Vector3 _originalScale;

        private void OnEnable()
        {
            _inputDetector.OnElementSelected += OnElementSelected;
            _inputDetector.OnSelectionCancelled += StopHighlight;
            _inputDetector.OnSwapRequested += OnSwapRequested;
        }

        private void OnDisable()
        {
            _inputDetector.OnElementSelected -= OnElementSelected;
            _inputDetector.OnSelectionCancelled -= StopHighlight;
            _inputDetector.OnSwapRequested -= OnSwapRequested;
            StopHighlight();
        }

        private void OnElementSelected(Vector2Int pos)
        {
            StopHighlight();

            var element = _board.GetElement(pos);
            if (element == null) return;

            _selectedTransform = element.transform;
            _originalScale = _selectedTransform.localScale;

            _currentTween = _selectedTransform
                .DOScale(_originalScale * _pulseScale, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void OnSwapRequested(Vector2Int from, Vector2Int to)
        {
            StopHighlight();
        }

        private void StopHighlight()
        {
            _currentTween?.Kill();
            _currentTween = null;

            if (_selectedTransform != null)
            {
                _selectedTransform.localScale = _originalScale;
                _selectedTransform = null;
            }
        }
    }
}
