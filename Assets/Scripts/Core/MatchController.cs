using System;
using UnityEngine;

namespace Match3.Core
{
    public class MatchController : MonoBehaviour
    {
        public event Action OnMatchProcessingStarted;
        public event Action OnMatchProcessingComplete;
        public event Action<int> OnScoreAdded;

        [Header("Dependencies")]
        [SerializeField] private MatchDetector _detector;
        [SerializeField] private DestroyHandler _destroyHandler;
        [SerializeField] private ScoreCalculator _scoreCalculator;

        private bool _isProcessing;

        public bool IsProcessing => _isProcessing;

        private void OnEnable()
        {
            _destroyHandler.OnDestroyComplete += OnDestroyComplete;
        }

        private void OnDisable()
        {
            _destroyHandler.OnDestroyComplete -= OnDestroyComplete;
        }

        public void ProcessMatchesAt(Vector2Int posA, Vector2Int posB)
        {
            if (_isProcessing) return;

            var result = _detector.FindMatchesAt(posA, posB);

            if (result.HasMatches)
                ProcessMatches(result);
        }

        public void ProcessAllMatches()
        {
            if (_isProcessing) return;

            var result = _detector.FindAllMatches();

            if (result.HasMatches)
            {
                ProcessMatches(result);
            }
            else
            {
                _scoreCalculator.ResetCascade();
                OnMatchProcessingComplete?.Invoke();
            }
        }

        public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
        {
            return _detector.WouldCreateMatch(posA, posB);
        }

        private void ProcessMatches(Data.MatchResult result)
        {
            _isProcessing = true;
            OnMatchProcessingStarted?.Invoke();

            int score = _scoreCalculator.CalculateScore(result);
            OnScoreAdded?.Invoke(score);

            _destroyHandler.DestroyMatches(result);
        }

        private void OnDestroyComplete()
        {
            _isProcessing = false;
            _scoreCalculator.IncrementCascade();
            OnMatchProcessingComplete?.Invoke();
        }
    }
}
