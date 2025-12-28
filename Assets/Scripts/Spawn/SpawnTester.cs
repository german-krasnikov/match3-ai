using UnityEngine;

namespace Match3.Spawn
{
    /// <summary>
    /// Тестовый компонент для проверки SpawnComponent.
    /// </summary>
    public class SpawnTester : MonoBehaviour
    {
        [SerializeField] private SpawnComponent _spawn;
        [SerializeField] private bool _fillOnStart = true;

        private void Start()
        {
            if (_fillOnStart && _spawn != null)
            {
                _spawn.FillGrid();
                Debug.Log("Grid filled! Check visually for no 3+ matches.");
            }
        }
    }
}
