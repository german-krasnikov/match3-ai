using System.Collections.Generic;
using UnityEngine;
using Match3.Elements;

namespace Match3.Matching
{
    public enum MatchOrientation
    {
        Horizontal,
        Vertical,
        Cross
    }

    public readonly struct Match
    {
        public readonly ElementType Type;
        public readonly IReadOnlyList<Vector2Int> Positions;
        public readonly MatchOrientation Orientation;

        public int Count => Positions.Count;
        public bool IsValid => Positions != null && Positions.Count >= 3;

        public Match(ElementType type, List<Vector2Int> positions, MatchOrientation orientation)
        {
            Type = type;
            Positions = positions;
            Orientation = orientation;
        }

        public static Match Merge(Match a, Match b)
        {
            var positions = new HashSet<Vector2Int>(a.Positions);
            foreach (var pos in b.Positions)
                positions.Add(pos);

            return new Match(a.Type, new List<Vector2Int>(positions), MatchOrientation.Cross);
        }

        public override string ToString() => $"Match({Type}, {Count} elements, {Orientation})";
    }
}
