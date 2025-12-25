using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Board;
using Match3.Grid;
using Match3.Elements;

namespace Match3.GameLoop
{
    /// <summary>
    /// Shuffles board elements when no moves are available.
    /// </summary>
    public class BoardShuffler : MonoBehaviour
    {
        public event Action OnShuffleStarted;
        public event Action OnShuffleCompleted;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;
        [SerializeField] private GridComponent _grid;

        [Header("Animation")]
        [SerializeField] private float _shuffleDuration = 0.4f;
        [SerializeField] private Ease _shuffleEase = Ease.InOutQuad;
        [SerializeField] private float _staggerDelay = 0.02f;

        private readonly List<ElementComponent> _elementsBuffer = new();
        private readonly List<Vector2Int> _positionsBuffer = new();
        private Sequence _currentSequence;

        public bool IsShuffling { get; private set; }

        public void Shuffle()
        {
            if (IsShuffling) return;

            IsShuffling = true;
            OnShuffleStarted?.Invoke();

            CollectElements();
            ShufflePositions();
            UpdateBoard();
            AnimateShuffle();
        }

        private void CollectElements()
        {
            _elementsBuffer.Clear();
            _positionsBuffer.Clear();

            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var element = _board.GetElement(pos);

                    if (element != null)
                    {
                        _elementsBuffer.Add(element);
                        _positionsBuffer.Add(pos);
                    }
                }
            }
        }

        private void ShufflePositions()
        {
            // Fisher-Yates shuffle
            for (int i = _positionsBuffer.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_positionsBuffer[i], _positionsBuffer[j]) =
                    (_positionsBuffer[j], _positionsBuffer[i]);
            }
        }

        private void UpdateBoard()
        {
            // Clear board
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                    _board.SetElement(new Vector2Int(x, y), null);

            // Set elements at new positions
            for (int i = 0; i < _elementsBuffer.Count; i++)
            {
                var element = _elementsBuffer[i];
                var newPos = _positionsBuffer[i];
                _board.SetElement(newPos, element);
            }
        }

        private void AnimateShuffle()
        {
            KillCurrentAnimation();
            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < _elementsBuffer.Count; i++)
            {
                var element = _elementsBuffer[i];
                var newPos = _positionsBuffer[i];
                var worldPos = _grid.GridToWorld(newPos);

                float delay = i * _staggerDelay;

                _currentSequence.Insert(delay,
                    element.transform.DOMove(worldPos, _shuffleDuration)
                        .SetEase(_shuffleEase));
            }

            _currentSequence.OnComplete(OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            _elementsBuffer.Clear();
            _positionsBuffer.Clear();
            IsShuffling = false;
            OnShuffleCompleted?.Invoke();
        }

        private void KillCurrentAnimation()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Shuffle")]
        private void TestShuffle()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[BoardShuffler] Only works in Play Mode");
                return;
            }

            Debug.Log("[BoardShuffler] Starting shuffle...");
            Shuffle();
        }
#endif
    }
}
