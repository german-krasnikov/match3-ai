using System.Collections.Generic;
using UnityEngine;

namespace Match3.Gem
{
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Match3/GemConfig")]
    public class GemConfig : ScriptableObject
    {
        [SerializeField] private List<GemTypeData> _gems;

        /// <summary>
        /// Количество доступных типов gem-ов.
        /// </summary>
        public int TypeCount => _gems.Count;

        /// <summary>
        /// Возвращает спрайт для указанного типа.
        /// </summary>
        public Sprite GetSprite(GemType type)
        {
            foreach (var gem in _gems)
            {
                if (gem.Type == type)
                    return gem.Sprite;
            }
            Debug.LogWarning($"GemConfig: Sprite not found for {type}");
            return null;
        }

        /// <summary>
        /// Возвращает случайный тип из доступных.
        /// </summary>
        public GemType GetRandomType()
        {
            int index = Random.Range(0, _gems.Count);
            return _gems[index].Type;
        }

        /// <summary>
        /// Возвращает все доступные типы.
        /// </summary>
        public IReadOnlyList<GemType> GetAllTypes()
        {
            var types = new List<GemType>(_gems.Count);
            foreach (var gem in _gems)
            {
                types.Add(gem.Type);
            }
            return types;
        }
    }
}
