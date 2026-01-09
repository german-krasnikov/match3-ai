// Assets/Scripts/Configs/GameConfig.cs
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        public int GridWidth = 8;
        public int GridHeight = 8;

        [Header("Elements")]
        public int ElementTypeCount = 5;
        public Sprite[] ElementSprites;

        [Header("Matching")]
        public int MinMatchLength = 3;

        [Header("Animations")]
        public float SwapDuration = 0.3f;
        public float FallDuration = 0.2f;
        public float DestroyDuration = 0.2f;
        public float SpawnDelay = 0.1f;
    }
}
