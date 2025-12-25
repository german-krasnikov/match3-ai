using System.Collections.Generic;
using UnityEngine;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementDatabase", menuName = "Match3/Element Database")]
    public class ElementDatabase : ScriptableObject
    {
        [SerializeField] private List<ElementData> _elements = new();

        private Dictionary<ElementType, ElementData> _lookup;

        public IReadOnlyList<ElementData> Elements => _elements;
        public int Count => _elements.Count;

        public ElementData GetData(ElementType type)
        {
            BuildLookupIfNeeded();
            return _lookup.TryGetValue(type, out var data) ? data : null;
        }

        public ElementData GetRandom()
        {
            if (_elements.Count == 0) return null;
            return _elements[Random.Range(0, _elements.Count)];
        }

        public ElementType GetRandomType()
        {
            return GetRandom()?.Type ?? ElementType.None;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<ElementType, ElementData>();
            foreach (var element in _elements)
            {
                if (element != null && !_lookup.ContainsKey(element.Type))
                    _lookup[element.Type] = element;
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
