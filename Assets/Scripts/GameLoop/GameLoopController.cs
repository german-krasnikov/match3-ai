using System;
using System.Collections.Generic;
using UnityEngine;

public class GameLoopController : MonoBehaviour
{
    public event Action<BoardState> OnStateChanged;

    [SerializeField] private InputComponent _input;
    [SerializeField] private SwapComponent _swap;
    [SerializeField] private SwapAnimationComponent _swapAnimation;
    [SerializeField] private MatchDetectorComponent _matchDetector;
    [SerializeField] private DestroyComponent _destroy;
    [SerializeField] private GravityComponent _gravity;
    [SerializeField] private RefillComponent _refill;
    [SerializeField] private FallAnimationComponent _fallAnimation;

    [SerializeField] private int _maxCascadeCount = 20;

    public BoardState CurrentState { get; private set; } = BoardState.Idle;

    private Cell _swapCellA;
    private Cell _swapCellB;
    private int _cascadeCount;

    private void OnEnable()
    {
        if (_input != null)
            _input.OnSwapRequested += OnSwapRequested;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnSwapRequested -= OnSwapRequested;
    }

    private void SetState(BoardState state)
    {
        CurrentState = state;

        if (_input != null)
            _input.IsEnabled = (state == BoardState.Idle);

        OnStateChanged?.Invoke(state);
    }

    private void OnSwapRequested(Cell from, Cell to)
    {
        if (CurrentState != BoardState.Idle) return;

        _swapCellA = from;
        _swapCellB = to;
        _cascadeCount = 0;

        SetState(BoardState.Swapping);
        _swap.RequestSwap(from, to, OnSwapAnimationComplete);
    }

    private void OnSwapAnimationComplete(bool valid)
    {
        if (!valid)
        {
            SetState(BoardState.Idle);
            return;
        }

        CheckMatches();
    }

    private void CheckMatches()
    {
        SetState(BoardState.CheckingMatches);

        var matches = _matchDetector.FindMatches();

        if (matches.Count > 0)
        {
            ProcessMatches(matches);
        }
        else if (_cascadeCount == 0)
        {
            // First swap, no match - swap back
            SetState(BoardState.Swapping);
            _swap.SwapBack(_swapCellA, _swapCellB, OnSwapBackComplete);
        }
        else
        {
            // Cascade finished, no more matches
            SetState(BoardState.Idle);
        }
    }

    private void OnSwapBackComplete()
    {
        SetState(BoardState.Idle);
    }

    private void ProcessMatches(List<MatchData> matches)
    {
        SetState(BoardState.Destroying);
        _destroy.DestroyMatches(matches, OnDestroyComplete);
    }

    private void OnDestroyComplete()
    {
        ProcessGravity();
    }

    private void ProcessGravity()
    {
        SetState(BoardState.Falling);

        var falls = _gravity.ProcessGravity();

        SetState(BoardState.Refilling);

        if (_refill != null)
        {
            var refills = _refill.SpawnNewElements();
            falls.AddRange(refills);
        }

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
        _cascadeCount++;

        if (_cascadeCount >= _maxCascadeCount)
        {
            Debug.LogWarning($"[GameLoop] Max cascade limit ({_maxCascadeCount}) reached!");
            SetState(BoardState.Idle);
            return;
        }

        // Check for new matches (cascade)
        CheckMatches();
    }
}
