# Этап 4: Board State (Состояние доски)

## Цель
Создать компонент для хранения и управления состоянием игровой доски - какой элемент находится в какой ячейке.

---

## 4.1 BoardComponent

### Ответственность
- Хранение 2D массива элементов
- CRUD операции над элементами
- Запрос состояния ячеек

### API

```csharp
public class BoardComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action<Vector2Int, ElementComponent> OnElementSet;
    public event Action<Vector2Int> OnElementRemoved;
    public event Action OnBoardCleared;

    // === ЗАВИСИМОСТИ ===
    [SerializeField] private GridComponent _grid;

    // === СОСТОЯНИЕ ===
    private ElementComponent[,] _elements;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

    /// <summary>
    /// Инициализация массива по размерам сетки
    /// </summary>
    public void Initialize()
    {
        _elements = new ElementComponent[_grid.Width, _grid.Height];
    }

    /// <summary>
    /// Инициализация с готовым массивом (от InitialBoardSpawner)
    /// </summary>
    public void Initialize(ElementComponent[,] elements)
    {
        _elements = elements;
    }

    /// <summary>
    /// Получить элемент по позиции
    /// </summary>
    public ElementComponent GetElement(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return null;
        return _elements[pos.x, pos.y];
    }

    /// <summary>
    /// Установить элемент в позицию
    /// </summary>
    public void SetElement(Vector2Int pos, ElementComponent element)
    {
        if (!IsValidPosition(pos)) return;

        _elements[pos.x, pos.y] = element;
        if (element != null)
        {
            element.GridPosition = pos;
        }
        OnElementSet?.Invoke(pos, element);
    }

    /// <summary>
    /// Удалить элемент из позиции (не уничтожает объект!)
    /// </summary>
    public ElementComponent RemoveElement(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return null;

        var element = _elements[pos.x, pos.y];
        _elements[pos.x, pos.y] = null;
        OnElementRemoved?.Invoke(pos);
        return element;
    }

    /// <summary>
    /// Проверка - пустая ли ячейка
    /// </summary>
    public bool IsEmpty(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return false;
        return _elements[pos.x, pos.y] == null;
    }

    /// <summary>
    /// Получить все пустые позиции
    /// </summary>
    public List<Vector2Int> GetEmptyPositions()
    {
        var result = new List<Vector2Int>();
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                if (_elements[x, y] == null)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Получить пустые позиции в столбце (для Fall System)
    /// </summary>
    public List<int> GetEmptyRowsInColumn(int column)
    {
        var result = new List<int>();
        for (int y = 0; y < _grid.Height; y++)
        {
            if (_elements[column, y] == null)
            {
                result.Add(y);
            }
        }
        return result;
    }

    /// <summary>
    /// Получить тип элемента (для MatchFinder)
    /// </summary>
    public ElementType? GetElementType(Vector2Int pos)
    {
        var element = GetElement(pos);
        return element?.Type;
    }

    /// <summary>
    /// Поменять местами два элемента
    /// </summary>
    public void SwapElements(Vector2Int posA, Vector2Int posB)
    {
        var elementA = _elements[posA.x, posA.y];
        var elementB = _elements[posB.x, posB.y];

        _elements[posA.x, posA.y] = elementB;
        _elements[posB.x, posB.y] = elementA;

        if (elementA != null) elementA.GridPosition = posB;
        if (elementB != null) elementB.GridPosition = posA;
    }

    /// <summary>
    /// Очистить всю доску
    /// </summary>
    public void Clear()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                _elements[x, y] = null;
            }
        }
        OnBoardCleared?.Invoke();
    }

    // === ПРИВАТНЫЕ МЕТОДЫ ===

    private bool IsValidPosition(Vector2Int pos)
    {
        return _grid.IsValidPosition(pos);
    }

    // === DEBUG ===

    /// <summary>
    /// Размеры доски
    /// </summary>
    public int Width => _grid.Width;
    public int Height => _grid.Height;
}
```

---

## 4.2 Интеграция с существующими компонентами

### Связь с GridComponent
```
GridComponent           BoardComponent
     │                       │
     │   [SerializeField]    │
     └───────────────────────┤
                             │
     Размеры сетки ──────────┘
     Валидация позиций
```

### Связь с InitialBoardSpawner
```
InitialBoardSpawner         BoardComponent
        │                        │
        │   SpawnedElements      │
        └────────────────────────┤
                                 │
        Готовый массив ──────────┘
```

**Обновить InitialBoardSpawner:**
```csharp
public class InitialBoardSpawner : MonoBehaviour
{
    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactory _factory;
    [SerializeField] private BoardComponent _board; // ДОБАВИТЬ

    public void SpawnInitialBoard()
    {
        var elements = new ElementComponent[_grid.Width, _grid.Height];

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var gridPos = new Vector2Int(x, y);
                var worldPos = _grid.GridToWorld(gridPos);
                var element = SpawnWithoutMatches(x, y, worldPos, gridPos);
                elements[x, y] = element;
            }
        }

        // Инициализировать BoardComponent
        _board.Initialize(elements);
    }
}
```

---

## 4.3 Структура файлов

```
Assets/Scripts/Board/
└── BoardComponent.cs
```

---

## 4.4 Checklist реализации

### BoardComponent.cs
- [ ] Создать папку `Assets/Scripts/Board/`
- [ ] Создать `BoardComponent.cs`
- [ ] События: OnElementSet, OnElementRemoved, OnBoardCleared
- [ ] SerializeField для GridComponent
- [ ] Метод Initialize() - пустой массив
- [ ] Метод Initialize(ElementComponent[,]) - с данными
- [ ] Метод GetElement(Vector2Int)
- [ ] Метод SetElement(Vector2Int, ElementComponent)
- [ ] Метод RemoveElement(Vector2Int) - возвращает элемент
- [ ] Метод IsEmpty(Vector2Int)
- [ ] Метод GetEmptyPositions()
- [ ] Метод GetEmptyRowsInColumn(int)
- [ ] Метод GetElementType(Vector2Int)
- [ ] Метод SwapElements(Vector2Int, Vector2Int)
- [ ] Метод Clear()
- [ ] Свойства Width, Height

### Интеграция
- [ ] Обновить InitialBoardSpawner - добавить ссылку на BoardComponent
- [ ] Обновить InitialBoardSpawner - вызывать _board.Initialize(elements)

### Scene Setup
- [ ] Добавить BoardComponent на GameManager
- [ ] Связать GridComponent → BoardComponent
- [ ] Связать BoardComponent → InitialBoardSpawner

### Тестирование
- [ ] Запустить сцену - доска инициализируется
- [ ] Проверить GetElement возвращает правильные элементы
- [ ] Проверить IsEmpty для пустых/непустых ячеек
- [ ] Debug.Log состояния после инициализации

---

## 4.5 Порядок реализации

1. **Создать BoardComponent.cs** (~30 строк базовой логики)
2. **Обновить InitialBoardSpawner** (добавить 2 строки)
3. **Настроить сцену** (связать компоненты)
4. **Тест** - запустить и проверить

---

## 4.6 Примечания

### Почему BoardComponent отдельно от GridComponent?

**Single Responsibility:**
- `GridComponent` - геометрия сетки (размеры, конверсия координат)
- `BoardComponent` - состояние игры (какие элементы где)

Это разные ответственности. Grid не меняется во время игры, Board меняется постоянно.

### Почему RemoveElement возвращает элемент?

Для пулинга. Когда удаляем элемент с доски - не уничтожаем его, а возвращаем в пул через ElementFactory.Return().

### События нужны?

Да, для будущих систем:
- UI обновление при изменении доски
- Звуки при установке/удалении
- Аналитика

---

## 4.7 Время реализации

~15-20 минут:
- BoardComponent.cs: 10 мин
- Интеграция: 5 мин
- Тест: 5 мин
