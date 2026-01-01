using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Match;

namespace Match3.Destroy
{
    /// <summary>
    /// Handles gem removal from board data.
    /// Does NOT handle animation — that's DestroyAnimator's job.
    /// </summary>
    public class DestroySystem
    {
        /// <summary>
        /// Fires after gems are removed from data.
        /// Use for scoring, combo counting, etc.
        /// </summary>
        public event Action<List<Vector2Int>> OnGemsDestroyed;

        /// <summary>
        /// Removes gems at given positions from board.
        /// Fires OnGemsDestroyed after all removed.
        /// </summary>
        /// <param name="board">Board data to modify</param>
        /// <param name="positions">Positions to clear</param>
        public void DestroyGems(BoardData board, List<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0)
                return;

            // Remove each gem from data
            // BoardData.RemoveGem fires OnGemRemoved -> BoardView.DestroyGem
            foreach (var pos in positions)
            {
                board.RemoveGem(pos);
            }

            // Fire event for scoring system
            OnGemsDestroyed?.Invoke(positions);
        }

        /// <summary>
        /// Extracts unique positions from list of matches.
        /// Handles overlapping positions (L/T-shapes).
        /// </summary>
        public List<Vector2Int> GetUniquePositions(List<MatchData> matches)
        {
            var unique = new HashSet<Vector2Int>();

            foreach (var match in matches)
            {
                foreach (var pos in match.Positions)
                {
                    unique.Add(pos);
                }
            }

            return new List<Vector2Int>(unique);
        }
    }
}
