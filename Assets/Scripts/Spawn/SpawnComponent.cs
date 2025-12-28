using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Core;
using Random = UnityEngine.Random;

namespace Match3.Spawn
{
    /// <summary>
    /// Система спауна элементов на сетке.
    /// Гарантирует отсутствие матчей при начальном заполнении.
    /// </summary>
    public class SpawnComponent : MonoBehaviour, ISpawnSystem
    {
        // === СОБЫТИЯ ===
        public event Action OnGridFilled;

        // === ЗАВИСИМОСТИ ===
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour _gridComponent;
        [SerializeField] private MonoBehaviour _factoryComponent;

        private IGrid _grid;
        private IElementFactory _factory;

        // === КЭШИРОВАННЫЕ ДАННЫЕ ===
        private static readonly ElementType[] AllTypes =
        {
            ElementType.Red,
            ElementType.Green,
            ElementType.Blue,
            ElementType.Yellow,
            ElementType.Purple
        };

        private readonly List<ElementType> _availableTypes = new(5);

        // === UNITY CALLBACKS ===

        private void Awake()
        {
            _grid = _gridComponent as IGrid;
            _factory = _factoryComponent as IElementFactory;

            if (_grid == null)
                Debug.LogError("SpawnComponent: Grid component must implement IGrid", this);
            if (_factory == null)
                Debug.LogError("SpawnComponent: Factory component must implement IElementFactory", this);
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Заполняет всю сетку элементами без создания матчей.
        /// Заполнение идёт снизу вверх, слева направо.
        /// </summary>
        public void FillGrid()
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    SpawnAt(new Vector2Int(x, y));
                }
            }

            OnGridFilled?.Invoke();
        }

        /// <summary>
        /// Спавнит элемент в указанной позиции сетки.
        /// </summary>
        public IGridElement SpawnAt(Vector2Int gridPos)
        {
            if (!_grid.IsValidPosition(gridPos))
                return null;

            if (_grid.GetElementAt(gridPos) != null)
                return null;

            var type = GetRandomTypeWithoutMatch(gridPos);
            var worldPos = _grid.GridToWorld(gridPos);
            var element = _factory.Create(type, worldPos);

            element.GridPosition = gridPos;
            _grid.SetElementAt(gridPos, element);

            return element;
        }

        /// <summary>
        /// Спавнит элемент над сеткой для последующего падения.
        /// Используется системой гравитации.
        /// </summary>
        public IGridElement SpawnAtTop(int column)
        {
            var spawnGridPos = new Vector2Int(column, _grid.Height);
            var worldPos = _grid.GridToWorld(spawnGridPos);

            var type = AllTypes[Random.Range(0, AllTypes.Length)];
            var element = _factory.Create(type, worldPos);

            return element;
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Выбирает случайный тип элемента, который не создаст матч в данной позиции.
        /// Проверяет 2 элемента слева и 2 элемента снизу.
        /// </summary>
        private ElementType GetRandomTypeWithoutMatch(Vector2Int pos)
        {
            _availableTypes.Clear();
            _availableTypes.AddRange(AllTypes);

            // Проверка 2 элементов слева
            if (pos.x >= 2)
            {
                var left1 = _grid.GetElementAt(pos + Vector2Int.left);
                var left2 = _grid.GetElementAt(pos + Vector2Int.left * 2);

                if (left1 != null && left2 != null && left1.Type == left2.Type)
                    _availableTypes.Remove(left1.Type);
            }

            // Проверка 2 элементов снизу
            if (pos.y >= 2)
            {
                var down1 = _grid.GetElementAt(pos + Vector2Int.down);
                var down2 = _grid.GetElementAt(pos + Vector2Int.down * 2);

                if (down1 != null && down2 != null && down1.Type == down2.Type)
                    _availableTypes.Remove(down1.Type);
            }

            return _availableTypes[Random.Range(0, _availableTypes.Count)];
        }
    }
}
