// Assets/Scripts/Features/Board/Models/BoardModel.cs
using System;
using System.Collections.Generic;
using Common;

namespace Features.Board.Models
{
    public class BoardModel
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Cell[,] _cells;

        public event Action<GridPosition, ElementType> OnElementAdded;
        public event Action<GridPosition> OnElementRemoved;
        public event Action<GridPosition, GridPosition> OnElementsSwapped;
        public event Action<GridPosition, GridPosition> OnElementMoved;

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];
            InitializeCells();
        }

        private void InitializeCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _cells[x, y] = new Cell(new GridPosition(x, y));
                }
            }
        }

        public bool IsValidPosition(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public Cell GetCell(GridPosition pos)
        {
            return IsValidPosition(pos) ? _cells[pos.X, pos.Y] : null;
        }

        public Element GetElement(GridPosition pos)
        {
            return GetCell(pos)?.Element;
        }

        public void SetElement(GridPosition pos, Element element)
        {
            var cell = GetCell(pos);
            if (cell == null) return;

            cell.SetElement(element);
            OnElementAdded?.Invoke(pos, element.Type);
        }

        public void RemoveElement(GridPosition pos)
        {
            var cell = GetCell(pos);
            if (cell == null || cell.IsEmpty) return;

            cell.RemoveElement();
            OnElementRemoved?.Invoke(pos);
        }

        /// <summary>
        /// Remove multiple elements at once.
        /// Fires OnElementRemoved for each valid position.
        /// </summary>
        public void RemoveElements(List<GridPosition> positions)
        {
            if (positions == null) return;

            foreach (var pos in positions)
            {
                RemoveElement(pos);
            }
        }

        public void SwapElements(GridPosition a, GridPosition b)
        {
            var cellA = GetCell(a);
            var cellB = GetCell(b);
            if (cellA == null || cellB == null) return;

            var elementA = cellA.Element;
            var elementB = cellB.Element;

            cellA.SetElement(elementB);
            cellB.SetElement(elementA);

            OnElementsSwapped?.Invoke(a, b);
        }

        /// <summary>
        /// Move element from one position to another (for falling).
        /// Source becomes empty, target receives the element.
        /// Does nothing if source is empty or target is occupied.
        /// </summary>
        public void MoveElement(GridPosition from, GridPosition to)
        {
            var cellFrom = GetCell(from);
            var cellTo = GetCell(to);

            if (cellFrom == null || cellTo == null) return;
            if (cellFrom.IsEmpty) return;
            if (!cellTo.IsEmpty) return;

            var element = cellFrom.RemoveElement();
            cellTo.SetElement(element);

            OnElementMoved?.Invoke(from, to);
        }

        /// <summary>
        /// Apply multiple moves at once (for batch falling).
        /// Moves are applied in order - be careful about dependencies.
        /// </summary>
        public void ApplyMoves(List<(GridPosition from, GridPosition to)> moves)
        {
            if (moves == null) return;

            foreach (var (from, to) in moves)
            {
                MoveElement(from, to);
            }
        }
    }
}
