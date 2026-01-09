// Assets/Scripts/Features/Board/Services/InputService.cs
using System;
using Common;

namespace Features.Board.Services
{
    public class InputService : IInputService
    {
        private bool _inputEnabled = true;

        public event Action<GridPosition, GridPosition> OnSwapRequested;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        public void RequestSwap(GridPosition from, GridPosition to)
        {
            if (!_inputEnabled) return;
            if (!from.IsValid || !to.IsValid) return;
            if (from.Equals(to)) return;
            if (!AreAdjacent(from, to)) return;

            OnSwapRequested?.Invoke(from, to);
        }

        public static bool AreAdjacent(GridPosition a, GridPosition b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
    }
}
