using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Match3.Core;
using Match3.Grid;
using Match3.Elements;

namespace Match3.Destruction
{
    public class DestructionComponent : MonoBehaviour, IDestructionSystem
    {
        // === СОБЫТИЯ ===
        public event Action<List<Vector2Int>> OnDestructionStarted;
        public event Action<List<Vector2Int>> OnDestructionCompleted;

        // === НАСТРОЙКИ ===
        [Header("Settings")]
        [SerializeField] private float _destroyDuration = 0.2f;
        [SerializeField] private Ease _scaleEase = Ease.InBack;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private GridComponent _grid;

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        public async Task DestroyElements(List<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0)
                return;

            OnDestructionStarted?.Invoke(positions);

            var tasks = new List<Task>();

            foreach (var pos in positions)
            {
                var element = _grid.GetElementAt(pos);
                if (element == null)
                    continue;

                // Очищаем ячейку сразу (логически элемент уже удалён)
                _grid.ClearCell(pos);

                // Запускаем анимацию параллельно
                if (element is ElementComponent elementComponent)
                {
                    tasks.Add(AnimateDestruction(elementComponent));
                }
                else if (element.GameObject != null)
                {
                    // Fallback для других типов IGridElement
                    Destroy(element.GameObject);
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            OnDestructionCompleted?.Invoke(positions);
        }

        public async Task DestroyElement(Vector2Int pos)
        {
            await DestroyElements(new List<Vector2Int> { pos });
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        private async Task AnimateDestruction(ElementComponent element)
        {
            if (element == null || element.gameObject == null)
                return;

            var spriteRenderer = element.GetComponent<SpriteRenderer>();
            var sequence = DOTween.Sequence();

            // Scale down
            sequence.Join(
                element.transform
                    .DOScale(0f, _destroyDuration)
                    .SetEase(_scaleEase)
            );

            // Fade out
            if (spriteRenderer != null)
            {
                sequence.Join(
                    spriteRenderer.DOFade(0f, _destroyDuration)
                );
            }

            await sequence.AsyncWaitForCompletion();

            if (element != null && element.gameObject != null)
            {
                Destroy(element.gameObject);
            }
        }
    }
}
