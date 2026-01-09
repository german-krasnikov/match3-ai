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

        event Action<GridPosition> OnClicked;
        event Action<GridPosition> OnDragStart;
        event Action<GridPosition> OnDragEnd;
    }
}
