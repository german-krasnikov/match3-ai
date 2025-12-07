using System.Collections.Generic;
using UnityEngine;

namespace Match3.Data
{
    public class MatchResult
    {
        public List<MatchData> Matches { get; } = new();
        public HashSet<Vector2Int> AllPositions { get; } = new();

        public bool HasMatches => Matches.Count > 0;

        public void AddMatch(MatchData match)
        {
            Matches.Add(match);
            foreach (var pos in match.Positions)
                AllPositions.Add(pos);
        }
    }
}
