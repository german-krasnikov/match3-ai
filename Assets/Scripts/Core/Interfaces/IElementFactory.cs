using UnityEngine;

namespace Match3.Core
{
    public interface IElementFactory
    {
        IGridElement Create(ElementType type, Vector3 worldPosition);
        void Destroy(IGridElement element);
    }
}
