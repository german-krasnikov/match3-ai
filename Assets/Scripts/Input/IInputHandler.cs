using System;
using Match3.Core;

namespace Match3.Input
{
    public interface IInputHandler
    {
        event Action<GridPosition, GridPosition> OnSwipeDetected;
        void SetEnabled(bool enabled);
        bool IsEnabled { get; }
    }
}
