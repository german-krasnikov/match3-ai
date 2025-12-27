using UnityEngine;

namespace Match3.Pieces
{
    /// <summary>
    /// Отвечает ТОЛЬКО за визуальное представление фишки (SRP).
    /// </summary>
    public class PieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public void Setup(Sprite sprite, Color color)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = color;
        }

        public void SetAlpha(float alpha)
        {
            var c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }

        public SpriteRenderer Renderer => _spriteRenderer;
    }
}
