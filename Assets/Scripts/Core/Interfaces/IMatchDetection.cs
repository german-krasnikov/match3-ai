using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core
{
    public interface IMatchDetection
    {
        List<Vector2Int> FindAllMatches();
        List<Vector2Int> FindMatchesAt(Vector2Int pos);
        bool HasAnyMatch();
        bool WouldCreateMatch(Vector2Int pos, ElementType type);

        event Action<List<Vector2Int>> OnMatchesFound;
    }
}
