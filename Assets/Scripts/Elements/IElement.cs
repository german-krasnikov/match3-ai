using System;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Elements
{
    public interface IElement
    {
        ElementType Type { get; }
        GridPosition Position { get; set; }
        Transform Transform { get; }

        void Initialize(ElementType type, GridPosition position);
        void PlayDestroyAnimation(Action onComplete);
        void MoveTo(Vector3 worldPosition, float duration, Action onComplete = null);
    }
}
