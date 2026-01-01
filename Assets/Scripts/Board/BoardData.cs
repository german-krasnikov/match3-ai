using System;
using UnityEngine;
using Match3.Gem;

namespace Match3.Board
{
    public class BoardData
    {
        private readonly GemData?[,] _gems;
        private readonly int _width;
        private readonly int _height;

        public int Width => _width;
        public int Height => _height;
        public Vector2Int Size => new Vector2Int(_width, _height);

        /// <summary>
        /// Fires when gem is added to position.
        /// </summary>
        public event Action<Vector2Int, GemData> OnGemAdded;

        /// <summary>
        /// Fires when gem is removed from position.
        /// </summary>
        public event Action<Vector2Int> OnGemRemoved;

        public BoardData(int width, int height)
        {
            _width = width;
            _height = height;
            _gems = new GemData?[width, height];
        }

        public BoardData(Vector2Int size) : this(size.x, size.y) { }

        /// <summary>
        /// Returns gem at position or null if empty/invalid.
        /// </summary>
        public GemData? GetGem(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return null;
            return _gems[pos.x, pos.y];
        }

        /// <summary>
        /// Sets gem at position. Overwrites existing.
        /// </summary>
        public void SetGem(Vector2Int pos, GemData gem)
        {
            if (!IsValidPosition(pos))
                return;
            _gems[pos.x, pos.y] = gem;
            OnGemAdded?.Invoke(pos, gem);
        }

        /// <summary>
        /// Removes gem at position.
        /// </summary>
        public void RemoveGem(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return;
            if (_gems[pos.x, pos.y] == null)
                return;
            _gems[pos.x, pos.y] = null;
            OnGemRemoved?.Invoke(pos);
        }

        /// <summary>
        /// Returns true if position has no gem.
        /// </summary>
        public bool IsEmpty(Vector2Int pos)
        {
            if (!IsValidPosition(pos))
                return false;
            return _gems[pos.x, pos.y] == null;
        }

        /// <summary>
        /// Returns true if position is within board bounds.
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width &&
                   pos.y >= 0 && pos.y < _height;
        }

        /// <summary>
        /// Moves gem from one position to another.
        /// Does NOT fire events (use for swaps, falls).
        /// </summary>
        public void MoveGem(Vector2Int from, Vector2Int to)
        {
            if (!IsValidPosition(from) || !IsValidPosition(to))
                return;
            var gem = _gems[from.x, from.y];
            if (gem == null)
                return;
            _gems[from.x, from.y] = null;
            _gems[to.x, to.y] = gem.Value.WithPosition(to);
        }

        /// <summary>
        /// Swaps gems at two positions.
        /// Does NOT fire events.
        /// </summary>
        public void SwapGems(Vector2Int a, Vector2Int b)
        {
            if (!IsValidPosition(a) || !IsValidPosition(b))
                return;
            var gemA = _gems[a.x, a.y];
            var gemB = _gems[b.x, b.y];

            _gems[a.x, a.y] = gemB?.WithPosition(a);
            _gems[b.x, b.y] = gemA?.WithPosition(b);
        }

        /// <summary>
        /// Returns gem type at position or null.
        /// </summary>
        public GemType? GetGemType(Vector2Int pos)
        {
            var gem = GetGem(pos);
            return gem?.Type;
        }
    }
}
