using System.Collections.Generic;
using UnityEngine;

namespace Match3.Data
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Match3/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Size")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1f;

        [Header("Animation")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private float _fallSpeed = 10f;
        [SerializeField] private float _destroyDuration = 0.15f;

        [Header("Elements")]
        [SerializeField] private ElementType[] _elementTypes;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public float SwapDuration => _swapDuration;
        public float FallSpeed => _fallSpeed;
        public float DestroyDuration => _destroyDuration;
        public IReadOnlyList<ElementType> ElementTypes => _elementTypes;
    }
}
