using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Grid;
using Match3.Spawn;
using Match3.Elements;

namespace Match3.Refill
{
    public class RefillHandler : MonoBehaviour
    {
        public event Action OnRefillsStarted;
        public event Action OnRefillsCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private ElementFactory _factory;
        [SerializeField] private RefillAnimator _animator;

        private List<RefillData> _currentRefills;
        private List<ElementComponent> _createdElements;

        public bool IsRefilling { get; private set; }

        public void ExecuteRefills()
        {
            if (IsRefilling) return;

            _currentRefills = RefillCalculator.CalculateRefills(_board, _grid);

            if (_currentRefills.Count == 0)
            {
                OnRefillsCompleted?.Invoke();
                return;
            }

            IsRefilling = true;
            OnRefillsStarted?.Invoke();

            CreateElements();
            UpdateBoardState();
            AnimateRefills();
        }

        private void CreateElements()
        {
            _createdElements = new List<ElementComponent>(_currentRefills.Count);

            foreach (var refill in _currentRefills)
            {
                var element = _factory.CreateRandom(
                    refill.SpawnWorldPosition,
                    refill.TargetPosition
                );
                _createdElements.Add(element);
            }
        }

        private void UpdateBoardState()
        {
            for (int i = 0; i < _currentRefills.Count; i++)
            {
                var refill = _currentRefills[i];
                var element = _createdElements[i];
                _board.SetElement(refill.TargetPosition, element);
            }
        }

        private void AnimateRefills()
        {
            _animator.AnimateRefills(_currentRefills, _createdElements, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            IsRefilling = false;
            _currentRefills = null;
            _createdElements = null;
            OnRefillsCompleted?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Execute Refills")]
        private void TestExecuteRefills()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[RefillHandler] Only works in Play Mode");
                return;
            }

            var refills = RefillCalculator.CalculateRefills(_board, _grid);
            Debug.Log($"[RefillHandler] Calculated {refills.Count} refills:");
            foreach (var refill in refills)
            {
                Debug.Log($"  {refill}");
            }

            ExecuteRefills();
        }
#endif
    }
}
