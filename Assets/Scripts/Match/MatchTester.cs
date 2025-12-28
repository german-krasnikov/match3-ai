using System.Collections.Generic;
using UnityEngine;
using Match3.Core;
using Match3.Grid;
using Match3.Spawn;

namespace Match3.Match
{
    public class MatchTester : MonoBehaviour
    {
        [SerializeField] private GridComponent _grid;
        [SerializeField] private SpawnComponent _spawn;
        [SerializeField] private MatchDetectionComponent _matchDetection;

        [Header("Test Settings")]
        [SerializeField] private bool _fillGridOnStart = true;
        [SerializeField] private bool _testMatchesOnStart = true;

        private void Start()
        {
            if (_fillGridOnStart && _spawn != null)
                _spawn.FillGrid();

            if (_testMatchesOnStart)
                TestFindAllMatches();
        }

        [ContextMenu("Test FindAllMatches")]
        public void TestFindAllMatches()
        {
            var matches = _matchDetection.FindAllMatches();
            Debug.Log($"[MatchTester] FindAllMatches: found {matches.Count} positions");

            if (matches.Count > 0)
            {
                Debug.LogWarning($"[MatchTester] Matches found! Positions: {FormatPositions(matches)}");
                HighlightMatches(matches);
            }
            else
            {
                Debug.Log("[MatchTester] No matches - grid is clean!");
            }
        }

        [ContextMenu("Test HasAnyMatch")]
        public void TestHasAnyMatch()
        {
            bool hasMatch = _matchDetection.HasAnyMatch();
            Debug.Log($"[MatchTester] HasAnyMatch: {hasMatch}");
        }

        [ContextMenu("Create Test Match (Row 0)")]
        public void CreateTestMatch()
        {
            // Создаём горизонтальный матч в нижнем ряду для теста
            Debug.Log("[MatchTester] Creating test match at row 0: positions (0,0), (1,0), (2,0)");

            // Очищаем первые 3 ячейки
            for (int x = 0; x < 3; x++)
                _grid.ClearCell(new Vector2Int(x, 0));

            // Тут нужен доступ к фабрике, пока просто логируем
            Debug.Log("[MatchTester] Cells cleared. Use SpawnComponent to spawn same-type elements manually.");
        }

        private string FormatPositions(List<Vector2Int> positions)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pos in positions)
                sb.Append($"({pos.x},{pos.y}) ");
            return sb.ToString();
        }

        private void HighlightMatches(List<Vector2Int> matches)
        {
            foreach (var pos in matches)
            {
                var element = _grid.GetElementAt(pos);
                if (element?.GameObject != null)
                {
                    var sr = element.GameObject.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = Color.white; // Подсветка
                }
            }
        }
    }
}
