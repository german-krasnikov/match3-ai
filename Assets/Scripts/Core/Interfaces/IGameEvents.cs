using System;

namespace Match3.Core
{
    public interface IGameEvents
    {
        event Action OnGameStarted;
        event Action OnGameEnded;
        event Action OnTurnStarted;
        event Action OnTurnEnded;
        event Action<int> OnCascade;
    }
}
