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
    private Action<bool> _swapCallback;

    public bool IsSwapping => _isSwapping;

    public void RequestSwap(Cell from, Cell to, Action<bool> onComplete = null)
    {
        if (_isSwapping)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (!IsValidSwap(from, to))
        {
            OnSwapCompleted?.Invoke(from, to, false);
            onComplete?.Invoke(false);
            return;
        }

        _isSwapping = true;
        _pendingCellA = from;
        _pendingCellB = to;
        _swapCallback = onComplete;

        OnSwapStarted?.Invoke(from, to);
        SwapCellData(from, to);
        _animation.AnimateSwap(from.Element, to.Element, OnSwapAnimationComplete);
    }

    public void SwapBack(Cell a, Cell b, Action onComplete = null)
    {
        _pendingCellA = a;
        _pendingCellB = b;
        _swapCallback = _ => onComplete?.Invoke();

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
        _swapCallback?.Invoke(true);
        _swapCallback = null;
    }

    private void OnSwapBackComplete()
    {
        _isSwapping = false;
        OnSwapCompleted?.Invoke(_pendingCellA, _pendingCellB, false);
        _swapCallback?.Invoke(false);
        _swapCallback = null;
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
