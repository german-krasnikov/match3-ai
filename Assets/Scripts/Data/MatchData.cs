using System.Collections.Generic;
using UnityEngine;

namespace Match3.Data
{
    public class MatchData
    {
        public List<Vector2Int> Positions { get; } = new();
        public MatchType Type { get; set; } = MatchType.None;
        public TileType TileType { get; set; }
        public Vector2Int Center { get; set; }

        public int Count => Positions.Count;

        public void AddPosition(Vector2Int pos)
        {
            if (!Positions.Contains(pos))
                Positions.Add(pos);
        }

        public void Merge(MatchData other)
        {
            foreach (var pos in other.Positions)
                AddPosition(pos);
        }

        public bool Intersects(MatchData other)
        {
            foreach (var pos in Positions)
            {
                if (other.Positions.Contains(pos))
                    return true;
            }
            return false;
        }
    }
}
