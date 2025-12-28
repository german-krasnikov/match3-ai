using System;
using System.Threading.Tasks;

namespace Match3.Core
{
    public interface IGravitySystem
    {
        Task ApplyGravity();
        bool HasEmptyCells();

        event Action OnGravityStarted;
        event Action OnGravityCompleted;
    }
}
