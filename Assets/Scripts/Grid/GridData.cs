using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Elements;

namespace Match3.Grid
{
    public class GridData
    {
        public event Action<GridPosition, IElement> OnElementSet;
        public event Action<GridPosition> OnElementRemoved;

        private readonly IElement[,] _elements;
        private readonly int _width;
        private readonly int _height;

        public int Width => _width;
        public int Height => _height;

        public GridData(int width, int height)
        {
            _width = width;
            _height = height;
            _elements = new IElement[width, height];
        }

        public bool IsValidPosition(GridPosition pos)
            => pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height;

        public IElement GetElement(GridPosition pos)
            => IsValidPosition(pos) ? _elements[pos.X, pos.Y] : null;

        public void SetElement(GridPosition pos, IElement element)
        {
            if (!IsValidPosition(pos)) return;
            _elements[pos.X, pos.Y] = element;
            OnElementSet?.Invoke(pos, element);
        }

        public void RemoveElement(GridPosition pos)
        {
            if (!IsValidPosition(pos)) return;
            _elements[pos.X, pos.Y] = null;
            OnElementRemoved?.Invoke(pos);
        }

        public void SwapElements(GridPosition a, GridPosition b)
        {
            var tempA = GetElement(a);
            var tempB = GetElement(b);
            _elements[a.X, a.Y] = tempB;
            _elements[b.X, b.Y] = tempA;
        }

        public List<GridPosition> GetEmptyPositions()
        {
            var empty = new List<GridPosition>();
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    if (_elements[x, y] == null)
                        empty.Add(new GridPosition(x, y));
            return empty;
        }
    }
}
