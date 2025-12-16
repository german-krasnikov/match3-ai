using UnityEngine;

public class ElementFactory : MonoBehaviour
{
    [SerializeField] private ElementConfig _config;
    [SerializeField] private Transform _container;

    public ElementComponent Create(ElementType type, Vector3 worldPosition, int gridX, int gridY)
    {
        var element = Instantiate(_config.Prefab, worldPosition, Quaternion.identity, _container);
        element.Initialize(type, _config.GetColor(type), gridX, gridY);
        return element;
    }

    public ElementComponent CreateRandom(Vector3 worldPosition, int gridX, int gridY)
    {
        var type = GetRandomType();
        return Create(type, worldPosition, gridX, gridY);
    }

    public ElementComponent CreateRandomExcluding(Vector3 worldPosition, int gridX, int gridY, params ElementType[] excluded)
    {
        var type = GetRandomTypeExcluding(excluded);
        return Create(type, worldPosition, gridX, gridY);
    }

    public void Destroy(ElementComponent element)
    {
        if (element != null)
            element.DestroyElement();
    }

    private ElementType GetRandomType()
    {
        int index = Random.Range(0, _config.TypeCount);
        return (ElementType)index;
    }

    private ElementType GetRandomTypeExcluding(ElementType[] excluded)
    {
        ElementType type;
        int attempts = 0;
        do
        {
            type = GetRandomType();
            attempts++;
        } while (System.Array.IndexOf(excluded, type) >= 0 && attempts < 100);

        return type;
    }
}
