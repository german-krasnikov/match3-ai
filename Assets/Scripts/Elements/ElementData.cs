using UnityEngine;

namespace Match3.Elements
{
    [CreateAssetMenu(fileName = "ElementData", menuName = "Match3/Element Data")]
    public class ElementData : ScriptableObject
    {
        [SerializeField] private ElementType _type;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;

        public ElementType Type => _type;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
    }
}
