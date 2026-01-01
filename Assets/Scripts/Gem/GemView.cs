using UnityEngine;

namespace Match3.Gem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private GemType _type;

        /// <summary>
        /// Текущий тип gem-а.
        /// </summary>
        public GemType Type => _type;

        /// <summary>
        /// Позиция на сетке (для удобства доступа из BoardView).
        /// </summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>
        /// Инициализирует gem с типом и конфигом.
        /// </summary>
        public void Setup(GemType type, GemConfig config)
        {
            _type = type;
            var sprite = config.GetSprite(type);
            if (sprite != null)
                _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = config.GetColor(type);
        }

        /// <summary>
        /// Устанавливает позицию в мировых координатах.
        /// </summary>
        public void SetWorldPosition(Vector3 pos)
        {
            transform.position = pos;
        }

        /// <summary>
        /// Устанавливает позицию на сетке (для отслеживания).
        /// </summary>
        public void SetGridPosition(Vector2Int pos)
        {
            GridPosition = pos;
        }

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
