using System;
using UnityEngine;

namespace Match3.Core
{
    public interface ISpawnSystem
    {
        void FillGrid();
        IGridElement SpawnAt(Vector2Int pos);
        IGridElement SpawnAtTop(int column);

        event Action OnGridFilled;
    }
}
