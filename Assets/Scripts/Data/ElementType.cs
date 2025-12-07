using UnityEngine;

namespace Match3.Data
{
    [CreateAssetMenu(fileName = "ElementType", menuName = "Match3/Element Type")]
    public class ElementType : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private GameObject _destroyVfxPrefab;
        [SerializeField] private AudioClip _destroySound;

        public string Id => _id;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
        public GameObject DestroyVfxPrefab => _destroyVfxPrefab;
        public AudioClip DestroySound => _destroySound;
    }
}
