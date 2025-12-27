using System;

namespace Match3.Core
{
    public interface IScoreHandler
    {
        int CurrentScore { get; }

        void AddScore(int matchCount, PieceType type);
        void Reset();

        event Action<int> OnScoreChanged;
    }
}
