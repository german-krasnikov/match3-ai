using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Elements;
using Match3.Matching;
using Match3.Spawn;

namespace Match3.Destroy
{
    public class DestroyHandler : MonoBehaviour
    {
        public event Action OnDestroyStarted;
        public event Action<int> OnDestroyCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private ElementFactory _factory;
        [SerializeField] private DestroyAnimator _animator;

        private readonly HashSet<Vector2Int> _positionsBuffer = new();
        private readonly List<ElementComponent> _elementsBuffer = new();

        public bool IsDestroying { get; private set; }

        public void DestroyMatches(List<Match> matches)
        {
            if (matches == null || matches.Count == 0) return;
            if (IsDestroying) return;

            IsDestroying = true;

            CollectUniquePositions(matches);
            CollectElements();

            if (_elementsBuffer.Count == 0)
            {
                FinishDestroy(0);
                return;
            }

            OnDestroyStarted?.Invoke();

            int count = _elementsBuffer.Count;
            _animator.AnimateDestroy(_elementsBuffer, () => OnAnimationComplete(count));
        }

        private void CollectUniquePositions(List<Match> matches)
        {
            _positionsBuffer.Clear();
            foreach (var match in matches)
                foreach (var pos in match.Positions)
                    _positionsBuffer.Add(pos);
        }

        private void CollectElements()
        {
            _elementsBuffer.Clear();
            foreach (var pos in _positionsBuffer)
            {
                var element = _board.GetElement(pos);
                if (element != null)
                    _elementsBuffer.Add(element);
            }
        }

        private void OnAnimationComplete(int count)
        {
            foreach (var pos in _positionsBuffer)
            {
                var element = _board.RemoveElement(pos);
                if (element != null)
                    _factory.Return(element);
            }

            FinishDestroy(count);
        }

        private void FinishDestroy(int count)
        {
            _positionsBuffer.Clear();
            _elementsBuffer.Clear();
            IsDestroying = false;
            OnDestroyCompleted?.Invoke(count);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Destroy All Matches")]
        private void TestDestroyAllMatches()
        {
            var matchFinder = GetComponent<MatchFinder>();
            if (matchFinder == null)
            {
                Debug.LogError("[DestroyHandler] MatchFinder not found on same GameObject");
                return;
            }

            var matches = matchFinder.FindAllMatches();
            Debug.Log($"[DestroyHandler] Found {matches.Count} matches, destroying {matches.Count} groups...");
            DestroyMatches(matches);
        }
#endif
    }
}
