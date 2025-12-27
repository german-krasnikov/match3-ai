using UnityEngine;
using Match3.Core;

namespace Match3.Pieces
{
    [CreateAssetMenu(fileName = "PieceConfig", menuName = "Match3/Piece Config")]
    public class PieceConfig : ScriptableObject
    {
        [System.Serializable]
        public class PieceVisualData
        {
            public PieceType Type;
            public Sprite Sprite;
            public Color Color = Color.white;
        }

        [SerializeField] private PieceVisualData[] _pieces;

        public Sprite GetSprite(PieceType type)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i].Type == type)
                    return _pieces[i].Sprite;
            }
            return null;
        }

        public Color GetColor(PieceType type)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i].Type == type)
                    return _pieces[i].Color;
            }
            return Color.white;
        }
    }
}
