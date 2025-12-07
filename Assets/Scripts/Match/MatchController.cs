using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;
using UnityEngine;

namespace Match3.Match
{
    public class MatchController : MonoBehaviour
    {
        public event Action<List<MatchData>> OnMatchesFound;
        public event Action OnNoMatches;

        private GridData _grid;
        private IMatchFinder _finder;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _finder = new LineMatchFinder();
        }

        public void SetMatchFinder(IMatchFinder finder)
        {
            _finder = finder ?? new LineMatchFinder();
        }

        public List<MatchData> CheckAll()
        {
            var matches = _finder.FindAllMatches(_grid);
            NotifyResults(matches);
            return matches;
        }

        public List<MatchData> CheckAt(GridPosition posA, GridPosition posB)
        {
            var matches = _finder.FindMatchesAt(_grid, new[] { posA, posB });
            NotifyResults(matches);
            return matches;
        }

        public List<MatchData> CheckAt(IEnumerable<GridPosition> positions)
        {
            var matches = _finder.FindMatchesAt(_grid, positions);
            NotifyResults(matches);
            return matches;
        }

        public bool HasMatchAt(GridPosition posA, GridPosition posB)
        {
            return _finder.FindMatchesAt(_grid, new[] { posA, posB }).Count > 0;
        }

        private void NotifyResults(List<MatchData> matches)
        {
            if (matches.Count > 0)
                OnMatchesFound?.Invoke(matches);
            else
                OnNoMatches?.Invoke();
        }
    }
}
