using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Elements
{
    public class ElementFactory : MonoBehaviour
    {
        [SerializeField] private ElementPool _pool;
        [SerializeField] private Transform _elementsParent;

        public IElement CreateElement(ElementType type, GridPosition position, Vector3 worldPos)
        {
            var element = _pool.Get();
            element.transform.SetParent(_elementsParent);
            element.transform.position = worldPos;
            element.Initialize(type, position);
            return element;
        }

        public void ReturnElement(IElement element)
        {
            if (element is ElementView view)
            {
                _pool.Return(view);
            }
        }
    }
}
