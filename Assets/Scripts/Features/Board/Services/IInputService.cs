// Assets/Scripts/Features/Board/Services/IInputService.cs
using System;
using Common;

namespace Features.Board.Services
{
    public interface IInputService
    {
        event Action<GridPosition, GridPosition> OnSwapRequested;
        void SetInputEnabled(bool enabled);
        void RequestSwap(GridPosition from, GridPosition to);
    }
}
