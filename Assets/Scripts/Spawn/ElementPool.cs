using System.Collections.Generic;
using UnityEngine;
using Match3.Elements;

namespace Match3.Spawn
{
    public class ElementPool : MonoBehaviour
    {
        [SerializeField] private ElementComponent _prefab;
        [SerializeField] private int _initialSize = 64;

        private Stack<ElementComponent> _pool;
        private Transform _poolContainer;

        public int PooledCount => _pool?.Count ?? 0;
        public int TotalCreated { get; private set; }

        private void Awake()
        {
            _pool = new Stack<ElementComponent>(_initialSize);
            _poolContainer = new GameObject("PooledElements").transform;
            _poolContainer.SetParent(transform);
            _poolContainer.gameObject.SetActive(false);

            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var element = CreateNew();
                Release(element);
            }
        }

        public ElementComponent Get()
        {
            var element = _pool.Count > 0 ? _pool.Pop() : CreateNew();
            element.gameObject.SetActive(true);
            return element;
        }

        public void Release(ElementComponent element)
        {
            element.ResetElement();
            element.transform.SetParent(_poolContainer);
            element.gameObject.SetActive(false);
            _pool.Push(element);
        }

        private ElementComponent CreateNew()
        {
            var element = Instantiate(_prefab, _poolContainer);
            TotalCreated++;
            return element;
        }
    }
}
