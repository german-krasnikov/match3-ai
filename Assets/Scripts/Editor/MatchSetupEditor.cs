using UnityEngine;
using UnityEditor;
using Match3.Board;
using Match3.Match;
using Match3.Gem;

namespace Match3.Editor
{
    public class MatchSetupEditor : EditorWindow
    {
        private BoardView _boardView;
        private MatchSystem _matchSystem;

        [MenuItem("Match3/Setup Match Test")]
        private static void ShowWindow()
        {
            GetWindow<MatchSetupEditor>("Match Test");
        }

        private void OnGUI()
        {
            GUILayout.Label("Match System Tester", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Find Board"))
            {
                FindBoardView();
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(_boardView == null);

            if (GUILayout.Button("Find All Matches"))
            {
                FindAllMatches();
            }

            if (GUILayout.Button("Create Test Board with Matches"))
            {
                CreateTestBoard();
            }

            if (GUILayout.Button("Highlight Matches"))
            {
                HighlightMatches();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (_boardView != null)
            {
                GUILayout.Label($"Board: {_boardView.name}", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("No BoardView found. Click 'Find Board'.", EditorStyles.helpBox);
            }
        }

        private void FindBoardView()
        {
            _boardView = FindObjectOfType<BoardView>();
            _matchSystem = new MatchSystem();

            if (_boardView == null)
            {
                Debug.LogError("[MatchTest] No BoardView found in scene!");
            }
            else
            {
                Debug.Log($"[MatchTest] Found BoardView: {_boardView.name}");
            }
        }

        private void FindAllMatches()
        {
            if (_boardView == null || _boardView.Data == null)
            {
                Debug.LogError("[MatchTest] BoardView or BoardData is null!");
                return;
            }

            var matches = _matchSystem.FindAllMatches(_boardView.Data);
            Debug.Log($"[MatchTest] Found {matches.Count} matches:");

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var positions = string.Join(", ", match.Positions);
                Debug.Log($"  Match {i}: Type={match.Type}, Count={match.Count}, Positions=[{positions}]");
            }
        }

        private void CreateTestBoard()
        {
            if (_boardView == null || _boardView.Data == null)
            {
                Debug.LogError("[MatchTest] BoardView or BoardData is null!");
                return;
            }

            var data = _boardView.Data;

            // Clear board first
            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (!data.IsEmpty(pos))
                    {
                        _boardView.DestroyGem(pos);
                        data.RemoveGem(pos);
                    }
                }
            }

            // Horizontal match 3 in bottom row (y=0)
            SetGemAt(new Vector2Int(0, 0), GemType.Red);
            SetGemAt(new Vector2Int(1, 0), GemType.Red);
            SetGemAt(new Vector2Int(2, 0), GemType.Red);

            // Vertical match 4 on left column (x=0, starting from y=1)
            SetGemAt(new Vector2Int(0, 1), GemType.Blue);
            SetGemAt(new Vector2Int(0, 2), GemType.Blue);
            SetGemAt(new Vector2Int(0, 3), GemType.Blue);
            SetGemAt(new Vector2Int(0, 4), GemType.Blue);

            // L-shape in top-right corner (if board is large enough)
            if (data.Width >= 5 && data.Height >= 5)
            {
                // Horizontal part of L
                SetGemAt(new Vector2Int(data.Width - 3, data.Height - 1), GemType.Green);
                SetGemAt(new Vector2Int(data.Width - 2, data.Height - 1), GemType.Green);
                SetGemAt(new Vector2Int(data.Width - 1, data.Height - 1), GemType.Green);

                // Vertical part of L
                SetGemAt(new Vector2Int(data.Width - 1, data.Height - 2), GemType.Green);
                SetGemAt(new Vector2Int(data.Width - 1, data.Height - 3), GemType.Green);
            }

            // Fill remaining with random non-matching gems
            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (data.IsEmpty(pos))
                    {
                        SetGemAt(pos, GemType.Yellow);
                    }
                }
            }

            Debug.Log("[MatchTest] Test board created with predefined matches.");
        }

        private void SetGemAt(Vector2Int pos, GemType type)
        {
            var gem = new GemData(type, pos);
            _boardView.Data.SetGem(pos, gem);
        }

        private void HighlightMatches()
        {
            if (_boardView == null || _boardView.Data == null)
            {
                Debug.LogError("[MatchTest] BoardView or BoardData is null!");
                return;
            }

            var matches = _matchSystem.FindAllMatches(_boardView.Data);

            Debug.Log($"[MatchTest] Highlighting {matches.Count} matches:");

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                Debug.Log($"  Match {i} positions: {string.Join(", ", match.Positions)}");

                foreach (var pos in match.Positions)
                {
                    var view = _boardView.GetView(pos);
                    if (view != null)
                    {
                        var spriteRenderer = view.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.color = Color.white;
                        }
                    }
                }
            }
        }
    }
}
