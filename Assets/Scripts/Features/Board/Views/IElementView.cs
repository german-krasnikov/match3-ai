// Assets/Scripts/Features/Board/Views/IElementView.cs
using System;
using Common;
using UnityEngine;

namespace Features.Board.Views
{
    public interface IElementView
    {
        GridPosition Position { get; }
        ElementType Type { get; }

        void Initialize(GridPosition pos, ElementType type, Sprite sprite);
        void SetSprite(Sprite sprite);
        void UpdatePosition(Vector3 worldPosition);
        void SetActive(bool active);
        void Destroy();

        /// <summary>
        /// Animate movement to target position over duration seconds.
        /// </summary>
        void MoveTo(Vector3 targetPosition, float duration, Action onComplete);

        /// <summary>
        /// Update logical grid position (after swap/move completes).
        /// </summary>
        void SetGridPosition(GridPosition newPos);

        event Action<GridPosition> OnClicked;
        event Action<GridPosition> OnDragStart;
        event Action<GridPosition> OnDragEnd;
    }
}
