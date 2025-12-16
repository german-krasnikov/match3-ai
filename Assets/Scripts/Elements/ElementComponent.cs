using System;
using UnityEngine;

public class ElementComponent : MonoBehaviour
{
    public event Action<ElementComponent> OnDestroyed;

    [SerializeField] private SpriteRenderer _spriteRenderer;

    private ElementType _type;
    private int _x;
    private int _y;

    public ElementType Type => _type;
    public int X => _x;
    public int Y => _y;
    public Vector2Int GridPosition => new Vector2Int(_x, _y);

    public void Initialize(ElementType type, Color color, int x, int y)
    {
        _type = type;
        _x = x;
        _y = y;
        _spriteRenderer.color = color;
    }

    public void SetGridPosition(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        _x = pos.x;
        _y = pos.y;
    }

    public void DestroyElement()
    {
        OnDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
}
