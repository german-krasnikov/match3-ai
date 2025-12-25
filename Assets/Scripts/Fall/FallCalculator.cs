using System.Collections.Generic;
using UnityEngine;
using Match3.Board;

namespace Match3.Fall
{
    public static class FallCalculator
    {
        private static readonly List<FallData> _fallsBuffer = new(64);

        public static List<FallData> CalculateFalls(BoardComponent board)
        {
            _fallsBuffer.Clear();

            for (int x = 0; x < board.Width; x++)
            {
                CalculateColumnFalls(board, x);
            }

            return new List<FallData>(_fallsBuffer);
        }

        private static void CalculateColumnFalls(BoardComponent board, int column)
        {
            int writeIndex = 0;

            for (int y = 0; y < board.Height; y++)
            {
                var pos = new Vector2Int(column, y);
                var element = board.GetElement(pos);

                if (element != null)
                {
                    if (y != writeIndex)
                    {
                        var from = pos;
                        var to = new Vector2Int(column, writeIndex);
                        _fallsBuffer.Add(new FallData(element, from, to));
                    }
                    writeIndex++;
                }
            }
        }
    }
}
