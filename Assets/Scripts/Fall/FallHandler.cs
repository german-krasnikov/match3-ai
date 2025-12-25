using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;

namespace Match3.Fall
{
    public class FallHandler : MonoBehaviour
    {
        public event Action OnFallsStarted;
        public event Action OnFallsCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private FallAnimator _animator;

        private List<FallData> _currentFalls;

        public bool IsFalling { get; private set; }

        public void ExecuteFalls()
        {
            if (IsFalling) return;

            _currentFalls = FallCalculator.CalculateFalls(_board);

            if (_currentFalls.Count == 0)
            {
                OnFallsCompleted?.Invoke();
                return;
            }

            IsFalling = true;
            OnFallsStarted?.Invoke();

            UpdateBoardState();
            AnimateFalls();
        }

        private void UpdateBoardState()
        {
            foreach (var fall in _currentFalls)
            {
                _board.SetElement(fall.From, null);
            }

            foreach (var fall in _currentFalls)
            {
                _board.SetElement(fall.To, fall.Element);
            }
        }

        private void AnimateFalls()
        {
            var worldPositions = new List<Vector3>(_currentFalls.Count);
            foreach (var fall in _currentFalls)
            {
                worldPositions.Add(_grid.GridToWorld(fall.To));
            }

            _animator.AnimateFalls(_currentFalls, worldPositions, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            IsFalling = false;
            _currentFalls = null;
            OnFallsCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Execute Falls")]
        private void TestExecuteFalls()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[FallHandler] Only works in Play Mode");
                return;
            }

            var falls = FallCalculator.CalculateFalls(_board);
            Debug.Log($"[FallHandler] Calculated {falls.Count} falls:");
            foreach (var fall in falls)
            {
                Debug.Log($"  {fall}");
            }

            ExecuteFalls();
        }
#endif
    }
}
