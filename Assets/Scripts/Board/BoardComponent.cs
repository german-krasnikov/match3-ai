using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Grid;
using Match3.Elements;

namespace Match3.Board
{
    public class BoardComponent : MonoBehaviour
    {
        public event Action<Vector2Int, ElementComponent> OnElementSet;
        public event Action<Vector2Int> OnElementRemoved;

        [SerializeField] private GridComponent _grid;

        private ElementComponent[,] _elements;

        public int Width => _grid.Width;
        public int Height => _grid.Height;

        public void Initialize()
        {
            _elements = new ElementComponent[_grid.Width, _grid.Height];
        }

        public void Initialize(ElementComponent[,] elements)
        {
            _elements = elements;
        }

        public ElementComponent GetElement(Vector2Int pos)
        {
            if (!_grid.IsValidPosition(pos)) return null;
            return _elements[pos.x, pos.y];
        }

        public void SetElement(Vector2Int pos, ElementComponent element)
        {
            if (!_grid.IsValidPosition(pos)) return;

            _elements[pos.x, pos.y] = element;
            if (element != null)
                element.GridPosition = pos;

            OnElementSet?.Invoke(pos, element);
        }

        public ElementComponent RemoveElement(Vector2Int pos)
        {
            if (!_grid.IsValidPosition(pos)) return null;

            var element = _elements[pos.x, pos.y];
            _elements[pos.x, pos.y] = null;
            OnElementRemoved?.Invoke(pos);
            return element;
        }

        public bool IsEmpty(Vector2Int pos)
        {
            if (!_grid.IsValidPosition(pos)) return false;
            return _elements[pos.x, pos.y] == null;
        }

        public List<Vector2Int> GetEmptyPositions()
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    if (_elements[x, y] == null)
                        result.Add(new Vector2Int(x, y));
                }
            }
            return result;
        }

        public List<int> GetEmptyRowsInColumn(int column)
        {
            var result = new List<int>();
            for (int y = 0; y < _grid.Height; y++)
            {
                if (_elements[column, y] == null)
                    result.Add(y);
            }
            return result;
        }

        public ElementType? GetElementType(Vector2Int pos)
        {
            var element = GetElement(pos);
            return element?.Type;
        }

        public void SwapElements(Vector2Int posA, Vector2Int posB)
        {
            var elementA = _elements[posA.x, posA.y];
            var elementB = _elements[posB.x, posB.y];

            _elements[posA.x, posA.y] = elementB;
            _elements[posB.x, posB.y] = elementA;

            if (elementA != null) elementA.GridPosition = posB;
            if (elementB != null) elementB.GridPosition = posA;
        }

        public void Clear()
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    _elements[x, y] = null;
                }
            }
        }
    }
}
