using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Match3.Core;

namespace Match3.Gravity
{
    /// <summary>
    /// Применяет гравитацию к элементам сетки.
    /// После уничтожения матчей элементы падают вниз,
    /// пустые места заполняются новыми элементами сверху.
    /// </summary>
    public class GravityComponent : MonoBehaviour, IGravitySystem
    {
        // === СОБЫТИЯ ===
        public event Action OnGravityStarted;
        public event Action OnGravityCompleted;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour _gridComponent;
        [SerializeField] private MonoBehaviour _spawnComponent;

        private IGrid _grid;
        private ISpawnSystem _spawn;

        // === НАСТРОЙКИ ===
        [Header("Settings")]
        [SerializeField] private float _fallDuration = 0.3f;
        [SerializeField] private Ease _fallEase = Ease.OutBounce;

        // === UNITY CALLBACKS ===

        private void Awake()
        {
            _grid = _gridComponent as IGrid;
            _spawn = _spawnComponent as ISpawnSystem;

            if (_grid == null)
                Debug.LogError("GravityComponent: Grid component must implement IGrid", this);
            if (_spawn == null)
                Debug.LogError("GravityComponent: Spawn component must implement ISpawnSystem", this);
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        public bool HasEmptyCells()
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    if (_grid.GetElementAt(new Vector2Int(x, y)) == null)
                        return true;
                }
            }
            return false;
        }

        public async Task ApplyGravity()
        {
            OnGravityStarted?.Invoke();

            // Обрабатываем все колонки параллельно
            var columnTasks = new List<Task>();
            for (int x = 0; x < _grid.Width; x++)
            {
                columnTasks.Add(ProcessColumn(x));
            }

            await Task.WhenAll(columnTasks);

            OnGravityCompleted?.Invoke();
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        private async Task ProcessColumn(int column)
        {
            // Фаза 1: Падение существующих элементов
            await FallExistingElements(column);

            // Фаза 2: Спаун новых элементов сверху
            await SpawnNewElements(column);
        }

        private async Task FallExistingElements(int column)
        {
            var animations = new List<Task>();

            // Проходим снизу вверх
            for (int y = 0; y < _grid.Height; y++)
            {
                var pos = new Vector2Int(column, y);

                if (_grid.GetElementAt(pos) != null)
                    continue;

                // Ищем первый элемент выше
                for (int above = y + 1; above < _grid.Height; above++)
                {
                    var abovePos = new Vector2Int(column, above);
                    var element = _grid.GetElementAt(abovePos);

                    if (element != null)
                    {
                        _grid.ClearCell(abovePos);
                        _grid.SetElementAt(pos, element);

                        var task = AnimateFall(element, abovePos, pos);
                        animations.Add(task);
                        break;
                    }
                }
            }

            await Task.WhenAll(animations);
        }

        private async Task SpawnNewElements(int column)
        {
            var animations = new List<Task>();

            // Считаем пустые ячейки сверху
            int emptyCount = 0;
            for (int y = _grid.Height - 1; y >= 0; y--)
            {
                if (_grid.GetElementAt(new Vector2Int(column, y)) == null)
                    emptyCount++;
                else
                    break;
            }

            if (emptyCount == 0)
                return;

            // Спавним новые элементы
            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = _grid.Height - emptyCount + i;
                var targetPos = new Vector2Int(column, targetY);
                var startPos = new Vector2Int(column, _grid.Height + i);

                var element = _spawn.SpawnAtTop(column);
                _grid.SetElementAt(targetPos, element);

                // Позиционируем над сеткой
                element.GameObject.transform.position = _grid.GridToWorld(startPos);

                var task = AnimateFall(element, startPos, targetPos);
                animations.Add(task);
            }

            await Task.WhenAll(animations);
        }

        private async Task AnimateFall(IGridElement element, Vector2Int from, Vector2Int to)
        {
            var targetWorldPos = _grid.GridToWorld(to);

            var tween = element.GameObject.transform
                .DOMove(targetWorldPos, _fallDuration)
                .SetEase(_fallEase);

            await tween.AsyncWaitForCompletion();
        }
    }
}
