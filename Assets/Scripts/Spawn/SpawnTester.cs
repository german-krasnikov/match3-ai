using UnityEngine;

namespace Match3.Spawn
{
    /// <summary>
    /// Тестовый компонент для проверки SpawnComponent.
    /// Нажмите Space для заполнения сетки.
    /// </summary>
    public class SpawnTester : MonoBehaviour
    {
        [SerializeField] private SpawnComponent _spawn;

        private bool _filled;

        private void OnEnable()
        {
            if (_spawn != null)
                _spawn.OnGridFilled += OnGridFilled;
        }

        private void OnDisable()
        {
            if (_spawn != null)
                _spawn.OnGridFilled -= OnGridFilled;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && !_filled)
            {
                Debug.Log("Filling grid...");
                _spawn.FillGrid();
            }
        }

        private void OnGridFilled()
        {
            _filled = true;
            Debug.Log("Grid filled! Check visually for no 3+ matches.");
        }
    }
}
