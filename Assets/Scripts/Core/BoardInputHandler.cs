using System;
using UnityEngine;
using Match3.Common;
using Match3.Components.Board;
using Match3.Components.Visual;
using Match3.Input;

namespace Match3.Core
{
    public class BoardInputHandler : MonoBehaviour
    {
        public event Action<Vector2Int, Vector2Int> OnSwapCompleted;
        public event Action OnMatchProcessingComplete;
        public event Action<int> OnScoreAdded;

        [Header("Dependencies")]
        [SerializeField] private InputController _inputController;
        [SerializeField] private SwapController _swapController;
        [SerializeField] private SelectionVisualComponent _selectionVisual;
        [SerializeField] private GridComponent _grid;
        [SerializeField] private MatchController _matchController;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _selectSound;
        [SerializeField] private AudioClip _swapSound;
        [SerializeField] private AudioClip _invalidSwapSound;
        [SerializeField] private AudioClip _matchSound;

        private void Awake()
        {
            _inputController.AddCondition(() => !_swapController.IsProcessing);
            if (_matchController != null)
                _inputController.AddCondition(() => !_matchController.IsProcessing);
        }

        private void OnEnable()
        {
            _inputController.OnTilePressed += OnTilePressed;
            _inputController.OnSwipe += OnSwipe;
            _inputController.OnInputCancelled += OnInputCancelled;

            _swapController.OnSwapStarted += OnSwapStarted;
            _swapController.OnSwapCompleted += HandleSwapCompleted;
            _swapController.OnSwapFailed += OnSwapFailed;
            _swapController.OnSwapInvalid += OnSwapInvalid;

            if (_matchController != null)
            {
                _matchController.OnMatchProcessingComplete += HandleMatchProcessingComplete;
                _matchController.OnScoreAdded += HandleScoreAdded;
                _matchController.OnMatchProcessingStarted += OnMatchStarted;
            }
        }

        private void OnDisable()
        {
            _inputController.OnTilePressed -= OnTilePressed;
            _inputController.OnSwipe -= OnSwipe;
            _inputController.OnInputCancelled -= OnInputCancelled;

            _swapController.OnSwapStarted -= OnSwapStarted;
            _swapController.OnSwapCompleted -= HandleSwapCompleted;
            _swapController.OnSwapFailed -= OnSwapFailed;
            _swapController.OnSwapInvalid -= OnSwapInvalid;

            if (_matchController != null)
            {
                _matchController.OnMatchProcessingComplete -= HandleMatchProcessingComplete;
                _matchController.OnScoreAdded -= HandleScoreAdded;
                _matchController.OnMatchProcessingStarted -= OnMatchStarted;
            }
        }

        private void OnTilePressed(Vector2Int gridPos)
        {
            Vector3 worldPos = _grid.GridToWorld(gridPos);
            _selectionVisual.Show(worldPos);
            PlaySound(_selectSound);
        }

        private void OnSwipe(Vector2Int fromPos, SwipeDirection direction)
        {
            _selectionVisual.Hide();
            _swapController.TrySwap(fromPos, direction);
        }

        private void OnInputCancelled()
        {
            _selectionVisual.Hide();
        }

        private void OnSwapStarted()
        {
            PlaySound(_swapSound);
        }

        private void HandleSwapCompleted(Vector2Int posA, Vector2Int posB)
        {
            OnSwapCompleted?.Invoke(posA, posB);
            _matchController?.ProcessMatchesAt(posA, posB);
        }

        private void OnSwapFailed()
        {
            // Swap reverted, no match found
        }

        private void OnSwapInvalid()
        {
            _selectionVisual.Hide();
            PlaySound(_invalidSwapSound);
        }

        private void OnMatchStarted()
        {
            PlaySound(_matchSound);
        }

        private void HandleMatchProcessingComplete()
        {
            OnMatchProcessingComplete?.Invoke();
        }

        private void HandleScoreAdded(int score)
        {
            OnScoreAdded?.Invoke(score);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
