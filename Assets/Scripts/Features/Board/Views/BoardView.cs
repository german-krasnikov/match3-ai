// Assets/Scripts/Features/Board/Views/BoardView.cs
using System;
using System.Collections.Generic;
using Common;
using Configs;
using Features.Board.Services;
using UnityEngine;

namespace Features.Board.Views
{
    public class BoardView : MonoBehaviour, IBoardView
    {
        [SerializeField] private GameConfig _config;

        private int _width;
        private int _height;
        private float _cellSize;
        private Vector3 _originOffset;

        private readonly Dictionary<GridPosition, ElementView> _elementViews = new();

        private GridPosition _dragStartPos = GridPosition.Invalid;
        private bool _isDragging;

        public event Action<GridPosition> OnCellClicked;
        public event Action<GridPosition, GridPosition> OnSwapAttempted;

        public void Initialize(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;

            _originOffset = new Vector3(
                -(_width - 1) * _cellSize * 0.5f,
                -(_height - 1) * _cellSize * 0.5f,
                0f
            );
        }

        public void CreateElement(GridPosition pos, ElementType type)
        {
            if (_elementViews.ContainsKey(pos))
            {
                RemoveElement(pos);
            }

            var sprite = GetSpriteForType(type);
            if (sprite == null) return;

            var elementGO = new GameObject($"Element_{pos.X}_{pos.Y}");
            elementGO.transform.SetParent(transform);

            var elementView = elementGO.AddComponent<ElementView>();
            elementView.Initialize(pos, type, sprite);
            elementView.UpdatePosition(GridToWorld(pos));

            elementView.OnDragStart += HandleElementDragStart;
            elementView.OnDragEnd += HandleElementDragEnd;
            elementView.OnClicked += HandleElementClicked;

            _elementViews[pos] = elementView;
        }

        public void RemoveElement(GridPosition pos)
        {
            if (_elementViews.TryGetValue(pos, out var view))
            {
                view.OnDragStart -= HandleElementDragStart;
                view.OnDragEnd -= HandleElementDragEnd;
                view.OnClicked -= HandleElementClicked;

                view.Destroy();
                _elementViews.Remove(pos);
            }
        }

        public void Clear()
        {
            foreach (var view in _elementViews.Values)
            {
                view.OnDragStart -= HandleElementDragStart;
                view.OnDragEnd -= HandleElementDragEnd;
                view.OnClicked -= HandleElementClicked;
                view.Destroy();
            }
            _elementViews.Clear();
        }

        public void SwapElements(GridPosition from, GridPosition to, Action onComplete)
        {
            if (!_elementViews.TryGetValue(from, out var viewFrom) ||
                !_elementViews.TryGetValue(to, out var viewTo))
            {
                onComplete?.Invoke();
                return;
            }

            float duration = _config != null ? _config.SwapDuration : 0.3f;
            int completedCount = 0;

            void OnMoveComplete()
            {
                completedCount++;
                if (completedCount >= 2)
                {
                    // Update dictionary keys after swap
                    _elementViews.Remove(from);
                    _elementViews.Remove(to);

                    viewFrom.SetGridPosition(to);
                    viewTo.SetGridPosition(from);

                    _elementViews[to] = viewFrom;
                    _elementViews[from] = viewTo;

                    onComplete?.Invoke();
                }
            }

            Vector3 targetFrom = GridToWorld(to);
            Vector3 targetTo = GridToWorld(from);

            viewFrom.MoveTo(targetFrom, duration, OnMoveComplete);
            viewTo.MoveTo(targetTo, duration, OnMoveComplete);
        }

        public void DestroyElements(List<GridPosition> positions, Action onComplete)
        {
            if (positions == null || positions.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            float duration = _config != null ? _config.DestroyDuration : 0.2f;
            int totalToDestroy = 0;
            int destroyedCount = 0;

            foreach (var pos in positions)
                if (_elementViews.ContainsKey(pos)) totalToDestroy++;

            if (totalToDestroy == 0)
            {
                onComplete?.Invoke();
                return;
            }

            foreach (var pos in positions)
            {
                if (_elementViews.TryGetValue(pos, out var view))
                {
                    var capturedPos = pos;
                    view.PlayDestroyAnimation(duration, () =>
                    {
                        _elementViews.Remove(capturedPos);
                        destroyedCount++;
                        if (destroyedCount >= totalToDestroy)
                            onComplete?.Invoke();
                    });
                }
            }
        }

        public void MoveElement(GridPosition from, GridPosition to, Action onComplete)
        {
            if (!_elementViews.TryGetValue(from, out var view))
            {
                onComplete?.Invoke();
                return;
            }

            float duration = _config != null ? _config.FallDuration : 0.2f;
            Vector3 targetPosition = GridToWorld(to);

            view.MoveTo(targetPosition, duration, () =>
            {
                _elementViews.Remove(from);
                view.SetGridPosition(to);
                _elementViews[to] = view;

                onComplete?.Invoke();
            });
        }

        public void MoveElements(List<FallMove> moves, Action onComplete)
        {
            if (moves == null || moves.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            float duration = _config != null ? _config.FallDuration : 0.2f;
            int totalMoves = 0;
            int completedMoves = 0;

            foreach (var move in moves)
            {
                if (_elementViews.ContainsKey(move.From))
                {
                    totalMoves++;
                }
            }

            if (totalMoves == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var viewsToMove = new List<(ElementView view, FallMove move)>();
            foreach (var move in moves)
            {
                if (_elementViews.TryGetValue(move.From, out var view))
                {
                    viewsToMove.Add((view, move));
                }
            }

            foreach (var (view, move) in viewsToMove)
            {
                _elementViews.Remove(move.From);
            }

            void OnMoveComplete(ElementView view, FallMove move)
            {
                view.SetGridPosition(move.To);
                _elementViews[move.To] = view;

                completedMoves++;
                if (completedMoves >= totalMoves)
                {
                    onComplete?.Invoke();
                }
            }

            foreach (var (view, move) in viewsToMove)
            {
                Vector3 targetPosition = GridToWorld(move.To);
                var capturedView = view;
                var capturedMove = move;
                view.MoveTo(targetPosition, duration, () => OnMoveComplete(capturedView, capturedMove));
            }
        }

        public void SpawnElement(GridPosition pos, ElementType type, int fallDistance, Action onComplete)
        {
            var sprite = GetSpriteForType(type);
            if (sprite == null)
            {
                onComplete?.Invoke();
                return;
            }

            var elementGO = new GameObject($"Element_{pos.X}_{pos.Y}");
            elementGO.transform.SetParent(transform);

            var elementView = elementGO.AddComponent<ElementView>();
            elementView.Initialize(pos, type, sprite);

            Vector3 targetPos = GridToWorld(pos);
            Vector3 startPos = targetPos + new Vector3(0, fallDistance * _cellSize, 0);
            elementView.UpdatePosition(startPos);

            elementView.OnDragStart += HandleElementDragStart;
            elementView.OnDragEnd += HandleElementDragEnd;
            elementView.OnClicked += HandleElementClicked;

            _elementViews[pos] = elementView;

            float duration = _config != null ? _config.FallDuration : 0.2f;
            float totalDuration = duration * Mathf.Max(1f, fallDistance * 0.5f);

            elementView.MoveTo(targetPos, totalDuration, onComplete);
        }

        public void SpawnElements(List<SpawnData> spawns, Action onComplete)
        {
            if (spawns == null || spawns.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int totalSpawns = spawns.Count;
            int completedSpawns = 0;

            foreach (var spawn in spawns)
            {
                SpawnElement(spawn.Position, spawn.Type, spawn.FallDistance, () =>
                {
                    completedSpawns++;
                    if (completedSpawns >= totalSpawns)
                        onComplete?.Invoke();
                });
            }
        }

        private void HandleElementDragStart(GridPosition pos)
        {
            _dragStartPos = pos;
            _isDragging = true;
        }

        private void HandleElementDragEnd(GridPosition endPos)
        {
            if (!_isDragging) return;

            _isDragging = false;

            if (_dragStartPos.IsValid && endPos.IsValid && !_dragStartPos.Equals(endPos))
            {
                OnSwapAttempted?.Invoke(_dragStartPos, endPos);
            }

            _dragStartPos = GridPosition.Invalid;
        }

        private void HandleElementClicked(GridPosition pos)
        {
            OnCellClicked?.Invoke(pos);
        }

        private Vector3 GridToWorld(GridPosition pos)
        {
            return transform.position + _originOffset + new Vector3(
                pos.X * _cellSize,
                pos.Y * _cellSize,
                0f
            );
        }

        public GridPosition WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - transform.position - _originOffset;

            int x = Mathf.RoundToInt(localPos.x / _cellSize);
            int y = Mathf.RoundToInt(localPos.y / _cellSize);

            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return GridPosition.Invalid;

            return new GridPosition(x, y);
        }

        private Sprite GetSpriteForType(ElementType type)
        {
            if (_config == null || _config.ElementSprites == null)
                return null;

            int index = (int)type - 1;
            if (index >= 0 && index < _config.ElementSprites.Length)
                return _config.ElementSprites[index];

            return null;
        }

        public void SetConfig(GameConfig config)
        {
            _config = config;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
