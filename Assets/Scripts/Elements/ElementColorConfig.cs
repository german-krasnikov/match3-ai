using UnityEngine;
using Match3.Core;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementColors", menuName = "Match3/Element Color Config")]
    public class ElementColorConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ElementColor
        {
            public ElementType type;
            public Color color;
        }

        [SerializeField] private ElementColor[] _colors;

        public Color GetColor(ElementType type)
        {
            foreach (var ec in _colors)
            {
                if (ec.type == type)
                    return ec.color;
            }
            return Color.white;
        }
    }
}
