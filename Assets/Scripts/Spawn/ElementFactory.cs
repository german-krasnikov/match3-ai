using System;
using UnityEngine;
using Match3.Elements;

namespace Match3.Spawn
{
    public class ElementFactory : MonoBehaviour
    {
        public event Action<ElementComponent> OnElementCreated;
        public event Action<ElementComponent> OnElementReturned;

        [SerializeField] private ElementPool _pool;
        [SerializeField] private ElementDatabase _database;

        public ElementComponent Create(ElementType type, Vector3 worldPos, Vector2Int gridPos)
        {
            var data = _database.GetData(type);
            if (data == null)
            {
                Debug.LogError($"[ElementFactory] No data for type: {type}");
                return null;
            }
            return CreateInternal(data, worldPos, gridPos);
        }

        public ElementComponent CreateRandom(Vector3 worldPos, Vector2Int gridPos)
        {
            var data = _database.GetRandom();
            return CreateInternal(data, worldPos, gridPos);
        }

        public ElementComponent CreateRandomExcluding(Vector3 worldPos, Vector2Int gridPos, params ElementType[] excluded)
        {
            var data = GetRandomExcluding(excluded);
            return CreateInternal(data, worldPos, gridPos);
        }

        public void Return(ElementComponent element)
        {
            OnElementReturned?.Invoke(element);
            _pool.Release(element);
        }

        private ElementComponent CreateInternal(ElementData data, Vector3 worldPos, Vector2Int gridPos)
        {
            var element = _pool.Get();
            element.transform.position = worldPos;
            element.Initialize(data, gridPos);
            OnElementCreated?.Invoke(element);
            return element;
        }

        private ElementData GetRandomExcluding(ElementType[] excluded)
        {
            for (int i = 0; i < 10; i++)
            {
                var data = _database.GetRandom();
                if (!IsExcluded(data.Type, excluded))
                    return data;
            }
            return _database.GetRandom();
        }

        private bool IsExcluded(ElementType type, ElementType[] excluded)
        {
            foreach (var ex in excluded)
                if (ex == type) return true;
            return false;
        }
    }
}
