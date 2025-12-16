using System;
using UnityEngine;

public class InputComponent : MonoBehaviour
{
    public event Action<Cell, Cell> OnSwapRequested;

    [SerializeField] private GridComponent _grid;
    [SerializeField] private SwapConfig _config;
    [SerializeField] private SwapComponent _swap;
    [SerializeField] private Camera _camera;

    private Vector3 _startWorldPos;
    private Cell _startCell;
    private bool _isDragging;

    public bool IsEnabled { get; set; } = true;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void OnEnable()
    {
        if (_swap != null)
        {
            _swap.OnSwapStarted += OnSwapStarted;
            _swap.OnSwapCompleted += OnSwapCompleted;
        }
    }

    private void OnDisable()
    {
        if (_swap != null)
        {
            _swap.OnSwapStarted -= OnSwapStarted;
            _swap.OnSwapCompleted -= OnSwapCompleted;
        }
    }

    private void Update()
    {
        if (!IsEnabled) return;

        if (Input.GetMouseButtonDown(0))
            OnPointerDown();
        else if (Input.GetMouseButtonUp(0) && _isDragging)
            OnPointerUp();
    }

    private void OnPointerDown()
    {
        _startWorldPos = GetWorldMousePosition();
        Vector2Int gridPos = _grid.WorldToGrid(_startWorldPos);
        _startCell = _grid.GetCell(gridPos);

        if (_startCell == null || _startCell.IsEmpty)
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
    }

    private void OnPointerUp()
    {
        _isDragging = false;

        Vector3 endWorldPos = GetWorldMousePosition();
        Vector3 delta = endWorldPos - _startWorldPos;

        if (delta.magnitude < _config.MinSwipeDistance) return;

        Vector2Int direction = GetSwipeDirection(delta);
        Cell targetCell = _grid.GetNeighbor(_startCell, direction);

        if (targetCell == null) return;

        OnSwapRequested?.Invoke(_startCell, targetCell);
        _swap?.RequestSwap(_startCell, targetCell);
    }

    private Vector2Int GetSwipeDirection(Vector3 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? GridDirections.Right : GridDirections.Left;
        else
            return delta.y > 0 ? GridDirections.Up : GridDirections.Down;
    }

    private Vector3 GetWorldMousePosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -_camera.transform.position.z;
        return _camera.ScreenToWorldPoint(mousePos);
    }

    private void OnSwapStarted(Cell a, Cell b)
    {
        IsEnabled = false;
    }

    private void OnSwapCompleted(Cell a, Cell b, bool success)
    {
        IsEnabled = true;
    }
}
