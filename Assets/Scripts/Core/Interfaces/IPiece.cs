using System;
using UnityEngine;

namespace Match3.Core
{
    public interface IPiece
    {
        PieceType Type { get; }
        GridPosition Position { get; set; }
        GameObject GameObject { get; }

        void SetWorldPosition(Vector3 position);

        event Action<IPiece> OnDestroyed;
    }
}
