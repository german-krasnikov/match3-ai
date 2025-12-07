using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Match3.Data;
using Match3.Components.Board;

namespace Match3.Core
{
    public class MatchDetector : MonoBehaviour
    {
        public event Action<MatchResult> OnMatchesFound;

        [Header("Settings")]
        [SerializeField] private int _minMatchLength = 3;
        [SerializeField] private bool _detectLShapes = true;
        [SerializeField] private bool _detectTShapes = true;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;

        public MatchResult FindAllMatches()
        {
            var result = new MatchResult();
            var horizontal = FindHorizontalMatches();
            var vertical = FindVerticalMatches();
            var merged = MergeMatches(horizontal, vertical);

            foreach (var match in merged)
            {
                match.Type = DetermineMatchType(match);
                match.Center = CalculateCenter(match);
                result.AddMatch(match);
            }

            if (result.HasMatches)
                OnMatchesFound?.Invoke(result);

            return result;
        }

        public MatchResult FindMatchesAt(params Vector2Int[] positions)
        {
            var result = new MatchResult();
            var processed = new HashSet<Vector2Int>();

            foreach (var pos in positions)
            {
                if (processed.Contains(pos)) continue;

                var hMatch = FindLineFromPoint(pos, Vector2Int.right);
                var vMatch = FindLineFromPoint(pos, Vector2Int.up);

                if (hMatch != null)
                    foreach (var p in hMatch.Positions) processed.Add(p);
                if (vMatch != null)
                    foreach (var p in vMatch.Positions) processed.Add(p);

                var merged = MergeIfIntersect(hMatch, vMatch);
                foreach (var m in merged)
                {
                    m.Type = DetermineMatchType(m);
                    m.Center = CalculateCenter(m);
                    result.AddMatch(m);
                }
            }

            if (result.HasMatches)
                OnMatchesFound?.Invoke(result);

            return result;
        }

        public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
        {
            var tileA = GetTileType(posA);
            var tileB = GetTileType(posB);

            return CheckMatchAtPosition(posA, tileB, posB) ||
                   CheckMatchAtPosition(posB, tileA, posA);
        }

        private List<MatchData> FindHorizontalMatches()
        {
            var matches = new List<MatchData>();

            for (int y = 0; y < _grid.Height; y++)
            {
                int x = 0;
                while (x < _grid.Width)
                {
                    var startType = GetTileType(x, y);
                    if (startType == TileType.None || startType == TileType.Blocker)
                    {
                        x++;
                        continue;
                    }

                    int length = 1;
                    while (x + length < _grid.Width && GetTileType(x + length, y) == startType)
                        length++;

                    if (length >= _minMatchLength)
                    {
                        var match = new MatchData { TileType = startType };
                        for (int i = 0; i < length; i++)
                            match.AddPosition(new Vector2Int(x + i, y));
                        matches.Add(match);
                    }

                    x += length;
                }
            }

            return matches;
        }

        private List<MatchData> FindVerticalMatches()
        {
            var matches = new List<MatchData>();

            for (int x = 0; x < _grid.Width; x++)
            {
                int y = 0;
                while (y < _grid.Height)
                {
                    var startType = GetTileType(x, y);
                    if (startType == TileType.None || startType == TileType.Blocker)
                    {
                        y++;
                        continue;
                    }

                    int length = 1;
                    while (y + length < _grid.Height && GetTileType(x, y + length) == startType)
                        length++;

                    if (length >= _minMatchLength)
                    {
                        var match = new MatchData { TileType = startType };
                        for (int i = 0; i < length; i++)
                            match.AddPosition(new Vector2Int(x, y + i));
                        matches.Add(match);
                    }

                    y += length;
                }
            }

            return matches;
        }

        private MatchData FindLineFromPoint(Vector2Int start, Vector2Int direction)
        {
            var type = GetTileType(start);
            if (type == TileType.None || type == TileType.Blocker) return null;

            var positions = new List<Vector2Int> { start };

            var pos = start + direction;
            while (_grid.IsValidPosition(pos) && GetTileType(pos) == type)
            {
                positions.Add(pos);
                pos += direction;
            }

            pos = start - direction;
            while (_grid.IsValidPosition(pos) && GetTileType(pos) == type)
            {
                positions.Add(pos);
                pos -= direction;
            }

            if (positions.Count < _minMatchLength)
                return null;

            var match = new MatchData { TileType = type };
            foreach (var p in positions)
                match.AddPosition(p);

            return match;
        }

        private List<MatchData> MergeMatches(List<MatchData> horizontal, List<MatchData> vertical)
        {
            var all = new List<MatchData>();
            all.AddRange(horizontal);
            all.AddRange(vertical);

            var merged = new List<MatchData>();
            var used = new bool[all.Count];

            for (int i = 0; i < all.Count; i++)
            {
                if (used[i]) continue;

                var current = all[i];
                used[i] = true;

                bool foundMerge;
                do
                {
                    foundMerge = false;
                    for (int j = 0; j < all.Count; j++)
                    {
                        if (used[j]) continue;
                        if (all[j].TileType != current.TileType) continue;

                        if (current.Intersects(all[j]))
                        {
                            current.Merge(all[j]);
                            used[j] = true;
                            foundMerge = true;
                        }
                    }
                } while (foundMerge);

                merged.Add(current);
            }

            return merged;
        }

        private List<MatchData> MergeIfIntersect(MatchData a, MatchData b)
        {
            var result = new List<MatchData>();

            if (a == null && b == null) return result;
            if (a == null) { result.Add(b); return result; }
            if (b == null) { result.Add(a); return result; }

            if (a.TileType == b.TileType && a.Intersects(b))
            {
                a.Merge(b);
                result.Add(a);
            }
            else
            {
                result.Add(a);
                result.Add(b);
            }

            return result;
        }

        private MatchType DetermineMatchType(MatchData match)
        {
            if (IsSimpleLine(match))
            {
                return match.Count switch
                {
                    3 => MatchType.Line3,
                    4 => MatchType.Line4,
                    >= 5 => MatchType.Line5,
                    _ => MatchType.None
                };
            }

            if (IsCross(match)) return MatchType.Cross;
            if (_detectTShapes && IsTShape(match)) return MatchType.TShape;
            if (_detectLShapes && IsLShape(match)) return MatchType.LShape;

            return match.Count >= 5 ? MatchType.Line5 :
                   match.Count >= 4 ? MatchType.Line4 :
                   MatchType.Line3;
        }

        private bool IsSimpleLine(MatchData match)
        {
            if (match.Count < 3) return false;

            var positions = match.Positions;
            bool allSameY = positions.All(p => p.y == positions[0].y);
            bool allSameX = positions.All(p => p.x == positions[0].x);

            return allSameY || allSameX;
        }

        private bool IsLShape(MatchData match)
        {
            if (match.Count < 5) return false;

            var positions = match.Positions;
            foreach (var pos in positions)
            {
                int horizontal = positions.Count(p => p.y == pos.y);
                int vertical = positions.Count(p => p.x == pos.x);
                if (horizontal >= 3 && vertical >= 3)
                    return true;
            }
            return false;
        }

        private bool IsTShape(MatchData match)
        {
            if (match.Count < 5) return false;

            var positions = match.Positions;
            foreach (var pos in positions)
            {
                int horizontal = positions.Count(p => p.y == pos.y);
                int vertical = positions.Count(p => p.x == pos.x);

                if (horizontal >= 3 && vertical >= 2)
                {
                    var hLine = positions.Where(p => p.y == pos.y).OrderBy(p => p.x).ToList();
                    int idx = hLine.IndexOf(pos);
                    if (idx > 0 && idx < hLine.Count - 1)
                        return true;
                }

                if (vertical >= 3 && horizontal >= 2)
                {
                    var vLine = positions.Where(p => p.x == pos.x).OrderBy(p => p.y).ToList();
                    int idx = vLine.IndexOf(pos);
                    if (idx > 0 && idx < vLine.Count - 1)
                        return true;
                }
            }
            return false;
        }

        private bool IsCross(MatchData match)
        {
            if (match.Count < 5) return false;

            var positions = match.Positions;
            foreach (var pos in positions)
            {
                bool hasUp = positions.Contains(pos + Vector2Int.up);
                bool hasDown = positions.Contains(pos + Vector2Int.down);
                bool hasLeft = positions.Contains(pos + Vector2Int.left);
                bool hasRight = positions.Contains(pos + Vector2Int.right);

                if (hasUp && hasDown && hasLeft && hasRight)
                    return true;
            }
            return false;
        }

        private Vector2Int CalculateCenter(MatchData match)
        {
            var positions = match.Positions;
            Vector2Int best = positions[0];
            int maxNeighbors = 0;

            foreach (var pos in positions)
            {
                int neighbors = 0;
                if (positions.Contains(pos + Vector2Int.up)) neighbors++;
                if (positions.Contains(pos + Vector2Int.down)) neighbors++;
                if (positions.Contains(pos + Vector2Int.left)) neighbors++;
                if (positions.Contains(pos + Vector2Int.right)) neighbors++;

                if (neighbors > maxNeighbors)
                {
                    maxNeighbors = neighbors;
                    best = pos;
                }
            }

            return best;
        }

        private TileType GetTileType(Vector2Int pos) => GetTileType(pos.x, pos.y);

        private TileType GetTileType(int x, int y)
        {
            var cell = _grid.GetCell(x, y);
            return cell?.CurrentTile?.Type ?? TileType.None;
        }

        private bool CheckMatchAtPosition(Vector2Int pos, TileType type, Vector2Int otherSwappedPos)
        {
            if (type == TileType.None || type == TileType.Blocker) return false;

            int hCount = 1;
            var p = pos + Vector2Int.left;
            while (_grid.IsValidPosition(p) && GetTypeForSwapCheck(p, pos, otherSwappedPos, type) == type)
            {
                hCount++;
                p += Vector2Int.left;
            }
            p = pos + Vector2Int.right;
            while (_grid.IsValidPosition(p) && GetTypeForSwapCheck(p, pos, otherSwappedPos, type) == type)
            {
                hCount++;
                p += Vector2Int.right;
            }

            if (hCount >= _minMatchLength) return true;

            int vCount = 1;
            p = pos + Vector2Int.down;
            while (_grid.IsValidPosition(p) && GetTypeForSwapCheck(p, pos, otherSwappedPos, type) == type)
            {
                vCount++;
                p += Vector2Int.down;
            }
            p = pos + Vector2Int.up;
            while (_grid.IsValidPosition(p) && GetTypeForSwapCheck(p, pos, otherSwappedPos, type) == type)
            {
                vCount++;
                p += Vector2Int.up;
            }

            return vCount >= _minMatchLength;
        }

        private TileType GetTypeForSwapCheck(Vector2Int checkPos, Vector2Int swappedPos, Vector2Int otherPos, TileType swappedType)
        {
            if (checkPos == swappedPos) return swappedType;
            if (checkPos == otherPos) return GetTileType(swappedPos);
            return GetTileType(checkPos);
        }
    }
}
