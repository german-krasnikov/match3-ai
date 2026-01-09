// Assets/Scripts/Features/Board/Services/IFallService.cs
using System.Collections.Generic;
using Common;
using Features.Board.Models;

namespace Features.Board.Services
{
    /// <summary>
    /// Calculates fall movements after elements are destroyed.
    /// </summary>
    public interface IFallService
    {
        /// <summary>
        /// Calculate all fall movements for the current board state.
        /// Returns list of (from, to) movements where 'from' has element and 'to' is empty below.
        /// Movements are ordered by column (left to right) and within column by Y (bottom to top).
        /// </summary>
        List<FallMove> CalculateFalls(BoardModel board);

        /// <summary>
        /// Get positions at the top of each column that will be empty after falls.
        /// These positions need new elements spawned (used in Step 9).
        /// </summary>
        List<GridPosition> GetEmptyTopPositions(BoardModel board);
    }

    /// <summary>
    /// Represents a single fall movement.
    /// </summary>
    public readonly struct FallMove
    {
        public readonly GridPosition From;
        public readonly GridPosition To;

        public FallMove(GridPosition from, GridPosition to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"{From} -> {To}";
    }
}
