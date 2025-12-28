using UnityEngine;
using Match3.Core;

namespace Match3.Gravity
{
    /// <summary>
    /// Тестер для системы гравитации.
    /// Клик удаляет элемент, Space применяет гравитацию.
    /// </summary>
    public class GravityTester : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _gridComponent;
        [SerializeField] private MonoBehaviour _gravityComponent;
        [SerializeField] private Camera _camera;

        private IGrid _grid;
        private IGravitySystem _gravity;
        private bool _isProcessing;

        private void Awake()
        {
            _grid = _gridComponent as IGrid;
            _gravity = _gravityComponent as IGravitySystem;

            if (_camera == null)
                _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (_gravity != null)
            {
                _gravity.OnGravityStarted += OnGravityStarted;
                _gravity.OnGravityCompleted += OnGravityCompleted;
            }
        }

        private void OnDisable()
        {
            if (_gravity != null)
            {
                _gravity.OnGravityStarted -= OnGravityStarted;
                _gravity.OnGravityCompleted -= OnGravityCompleted;
            }
        }

        private void Update()
        {
            if (_isProcessing) return;

            // Клик - удалить элемент
            if (Input.GetMouseButtonDown(0))
            {
                var worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                var gridPos = _grid.WorldToGrid(worldPos);

                if (_grid.IsValidPosition(gridPos))
                {
                    var element = _grid.GetElementAt(gridPos);
                    if (element != null)
                    {
                        Destroy(element.GameObject);
                        _grid.ClearCell(gridPos);
                        Debug.Log($"Removed element at {gridPos}");
                    }
                }
            }

            // Space - применить гравитацию
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_gravity.HasEmptyCells())
                {
                    _ = _gravity.ApplyGravity();
                }
                else
                {
                    Debug.Log("No empty cells");
                }
            }

            // G - авто-гравитация (удалить + применить)
            if (Input.GetKeyDown(KeyCode.G))
            {
                AutoGravityTest();
            }
        }

        private async void AutoGravityTest()
        {
            // Удаляем случайный элемент
            int x = Random.Range(0, _grid.Width);
            int y = Random.Range(0, _grid.Height);
            var pos = new Vector2Int(x, y);

            var element = _grid.GetElementAt(pos);
            if (element != null)
            {
                Destroy(element.GameObject);
                _grid.ClearCell(pos);
                Debug.Log($"Auto-removed at {pos}");

                await _gravity.ApplyGravity();
            }
        }

        private void OnGravityStarted()
        {
            _isProcessing = true;
            Debug.Log("Gravity started");
        }

        private void OnGravityCompleted()
        {
            _isProcessing = false;
            Debug.Log("Gravity completed");
        }
    }
}
