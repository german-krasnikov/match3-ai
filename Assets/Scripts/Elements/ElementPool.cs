using System.Collections.Generic;
using UnityEngine;

namespace Match3.Elements
{
    public class ElementPool : MonoBehaviour
    {
        [SerializeField] private ElementView _prefab;
        [SerializeField] private int _initialSize = 64;

        private Queue<ElementView> _available = new Queue<ElementView>();

        private void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var element = CreateNew();
                element.gameObject.SetActive(false);
                _available.Enqueue(element);
            }
        }

        public ElementView Get()
        {
            var element = _available.Count > 0 ? _available.Dequeue() : CreateNew();
            element.gameObject.SetActive(true);
            return element;
        }

        public void Return(ElementView element)
        {
            element.gameObject.SetActive(false);
            element.transform.localScale = Vector3.one;
            _available.Enqueue(element);
        }

        private ElementView CreateNew()
        {
            return Instantiate(_prefab, transform);
        }
    }
}
