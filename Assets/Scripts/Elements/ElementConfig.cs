using UnityEngine;

[CreateAssetMenu(fileName = "ElementConfig", menuName = "Match3/ElementConfig")]
public class ElementConfig : ScriptableObject
{
    [Header("Visuals")]
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private Color[] _colors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.6f, 0.2f, 0.8f)
    };

    [Header("Prefab")]
    [SerializeField] private ElementComponent _prefab;

    public ElementComponent Prefab => _prefab;
    public Sprite DefaultSprite => _defaultSprite;
    public int TypeCount => _colors.Length;

    public Color GetColor(ElementType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _colors.Length)
            return Color.white;
        return _colors[index];
    }
}
