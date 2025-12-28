using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Match3.Core
{
    public interface IDestructionSystem
    {
        Task DestroyElements(List<Vector2Int> positions);
        Task DestroyElement(Vector2Int pos);

        event Action<List<Vector2Int>> OnDestructionStarted;
        event Action<List<Vector2Int>> OnDestructionCompleted;
    }
}
