using UnityEngine;
using Match3.Core;

namespace Match3.Grid
{
    /// <summary>
    /// STUB for testing GridComponent before Element System (Step 3) is ready.
    /// Delete after integration.
    /// </summary>
    public class StubGridElement : IGridElement
    {
        public Vector2Int GridPosition { get; set; }
        public ElementType Type { get; }
        public GameObject GameObject { get; }

        public StubGridElement(ElementType type = ElementType.Red)
        {
            Type = type;
            GameObject = null;
        }
    }
}
