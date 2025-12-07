using System;
using UnityEngine;
using Match3.Data;

namespace Match3.Interfaces
{
    public interface ITile
    {
        TileType Type { get; }
        Vector2Int GridPosition { get; }
        bool IsMatchable { get; }
        bool IsMatched { get; set; }

        event Action<ITile> OnDestroyed;
    }
}
