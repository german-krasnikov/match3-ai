using UnityEngine;

namespace Match3.Data
{
    [CreateAssetMenu(fileName = "TileData", menuName = "Match3/TileData")]
    public class TileData : ScriptableObject
    {
        public TileType type;
        public Sprite sprite;
        public Color color = Color.white;
    }
}
