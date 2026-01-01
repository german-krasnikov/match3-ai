using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Gem;

namespace Match3.Match
{
    /// <summary>
    /// Finds matches on the board. Stateless, pure logic.
    /// </summary>
    public class MatchSystem
    {
        private const int MinMatchLength = 3;

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Finds all matches on the board.
        /// Returns merged list (L/T-shapes combined).
        /// </summary>
        public List<MatchData> FindAllMatches(BoardData board)
        {
            var rawMatches = new List<MatchData>();

            // Scan all horizontal lines
            for (int y = 0; y < board.Height; y++)
            {
                ScanLine(board, new Vector2Int(0, y), Vector2Int.right, rawMatches);
            }

            // Scan all vertical lines
            for (int x = 0; x < board.Width; x++)
            {
                ScanLine(board, new Vector2Int(x, 0), Vector2Int.up, rawMatches);
            }

            // Merge overlapping matches (L/T-shapes)
            return MergeOverlapping(rawMatches);
        }

        /// <summary>
        /// Finds matches that include given position.
        /// Used for swap validation.
        /// </summary>
        public List<MatchData> FindMatchesAt(BoardData board, Vector2Int pos)
        {
            var gemType = board.GetGemType(pos);
            if (gemType == null) return new List<MatchData>();

            var rawMatches = new List<MatchData>();

            // Find horizontal match through pos
            var hMatch = FindLineThrough(board, pos, Vector2Int.right, gemType.Value);
            if (hMatch.Count >= MinMatchLength)
            {
                rawMatches.Add(new MatchData(hMatch, gemType.Value));
            }

            // Find vertical match through pos
            var vMatch = FindLineThrough(board, pos, Vector2Int.up, gemType.Value);
            if (vMatch.Count >= MinMatchLength)
            {
                rawMatches.Add(new MatchData(vMatch, gemType.Value));
            }

            // Merge if they overlap at pos (L/T-shape)
            return MergeOverlapping(rawMatches);
        }

        /// <summary>
        /// Returns true if board has any match.
        /// Optimized for early exit.
        /// </summary>
        public bool HasAnyMatch(BoardData board)
        {
            // Check horizontal lines
            for (int y = 0; y < board.Height; y++)
            {
                if (HasMatchInLine(board, new Vector2Int(0, y), Vector2Int.right))
                    return true;
            }

            // Check vertical lines
            for (int x = 0; x < board.Width; x++)
            {
                if (HasMatchInLine(board, new Vector2Int(x, 0), Vector2Int.up))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if swap would create a match.
        /// Does NOT modify board.
        /// </summary>
        public bool WouldMatchAfterSwap(BoardData board, Vector2Int a, Vector2Int b)
        {
            // Temporarily swap in logic (not actual board)
            var typeA = board.GetGemType(a);
            var typeB = board.GetGemType(b);

            if (typeA == null || typeB == null) return false;

            // Check if A at position B creates match
            if (WouldMatchAt(board, b, typeA.Value, a))
                return true;

            // Check if B at position A creates match
            if (WouldMatchAt(board, a, typeB.Value, b))
                return true;

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // PRIVATE: Line Scanning
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Scans a line and adds all matches found to the list.
        /// </summary>
        private void ScanLine(BoardData board, Vector2Int start, Vector2Int dir, List<MatchData> results)
        {
            GemType? currentType = null;
            var currentPositions = new List<Vector2Int>();
            var pos = start;

            while (board.IsValidPosition(pos))
            {
                var type = board.GetGemType(pos);

                if (type == null)
                {
                    // Empty cell - flush current sequence
                    FlushMatch(currentPositions, currentType, results);
                    currentPositions.Clear();
                    currentType = null;
                }
                else if (type == currentType)
                {
                    // Continue sequence
                    currentPositions.Add(pos);
                }
                else
                {
                    // New type - flush previous, start new
                    FlushMatch(currentPositions, currentType, results);
                    currentPositions.Clear();
                    currentPositions.Add(pos);
                    currentType = type;
                }

                pos += dir;
            }

            // Flush last sequence
            FlushMatch(currentPositions, currentType, results);
        }

        /// <summary>
        /// Adds match to results if has minimum length.
        /// </summary>
        private void FlushMatch(List<Vector2Int> positions, GemType? type, List<MatchData> results)
        {
            if (type == null) return;
            if (positions.Count < MinMatchLength) return;

            results.Add(new MatchData(new List<Vector2Int>(positions), type.Value));
        }

        /// <summary>
        /// Finds contiguous line of same type through position.
        /// Expands in both directions along axis.
        /// </summary>
        private List<Vector2Int> FindLineThrough(BoardData board, Vector2Int pos, Vector2Int dir, GemType type)
        {
            var result = new List<Vector2Int> { pos };

            // Expand in positive direction
            var check = pos + dir;
            while (board.IsValidPosition(check) && board.GetGemType(check) == type)
            {
                result.Add(check);
                check += dir;
            }

            // Expand in negative direction
            check = pos - dir;
            while (board.IsValidPosition(check) && board.GetGemType(check) == type)
            {
                result.Add(check);
                check -= dir;
            }

            return result;
        }

        /// <summary>
        /// Returns true if line has at least one match.
        /// Early exit optimization.
        /// </summary>
        private bool HasMatchInLine(BoardData board, Vector2Int start, Vector2Int dir)
        {
            GemType? currentType = null;
            int count = 0;
            var pos = start;

            while (board.IsValidPosition(pos))
            {
                var type = board.GetGemType(pos);

                if (type == null)
                {
                    currentType = null;
                    count = 0;
                }
                else if (type == currentType)
                {
                    count++;
                    if (count >= MinMatchLength) return true;
                }
                else
                {
                    currentType = type;
                    count = 1;
                }

                pos += dir;
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // PRIVATE: Swap Validation
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Checks if placing targetType at pos would create match.
        /// ignorePos is treated as empty (the swapped gem's original position).
        /// </summary>
        private bool WouldMatchAt(BoardData board, Vector2Int pos, GemType targetType, Vector2Int ignorePos)
        {
            // Check horizontal
            int hCount = 1;
            hCount += CountInDirection(board, pos, Vector2Int.right, targetType, ignorePos);
            hCount += CountInDirection(board, pos, Vector2Int.left, targetType, ignorePos);
            if (hCount >= MinMatchLength) return true;

            // Check vertical
            int vCount = 1;
            vCount += CountInDirection(board, pos, Vector2Int.up, targetType, ignorePos);
            vCount += CountInDirection(board, pos, Vector2Int.down, targetType, ignorePos);
            if (vCount >= MinMatchLength) return true;

            return false;
        }

        /// <summary>
        /// Counts consecutive gems of same type in direction.
        /// Stops at different type, empty, or ignorePos.
        /// </summary>
        private int CountInDirection(BoardData board, Vector2Int start, Vector2Int dir, GemType type, Vector2Int ignorePos)
        {
            int count = 0;
            var pos = start + dir;

            while (board.IsValidPosition(pos))
            {
                if (pos == ignorePos) break;  // Treat as empty
                if (board.GetGemType(pos) != type) break;

                count++;
                pos += dir;
            }

            return count;
        }

        // ═══════════════════════════════════════════════════════════
        // PRIVATE: Merge Overlapping
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Merges matches that overlap and have same type.
        /// Creates L-shapes and T-shapes from intersecting lines.
        /// </summary>
        private List<MatchData> MergeOverlapping(List<MatchData> matches)
        {
            if (matches.Count <= 1) return matches;

            var result = new List<MatchData>();
            var used = new HashSet<int>();

            for (int i = 0; i < matches.Count; i++)
            {
                if (used.Contains(i)) continue;

                var merged = new HashSet<Vector2Int>(matches[i].Positions);
                var type = matches[i].Type;
                bool changed = true;

                // Keep merging until no more overlaps found
                while (changed)
                {
                    changed = false;
                    for (int j = i + 1; j < matches.Count; j++)
                    {
                        if (used.Contains(j)) continue;
                        if (matches[j].Type != type) continue;

                        // Check overlap
                        if (Overlaps(merged, matches[j].Positions))
                        {
                            foreach (var pos in matches[j].Positions)
                            {
                                merged.Add(pos);
                            }
                            used.Add(j);
                            changed = true;
                        }
                    }
                }

                result.Add(new MatchData(new List<Vector2Int>(merged), type));
                used.Add(i);
            }

            return result;
        }

        /// <summary>
        /// Returns true if any position in list is in set.
        /// </summary>
        private bool Overlaps(HashSet<Vector2Int> set, IReadOnlyList<Vector2Int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (set.Contains(list[i])) return true;
            }
            return false;
        }
    }
}
