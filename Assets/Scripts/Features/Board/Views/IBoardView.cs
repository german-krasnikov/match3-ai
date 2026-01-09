// Assets/Scripts/Features/Board/Views/IBoardView.cs
using System;
using System.Collections.Generic;
using Common;
using Features.Board.Services;

namespace Features.Board.Views
{
    public interface IBoardView
    {
        void Initialize(int width, int height, float cellSize);
        void CreateElement(GridPosition pos, ElementType type);
        void RemoveElement(GridPosition pos);
        void Clear();

        void SwapElements(GridPosition from, GridPosition to, Action onComplete);
        void DestroyElements(List<GridPosition> positions, Action onComplete);
        void MoveElement(GridPosition from, GridPosition to, Action onComplete);
        void MoveElements(List<FallMove> moves, Action onComplete);

        /// <summary>
        /// Spawn element with fall-from-top animation.
        /// </summary>
        void SpawnElement(GridPosition pos, ElementType type, int fallDistance, Action onComplete);

        /// <summary>
        /// Spawn multiple elements with fall animation. Calls onComplete when ALL finish.
        /// </summary>
        void SpawnElements(List<SpawnData> spawns, Action onComplete);

        /// <summary>
        /// Spawn a new element at position with fall-in animation from above.
        /// Used during refill phase after falls complete.
        /// </summary>
        void SpawnElement(GridPosition pos, ElementType type, Action onComplete);

        /// <summary>
        /// Spawn multiple elements simultaneously.
        /// Calls onComplete when ALL spawn animations finish.
        /// </summary>
        void SpawnElements(List<(GridPosition pos, ElementType type)> spawns, Action onComplete);

        event Action<GridPosition> OnCellClicked;
        event Action<GridPosition, GridPosition> OnSwapAttempted;
    }

    public readonly struct SpawnData
    {
        public readonly GridPosition Position;
        public readonly ElementType Type;
        public readonly int FallDistance;

        public SpawnData(GridPosition position, ElementType type, int fallDistance)
        {
            Position = position;
            Type = type;
            FallDistance = fallDistance;
        }
    }
}
