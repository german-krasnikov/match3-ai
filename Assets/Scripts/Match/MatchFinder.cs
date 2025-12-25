using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Elements;

namespace Match3.Matching
{
    public class MatchFinder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _minMatchLength = 3;

        [Header("Dependencies")]
        [SerializeField] private BoardComponent _board;

        private readonly List<Match> _matchBuffer = new();
        private readonly List<Vector2Int> _lineBuffer = new();

        public List<Match> FindAllMatches()
        {
            _matchBuffer.Clear();

            for (int y = 0; y < _board.Height; y++)
                FindHorizontalMatchesInRow(y, _matchBuffer);

            for (int x = 0; x < _board.Width; x++)
                FindVerticalMatchesInColumn(x, _matchBuffer);

            return MergeIntersectingMatches(_matchBuffer);
        }

        public List<Match> FindMatchesAt(Vector2Int position)
        {
            _matchBuffer.Clear();

            var horizontal = FindHorizontalMatchAt(position);
            if (horizontal.IsValid)
                _matchBuffer.Add(horizontal);

            var vertical = FindVerticalMatchAt(position);
            if (vertical.IsValid)
                _matchBuffer.Add(vertical);

            return MergeIntersectingMatches(_matchBuffer);
        }

        public bool WouldCreateMatch(Vector2Int posA, Vector2Int posB)
        {
            return FindMatchesAt(posA).Count > 0 || FindMatchesAt(posB).Count > 0;
        }

        private void FindHorizontalMatchesInRow(int y, List<Match> results)
        {
            int x = 0;
            while (x < _board.Width)
            {
                var type = _board.GetElementType(new Vector2Int(x, y));
                if (type == null || type == ElementType.None)
                {
                    x++;
                    continue;
                }

                _lineBuffer.Clear();
                _lineBuffer.Add(new Vector2Int(x, y));

                int nextX = x + 1;
                while (nextX < _board.Width && _board.GetElementType(new Vector2Int(nextX, y)) == type)
                {
                    _lineBuffer.Add(new Vector2Int(nextX, y));
                    nextX++;
                }

                if (_lineBuffer.Count >= _minMatchLength)
                    results.Add(new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Horizontal));

                x = nextX;
            }
        }

        private void FindVerticalMatchesInColumn(int x, List<Match> results)
        {
            int y = 0;
            while (y < _board.Height)
            {
                var type = _board.GetElementType(new Vector2Int(x, y));
                if (type == null || type == ElementType.None)
                {
                    y++;
                    continue;
                }

                _lineBuffer.Clear();
                _lineBuffer.Add(new Vector2Int(x, y));

                int nextY = y + 1;
                while (nextY < _board.Height && _board.GetElementType(new Vector2Int(x, nextY)) == type)
                {
                    _lineBuffer.Add(new Vector2Int(x, nextY));
                    nextY++;
                }

                if (_lineBuffer.Count >= _minMatchLength)
                    results.Add(new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Vertical));

                y = nextY;
            }
        }

        private Match FindHorizontalMatchAt(Vector2Int pos)
        {
            var type = _board.GetElementType(pos);
            if (type == null || type == ElementType.None)
                return default;

            _lineBuffer.Clear();
            _lineBuffer.Add(pos);

            for (int left = pos.x - 1; left >= 0; left--)
            {
                if (_board.GetElementType(new Vector2Int(left, pos.y)) != type) break;
                _lineBuffer.Insert(0, new Vector2Int(left, pos.y));
            }

            for (int right = pos.x + 1; right < _board.Width; right++)
            {
                if (_board.GetElementType(new Vector2Int(right, pos.y)) != type) break;
                _lineBuffer.Add(new Vector2Int(right, pos.y));
            }

            return _lineBuffer.Count >= _minMatchLength
                ? new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Horizontal)
                : default;
        }

        private Match FindVerticalMatchAt(Vector2Int pos)
        {
            var type = _board.GetElementType(pos);
            if (type == null || type == ElementType.None)
                return default;

            _lineBuffer.Clear();
            _lineBuffer.Add(pos);

            for (int down = pos.y - 1; down >= 0; down--)
            {
                if (_board.GetElementType(new Vector2Int(pos.x, down)) != type) break;
                _lineBuffer.Insert(0, new Vector2Int(pos.x, down));
            }

            for (int up = pos.y + 1; up < _board.Height; up++)
            {
                if (_board.GetElementType(new Vector2Int(pos.x, up)) != type) break;
                _lineBuffer.Add(new Vector2Int(pos.x, up));
            }

            return _lineBuffer.Count >= _minMatchLength
                ? new Match(type.Value, new List<Vector2Int>(_lineBuffer), MatchOrientation.Vertical)
                : default;
        }

        private List<Match> MergeIntersectingMatches(List<Match> matches)
        {
            if (matches.Count <= 1)
                return new List<Match>(matches);

            var result = new List<Match>();
            var merged = new bool[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (merged[i]) continue;

                var current = matches[i];

                for (int j = i + 1; j < matches.Count; j++)
                {
                    if (merged[j] || matches[i].Type != matches[j].Type) continue;

                    if (MatchesIntersect(current, matches[j]))
                    {
                        current = Match.Merge(current, matches[j]);
                        merged[j] = true;
                    }
                }

                result.Add(current);
            }

            return result;
        }

        private bool MatchesIntersect(Match a, Match b)
        {
            foreach (var posA in a.Positions)
                foreach (var posB in b.Positions)
                    if (posA == posB) return true;
            return false;
        }
    }
}
