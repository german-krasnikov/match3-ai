using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Core;
using Match3.Grid;

namespace Match3.Match
{
    public class MatchDetectionComponent : MonoBehaviour, IMatchDetection
    {
        public event Action<List<Vector2Int>> OnMatchesFound;

        [Header("Settings")]
        [SerializeField] private int _minMatchLength = 3;

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;

        public List<Vector2Int> FindAllMatches()
        {
            var matches = new HashSet<Vector2Int>();

            FindHorizontalMatches(matches);
            FindVerticalMatches(matches);

            var result = new List<Vector2Int>(matches);

            if (result.Count > 0)
                OnMatchesFound?.Invoke(result);

            return result;
        }

        public List<Vector2Int> FindMatchesAt(Vector2Int pos)
        {
            var matches = new HashSet<Vector2Int>();

            var horizontal = GetMatchLineThrough(pos, Vector2Int.right);
            if (horizontal.Count >= _minMatchLength)
                foreach (var p in horizontal)
                    matches.Add(p);

            var vertical = GetMatchLineThrough(pos, Vector2Int.up);
            if (vertical.Count >= _minMatchLength)
                foreach (var p in vertical)
                    matches.Add(p);

            return new List<Vector2Int>(matches);
        }

        public bool HasAnyMatch()
        {
            // Горизонтальные
            for (int y = 0; y < _grid.Height; y++)
                for (int x = 0; x <= _grid.Width - _minMatchLength; x++)
                    if (CheckLineMatch(new Vector2Int(x, y), Vector2Int.right))
                        return true;

            // Вертикальные
            for (int x = 0; x < _grid.Width; x++)
                for (int y = 0; y <= _grid.Height - _minMatchLength; y++)
                    if (CheckLineMatch(new Vector2Int(x, y), Vector2Int.up))
                        return true;

            return false;
        }

        public bool WouldCreateMatch(Vector2Int pos, ElementType type)
        {
            // Горизонталь: 2 слева
            if (CountSameType(pos, Vector2Int.left, type) >= 2)
                return true;

            // Горизонталь: 2 справа
            if (CountSameType(pos, Vector2Int.right, type) >= 2)
                return true;

            // Горизонталь: 1+1
            if (CountSameType(pos, Vector2Int.left, type) >= 1 &&
                CountSameType(pos, Vector2Int.right, type) >= 1)
                return true;

            // Вертикаль: 2 снизу
            if (CountSameType(pos, Vector2Int.down, type) >= 2)
                return true;

            // Вертикаль: 2 сверху
            if (CountSameType(pos, Vector2Int.up, type) >= 2)
                return true;

            // Вертикаль: 1+1
            if (CountSameType(pos, Vector2Int.down, type) >= 1 &&
                CountSameType(pos, Vector2Int.up, type) >= 1)
                return true;

            return false;
        }

        private void FindHorizontalMatches(HashSet<Vector2Int> matches)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x <= _grid.Width - _minMatchLength; x++)
                {
                    var line = GetMatchLine(new Vector2Int(x, y), Vector2Int.right);
                    if (line.Count >= _minMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        x += line.Count - 1;
                    }
                }
            }
        }

        private void FindVerticalMatches(HashSet<Vector2Int> matches)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y <= _grid.Height - _minMatchLength; y++)
                {
                    var line = GetMatchLine(new Vector2Int(x, y), Vector2Int.up);
                    if (line.Count >= _minMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        y += line.Count - 1;
                    }
                }
            }
        }

        private List<Vector2Int> GetMatchLine(Vector2Int start, Vector2Int dir)
        {
            var result = new List<Vector2Int>();

            var startEl = _grid.GetElementAt(start);
            if (startEl == null || startEl.Type == ElementType.None)
                return result;

            result.Add(start);

            var current = start + dir;
            while (_grid.IsValidPosition(current))
            {
                var el = _grid.GetElementAt(current);
                if (el == null || el.Type != startEl.Type)
                    break;

                result.Add(current);
                current += dir;
            }

            return result;
        }

        private List<Vector2Int> GetMatchLineThrough(Vector2Int pos, Vector2Int dir)
        {
            var result = new List<Vector2Int>();

            var element = _grid.GetElementAt(pos);
            if (element == null || element.Type == ElementType.None)
                return result;

            var type = element.Type;

            // Найти начало линии
            var start = pos;
            var check = pos - dir;
            while (_grid.IsValidPosition(check))
            {
                var el = _grid.GetElementAt(check);
                if (el == null || el.Type != type)
                    break;
                start = check;
                check -= dir;
            }

            // Собрать всю линию
            var current = start;
            while (_grid.IsValidPosition(current))
            {
                var el = _grid.GetElementAt(current);
                if (el == null || el.Type != type)
                    break;
                result.Add(current);
                current += dir;
            }

            return result;
        }

        private bool CheckLineMatch(Vector2Int start, Vector2Int dir)
        {
            var startEl = _grid.GetElementAt(start);
            if (startEl == null || startEl.Type == ElementType.None)
                return false;

            var type = startEl.Type;

            for (int i = 1; i < _minMatchLength; i++)
            {
                var el = _grid.GetElementAt(start + dir * i);
                if (el == null || el.Type != type)
                    return false;
            }

            return true;
        }

        private int CountSameType(Vector2Int start, Vector2Int dir, ElementType type)
        {
            int count = 0;
            var current = start + dir;

            while (_grid.IsValidPosition(current))
            {
                var el = _grid.GetElementAt(current);
                if (el == null || el.Type != type)
                    break;

                count++;
                current += dir;
            }

            return count;
        }
    }
}
