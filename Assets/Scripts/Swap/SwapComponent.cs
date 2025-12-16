using System;
using UnityEngine;

public class SwapComponent : MonoBehaviour
{
    public event Action<Cell, Cell> OnSwapStarted;
    public event Action<Cell, Cell, bool> OnSwapCompleted;

    [SerializeField] private SwapAnimationComponent _animation;

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
        if (_isSwapping) return;

        _isSwapping = true;
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
        _isSwapping = false;
        OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, true);
    }

    private void OnSwapBackComplete()
    {
        _isSwapping = false;
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
