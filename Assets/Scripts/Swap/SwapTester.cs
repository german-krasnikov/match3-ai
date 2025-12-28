using UnityEngine;
using Match3.Spawn;
using Match3.Match;

namespace Match3.Swap
{
    /// <summary>
    /// Test component for Swap System. Fills grid, handles input, performs swaps.
    /// </summary>
    public class SwapTester : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SpawnComponent _spawn;
        [SerializeField] private InputComponent _input;
        [SerializeField] private SwapComponent _swap;
        [SerializeField] private MatchDetectionComponent _matchDetection;

        [Header("Settings")]
        [SerializeField] private bool _fillOnStart = true;
        [SerializeField] private bool _autoSwapBack = true;

        private void Start()
        {
            if (_fillOnStart && _spawn != null)
            {
                _spawn.FillGrid();
                Debug.Log("[SwapTester] Grid filled. Click two adjacent cells to swap.");
            }
        }

        private void OnEnable()
        {
            if (_input != null)
                _input.OnSwapRequested += OnSwapRequested;

            if (_swap != null)
            {
                _swap.OnSwapStarted += OnSwapStarted;
                _swap.OnSwapCompleted += OnSwapCompleted;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
                _input.OnSwapRequested -= OnSwapRequested;

            if (_swap != null)
            {
                _swap.OnSwapStarted -= OnSwapStarted;
                _swap.OnSwapCompleted -= OnSwapCompleted;
            }
        }

        private async void OnSwapRequested(Vector2Int pos1, Vector2Int pos2)
        {
            _input.SetInputEnabled(false);

            bool success = await _swap.TrySwap(pos1, pos2);

            if (!success)
            {
                Debug.Log($"[SwapTester] Cannot swap {pos1} and {pos2} - not neighbors");
                _input.SetInputEnabled(true);
                return;
            }

            // Check for matches
            if (_matchDetection != null)
            {
                var matches = _matchDetection.FindAllMatches();
                if (matches.Count > 0)
                {
                    Debug.Log($"[SwapTester] Found {matches.Count} matched cells!");
                }
                else if (_autoSwapBack)
                {
                    Debug.Log("[SwapTester] No matches, swapping back...");
                    await _swap.SwapBack(pos1, pos2);
                }
            }

            _input.SetInputEnabled(true);
        }

        private void OnSwapStarted(Vector2Int pos1, Vector2Int pos2)
        {
            Debug.Log($"[SwapTester] Swap started: {pos1} <-> {pos2}");
        }

        private void OnSwapCompleted(Vector2Int pos1, Vector2Int pos2)
        {
            Debug.Log($"[SwapTester] Swap completed: {pos1} <-> {pos2}");
        }
    }
}
