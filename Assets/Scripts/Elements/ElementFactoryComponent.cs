using UnityEngine;
using Match3.Core;

namespace Match3.Elements
{
    public class ElementFactoryComponent : MonoBehaviour, IElementFactory
    {
        [Header("Dependencies")]
        [SerializeField] private ElementComponent _elementPrefab;
        [SerializeField] private ElementColorConfig _colorConfig;

        [Header("Settings")]
        [SerializeField] private Transform _elementsParent;

        public IGridElement Create(ElementType type, Vector3 worldPosition)
        {
            var element = Instantiate(_elementPrefab, worldPosition, Quaternion.identity, _elementsParent);
            element.Initialize(type, _colorConfig);
            return element;
        }

        public void Destroy(IGridElement element)
        {
            if (element?.GameObject == null)
                return;

            Object.Destroy(element.GameObject);
        }
    }
}
