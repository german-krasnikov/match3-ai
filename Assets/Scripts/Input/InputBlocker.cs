using UnityEngine;

namespace Match3.Input
{
    /// <summary>
    /// Manages input blocking during animations.
    /// Uses stack-based counting for multiple simultaneous blocks.
    /// </summary>
    public class InputBlocker : MonoBehaviour
    {
        private int _blockCount;

        public bool IsBlocked => _blockCount > 0;

        public void Block() => _blockCount++;

        public void Unblock() => _blockCount = Mathf.Max(0, _blockCount - 1);

        public void Reset() => _blockCount = 0;
    }
}
