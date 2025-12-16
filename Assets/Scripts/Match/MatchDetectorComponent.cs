using System.Collections.Generic;
using UnityEngine;

public class MatchDetectorComponent : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;

    private MatchDetector _detector;

    private void Awake()
    {
        _detector = new MatchDetector();
    }

    public List<MatchData> FindMatches()
    {
        return _detector.FindMatches(_grid);
    }

    public bool HasAnyMatch()
    {
        return _detector.HasAnyMatch(_grid);
    }

#if UNITY_EDITOR
    private List<MatchData> _lastMatches = new List<MatchData>();

    [ContextMenu("Find Matches (Debug)")]
    private void DebugFindMatches()
    {
        _detector ??= new MatchDetector();
        _lastMatches = FindMatches();

        if (_lastMatches.Count == 0)
        {
            Debug.Log("<color=yellow>No matches found</color>");
            return;
        }

        Debug.Log($"<color=green>Found {_lastMatches.Count} matches:</color>");
        foreach (var match in _lastMatches)
        {
            string cells = "";
            foreach (var cell in match.Cells)
                cells += $"({cell.X},{cell.Y}) ";
            Debug.Log($"  {match}: {cells}");
        }

        UnityEditor.SceneView.RepaintAll();
    }

    [ContextMenu("Check Has Any Match")]
    private void DebugHasAnyMatch()
    {
        _detector ??= new MatchDetector();
        bool has = HasAnyMatch();
        Debug.Log(has
            ? "<color=red>Matches exist on board</color>"
            : "<color=green>No matches on board</color>");
    }

    private void OnDrawGizmosSelected()
    {
        if (_lastMatches == null || _lastMatches.Count == 0) return;
        if (_grid == null) return;

        Color[] colors = { Color.red, Color.cyan, Color.magenta, Color.yellow, Color.white };

        for (int i = 0; i < _lastMatches.Count; i++)
        {
            var match = _lastMatches[i];
            Gizmos.color = colors[i % colors.Length];

            foreach (var cell in match.Cells)
            {
                Vector3 pos = _grid.GridToWorld(cell.X, cell.Y);
                Gizmos.DrawWireSphere(pos, 0.4f);
            }
        }
    }
#endif
}
