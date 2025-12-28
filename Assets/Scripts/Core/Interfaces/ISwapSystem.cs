using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Match3.Core
{
    public interface ISwapSystem
    {
        bool CanSwap(Vector2Int pos1, Vector2Int pos2);
        Task<bool> TrySwap(Vector2Int pos1, Vector2Int pos2);
        Task SwapBack(Vector2Int pos1, Vector2Int pos2);

        event Action<Vector2Int, Vector2Int> OnSwapStarted;
        event Action<Vector2Int, Vector2Int> OnSwapCompleted;
    }
}
