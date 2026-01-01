using System.Collections.Generic;
using UnityEngine;
using Match3.Gem;

namespace Match3.Match
{
    /// <summary>
    /// Data of a single match (3+ gems of same type in line).
    /// Positions may form line, L-shape, or T-shape after merge.
    /// </summary>
    public readonly struct MatchData
    {
        /// <summary>
        /// All positions in this match.
        /// </summary>
        public readonly IReadOnlyList<Vector2Int> Positions;

        /// <summary>
        /// Gem type of this match.
        /// </summary>
        public readonly GemType Type;

        /// <summary>
        /// Number of gems in this match.
        /// </summary>
        public int Count => Positions.Count;

        /// <summary>
        /// True if match has 4+ gems (special).
        /// </summary>
        public bool IsSpecial => Count >= 4;

        /// <summary>
        /// True if match has 5+ gems (super special).
        /// </summary>
        public bool IsSuperSpecial => Count >= 5;

        public MatchData(IReadOnlyList<Vector2Int> positions, GemType type)
        {
            Positions = positions;
            Type = type;
        }

        public MatchData(List<Vector2Int> positions, GemType type)
        {
            Positions = positions;
            Type = type;
        }

        /// <summary>
        /// Check if this match contains given position.
        /// </summary>
        public bool Contains(Vector2Int pos)
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                if (Positions[i] == pos) return true;
            }
            return false;
        }
    }
}
