using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Elements;
using Match3.Grid;
using UnityEngine;

namespace Match3.Destruction
{
    public class DestructionController : MonoBehaviour
    {
        public event Action<HashSet<GridPosition>> OnDestructionComplete;

        [SerializeField] private ElementFactory _factory;

        private GridData _grid;
        private readonly HashSet<GridPosition> _positionsToDestroy = new();
        private readonly List<IElement> _elementsToDestroy = new();
        private int _pendingAnimations;

        public void Initialize(GridData grid)
        {
            _grid = grid;
        }

        public void DestroyMatches(List<MatchData> matches)
        {
            CollectUniquePositions(matches);

            if (_positionsToDestroy.Count == 0)
            {
                OnDestructionComplete?.Invoke(_positionsToDestroy);
                return;
            }

            CollectElements();
            RemoveFromGrid();
            PlayDestroyAnimations();
        }

        private void CollectUniquePositions(List<MatchData> matches)
        {
            _positionsToDestroy.Clear();
            foreach (var match in matches)
            {
                foreach (var pos in match.Positions)
                    _positionsToDestroy.Add(pos);
            }
        }

        private void CollectElements()
        {
            _elementsToDestroy.Clear();
            foreach (var pos in _positionsToDestroy)
            {
                var element = _grid.GetElement(pos);
                if (element != null)
                    _elementsToDestroy.Add(element);
            }
        }

        private void RemoveFromGrid()
        {
            foreach (var pos in _positionsToDestroy)
                _grid.RemoveElement(pos);
        }

        private void PlayDestroyAnimations()
        {
            _pendingAnimations = _elementsToDestroy.Count;

            foreach (var element in _elementsToDestroy)
            {
                element.PlayDestroyAnimation(() =>
                {
                    _factory.ReturnElement(element);
                    _pendingAnimations--;

                    if (_pendingAnimations <= 0)
                        OnDestructionComplete?.Invoke(_positionsToDestroy);
                });
            }
        }
    }
}
