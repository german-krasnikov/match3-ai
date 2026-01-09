// Assets/Scripts/Features/Board/Services/SpawnService.cs
using System;
using System.Collections.Generic;
using Common;

namespace Features.Board.Services
{
    public class SpawnService : ISpawnService
    {
        private readonly int _elementTypeCount;
        private readonly Random _random;

        public SpawnService(int elementTypeCount, int? seed = null)
        {
            _elementTypeCount = elementTypeCount;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public ElementType GetRandomElement()
        {
            int typeIndex = _random.Next(1, _elementTypeCount + 1);
            return (ElementType)typeIndex;
        }

        public ElementType GetRandomElementExcluding(ElementType[] excluded)
        {
            var available = GetAvailableTypes(excluded);
            if (available.Count == 0)
                return ElementType.None;

            int index = _random.Next(available.Count);
            return available[index];
        }

        private List<ElementType> GetAvailableTypes(ElementType[] excluded)
        {
            var result = new List<ElementType>();
            var excludedSet = new HashSet<ElementType>(excluded);

            for (int i = 1; i <= _elementTypeCount; i++)
            {
                var type = (ElementType)i;
                if (!excludedSet.Contains(type))
                    result.Add(type);
            }

            return result;
        }
    }
}
