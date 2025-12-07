using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using Match3.Spawn;
using UnityEngine;

namespace Match3.Gravity
{
    public class GravityController : MonoBehaviour
    {
        public event Action<List<GridPosition>> OnGravityComplete;

        [SerializeField] private GridView _gridView;
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private GridConfig _config;
        private GridPositionConverter _converter;
        private GravityCalculator _calculator;

        private readonly List<GridPosition> _affectedPositions = new();
        private readonly HashSet<int> _affectedColumns = new();
        private int _pendingAnimations;

        private const float WaveDelay = 0.03f;
        private const float SpawnBaseDelay = 0.1f;

        public void Initialize(GridData grid)
        {
            _grid = grid;
            _config = _gridView.Config;
            _converter = _gridView.PositionConverter;
            _calculator = new GravityCalculator();
        }

        public void ApplyGravity(HashSet<GridPosition> destroyedPositions)
        {
            CollectAffectedColumns(destroyedPositions);

            var falls = _calculator.CalculateFalls(_grid, _affectedColumns);

            _affectedPositions.Clear();

            if (falls.Count == 0 && !HasEmptySpaces())
            {
                OnGravityComplete?.Invoke(_affectedPositions);
                return;
            }

            UpdateGridData(falls);
            var newElements = SpawnNewElements();

            int totalAnimations = falls.Count + newElements.Count;
            if (totalAnimations == 0)
            {
                OnGravityComplete?.Invoke(_affectedPositions);
                return;
            }

            _pendingAnimations = totalAnimations;
            AnimateFallsWave(falls);
            AnimateNewElementsWave(newElements);
        }

        private void CollectAffectedColumns(HashSet<GridPosition> positions)
        {
            _affectedColumns.Clear();
            foreach (var pos in positions)
                _affectedColumns.Add(pos.X);
        }

        private bool HasEmptySpaces()
        {
            foreach (int column in _affectedColumns)
            {
                if (_calculator.CountEmptyInColumn(_grid, column) > 0)
                    return true;
            }
            return false;
        }

        private void UpdateGridData(List<FallData> falls)
        {
            foreach (var fall in falls)
                _grid.RemoveElement(fall.From);

            foreach (var fall in falls)
            {
                fall.Element.Position = fall.To;
                _grid.SetElement(fall.To, fall.Element);
                _affectedPositions.Add(fall.To);
            }
        }

        private List<(IElement element, GridPosition target, int spawnOffset)> SpawnNewElements()
        {
            var newElements = new List<(IElement, GridPosition, int)>();

            foreach (int column in _affectedColumns)
            {
                int emptyCount = _calculator.CountEmptyInColumn(_grid, column);

                for (int i = 0; i < emptyCount; i++)
                {
                    int targetY = _config.Height - emptyCount + i;
                    var targetPos = new GridPosition(column, targetY);

                    int spawnOffset = emptyCount - i;

                    var element = _spawnController.SpawnAtTop(column, spawnOffset);
                    element.Position = targetPos;
                    _grid.SetElement(targetPos, element);

                    newElements.Add((element, targetPos, spawnOffset));
                    _affectedPositions.Add(targetPos);
                }
            }

            return newElements;
        }

        private void AnimateFallsWave(List<FallData> falls)
        {
            var sorted = falls.OrderByDescending(f => f.From.Y).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var fall = sorted[i];
                float delay = i * WaveDelay;
                float duration = fall.Distance / _config.FallSpeed;
                var targetWorld = _converter.GridToWorld(fall.To);

                StartCoroutine(AnimateWithDelay(fall.Element, targetWorld, duration, delay));
            }
        }

        private void AnimateNewElementsWave(List<(IElement element, GridPosition target, int spawnOffset)> newElements)
        {
            var sorted = newElements.OrderByDescending(e => e.spawnOffset).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var (element, target, spawnOffset) = sorted[i];
                float delay = SpawnBaseDelay + i * WaveDelay;
                float duration = spawnOffset / _config.FallSpeed;
                var targetWorld = _converter.GridToWorld(target);

                StartCoroutine(AnimateWithDelay(element, targetWorld, duration, delay));
            }
        }

        private IEnumerator AnimateWithDelay(IElement element, Vector3 target, float duration, float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            element.MoveTo(target, duration, OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            _pendingAnimations--;
            if (_pendingAnimations <= 0)
                OnGravityComplete?.Invoke(_affectedPositions);
        }
    }
}
