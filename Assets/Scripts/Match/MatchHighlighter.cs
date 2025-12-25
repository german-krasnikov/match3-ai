using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;

namespace Match3.Matching
{
    public class MatchHighlighter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private float _highlightDuration = 1f;
        [SerializeField] private Color _horizontalColor = new(1f, 0.5f, 0f, 0.7f);
        [SerializeField] private Color _verticalColor = new(0f, 0.5f, 1f, 0.7f);
        [SerializeField] private Color _crossColor = new(1f, 0f, 1f, 0.7f);

        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;
        [SerializeField] private MatchFinder _matchFinder;

        private List<Match> _currentMatches = new();
        private float _highlightTimer;

        public void HighlightMatches(List<Match> matches)
        {
            _currentMatches = matches;
            _highlightTimer = _highlightDuration;
        }

        [ContextMenu("Find And Highlight All Matches")]
        public void FindAndHighlightAll()
        {
            if (_matchFinder == null) return;

            var matches = _matchFinder.FindAllMatches();
            HighlightMatches(matches);

            Debug.Log($"[MatchHighlighter] Found {matches.Count} matches:");
            foreach (var match in matches)
                Debug.Log($"  {match}");
        }

        private void Update()
        {
            if (_highlightTimer > 0)
            {
                _highlightTimer -= Time.deltaTime;
                if (_highlightTimer <= 0)
                    _currentMatches.Clear();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _grid == null || _currentMatches.Count == 0)
                return;

            foreach (var match in _currentMatches)
            {
                Gizmos.color = match.Orientation switch
                {
                    MatchOrientation.Horizontal => _horizontalColor,
                    MatchOrientation.Vertical => _verticalColor,
                    MatchOrientation.Cross => _crossColor,
                    _ => Color.white
                };

                foreach (var pos in match.Positions)
                {
                    var worldPos = _grid.GridToWorld(pos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.3f);
                }
            }
        }
    }
}
