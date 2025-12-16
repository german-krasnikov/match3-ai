using System;
using System.Collections.Generic;
using UnityEngine;

public class SwapComponent : MonoBehaviour
{
    public event Action<Cell, Cell> OnSwapStarted;
    public event Action<Cell, Cell, bool> OnSwapCompleted;
    public event Action<List<MatchData>> OnMatchesFound;

    [SerializeField] private SwapAnimationComponent _animation;
    [SerializeField] private MatchDetectorComponent _matchDetector;
    [SerializeField] private DestroyComponent _destroy;
    [SerializeField] private GravityComponent _gravity;
    [SerializeField] private FallAnimationComponent _fallAnimation;
    [SerializeField] private RefillComponent _refill;

    private Cell _pendingCellA;
    private Cell _pendingCellB;
    private bool _isSwapping;

    public bool IsSwapping => _isSwapping;

    public void RequestSwap(Cell from, Cell to)
    {
        if (_isSwapping) return;

        if (!IsValidSwap(from, to))
        {
            OnSwapCompleted?.Invoke(from, to, false);
            return;
        }

        _isSwapping = true;
        _pendingCellA = from;
        _pendingCellB = to;

        OnSwapStarted?.Invoke(from, to);

        SwapCellData(from, to);

        _animation.AnimateSwap(from.Element, to.Element, OnSwapAnimationComplete);
    }

    public void SwapBack(Cell a, Cell b)
    {
        _pendingCellA = a;
        _pendingCellB = b;

        SwapCellData(a, b);

        _animation.AnimateSwap(a.Element, b.Element, OnSwapBackComplete);
    }

    private bool IsValidSwap(Cell a, Cell b)
    {
        if (a == null || b == null) return false;
        if (a.IsEmpty || b.IsEmpty) return false;

        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);

        return dx + dy == 1;
    }

    private void SwapCellData(Cell a, Cell b)
    {
        var elementA = a.Element;
        var elementB = b.Element;

        a.Element = elementB;
        b.Element = elementA;

        if (a.Element != null) a.Element.SetGridPosition(a.X, a.Y);
        if (b.Element != null) b.Element.SetGridPosition(b.X, b.Y);
    }

    private void OnSwapAnimationComplete()
    {
        if (_matchDetector == null)
        {
            _isSwapping = false;
            OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
            return;
        }

        var matches = _matchDetector.FindMatches();

        if (matches.Count > 0)
        {
            OnMatchesFound?.Invoke(matches);

            if (_destroy != null)
            {
                _destroy.OnDestructionComplete += OnDestructionComplete;
                _destroy.DestroyMatches(matches);
            }
            else
            {
                _isSwapping = false;
                OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
            }
        }
        else
        {
            // No match - swap back
            SwapBack(_pendingCellA, _pendingCellB);
        }
    }

    private void OnDestructionComplete()
    {
        Debug.Log("[Swap] OnDestructionComplete called");

        if (_destroy != null)
            _destroy.OnDestructionComplete -= OnDestructionComplete;

        Debug.Log($"[Swap] Gravity: {_gravity != null}, Refill: {_refill != null}, FallAnim: {_fallAnimation != null}");

        if (_gravity == null)
        {
            Debug.LogWarning("[Swap] No GravityComponent - skipping gravity");
            _isSwapping = false;
            OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
            return;
        }

        var falls = _gravity.ProcessGravity();
        Debug.Log($"[Swap] Gravity falls: {falls.Count}");

        if (_refill != null)
        {
            var refills = _refill.SpawnNewElements();
            Debug.Log($"[Swap] Refill spawned: {refills.Count}");
            falls.AddRange(refills);
        }

        Debug.Log($"[Swap] Total falls: {falls.Count}");

        if (_fallAnimation != null && falls.Count > 0)
        {
            _fallAnimation.AnimateFalls(falls, OnFallComplete);
        }
        else
        {
            OnFallComplete();
        }
    }

    private void OnFallComplete()
    {
        _isSwapping = false;
        OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
    }

    private void OnSwapBackComplete()
    {
        _isSwapping = false;
        OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, false);
    }

#if UNITY_EDITOR
    [SerializeField] private GridComponent _debugGrid;

    [ContextMenu("Test Swap (0,0) <-> (1,0)")]
    private void TestSwap()
    {
        if (_debugGrid == null) return;
        var a = _debugGrid.GetCell(0, 0);
        var b = _debugGrid.GetCell(1, 0);
        RequestSwap(a, b);
    }
#endif
}
