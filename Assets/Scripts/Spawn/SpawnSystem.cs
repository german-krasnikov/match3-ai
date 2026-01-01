using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Gem;

namespace Match3.Spawn
{
    public class SpawnSystem
    {
        private readonly GemConfig _config;

        public SpawnSystem(GemConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates gem type that won't create match at position.
        /// </summary>
        public GemType GenerateType(Vector2Int pos, BoardData board)
        {
            var forbidden = GetForbiddenTypes(pos, board);
            return GetRandomTypeExcluding(forbidden);
        }

        /// <summary>
        /// Returns types that would create a match at position.
        /// </summary>
        private HashSet<GemType> GetForbiddenTypes(Vector2Int pos, BoardData board)
        {
            var forbidden = new HashSet<GemType>();

            // Check horizontal (2 gems to the left)
            var leftType = CheckConsecutive(pos, Vector2Int.left, board, 2);
            if (leftType.HasValue)
                forbidden.Add(leftType.Value);

            // Check vertical (2 gems below)
            var belowType = CheckConsecutive(pos, Vector2Int.down, board, 2);
            if (belowType.HasValue)
                forbidden.Add(belowType.Value);

            return forbidden;
        }

        /// <summary>
        /// Checks if there are N consecutive gems of same type in direction.
        /// Returns that type if found, null otherwise.
        /// </summary>
        private GemType? CheckConsecutive(Vector2Int pos, Vector2Int direction, BoardData board, int count)
        {
            GemType? matchType = null;
            int matches = 0;

            for (int i = 1; i <= count; i++)
            {
                var checkPos = pos + direction * i;
                var type = board.GetGemType(checkPos);

                if (!type.HasValue)
                    return null;

                if (matchType == null)
                {
                    matchType = type.Value;
                    matches = 1;
                }
                else if (type.Value == matchType.Value)
                {
                    matches++;
                }
                else
                {
                    return null;
                }
            }

            return matches >= count ? matchType : null;
        }

        /// <summary>
        /// Gets random type excluding forbidden ones.
        /// Falls back to any random type if all forbidden (edge case).
        /// </summary>
        private GemType GetRandomTypeExcluding(HashSet<GemType> forbidden)
        {
            var allTypes = _config.GetAllTypes();

            // Build list of allowed types
            var allowed = new List<GemType>();
            foreach (var type in allTypes)
            {
                if (!forbidden.Contains(type))
                    allowed.Add(type);
            }

            // Fallback if somehow all types are forbidden
            if (allowed.Count == 0)
                return _config.GetRandomType();

            int index = Random.Range(0, allowed.Count);
            return allowed[index];
        }
    }
}
