# Этап 4: Board State (Состояние доски) ✅

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

    // === ЗАВИСИМОСТИ ===
    [SerializeField] private GridComponent _grid;

    // === СОСТОЯНИЕ ===
    private ElementComponent[,] _elements;

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void Initialize();
    public void Initialize(ElementComponent[,] elements);
    public ElementComponent GetElement(Vector2Int pos);
    public void SetElement(Vector2Int pos, ElementComponent element);
    public ElementComponent RemoveElement(Vector2Int pos);
    public bool IsEmpty(Vector2Int pos);
    public List<Vector2Int> GetEmptyPositions();
    public List<int> GetEmptyRowsInColumn(int column);
    public ElementType? GetElementType(Vector2Int pos);
    public void SwapElements(Vector2Int posA, Vector2Int posB);
    public void Clear();

    // === СВОЙСТВА ===
    public int Width => _grid.Width;
    public int Height => _grid.Height;
}
```

---

## 4.2 Интеграция с существующими компонентами

### Диаграмма связей
```
┌─────────────────┐
│  GridComponent  │
└────────┬────────┘
         │ [SerializeField]
         ▼
┌─────────────────┐      ┌──────────────────────┐
│ BoardComponent  │◄─────│ InitialBoardSpawner  │
└─────────────────┘      └──────────────────────┘
                              │
                              │ Initialize(elements)
                              ▼
                         После спауна
```

---

## 4.3 Структура файлов

```
Assets/Scripts/
├── Board/
│   └── BoardComponent.cs        ✅
└── Editor/
    └── BoardSystemSetup.cs      ✅
```

---

## 4.4 Editor Setup

### Автоматическая настройка сцены

**Меню:** `Match3 → Setup Scene → Stage 4 - Board System`

**Что делает скрипт:**
1. Проверяет наличие `GridComponent` (Stage 1)
2. Проверяет наличие `InitialBoardSpawner` (Stage 3)
3. Добавляет `BoardComponent` на GameObject с `GridComponent`
4. Связывает зависимости:
   - `BoardComponent._grid` → `GridComponent`
   - `InitialBoardSpawner._board` → `BoardComponent`

**Файл:** `Assets/Scripts/Editor/BoardSystemSetup.cs`

---

## 4.5 Checklist реализации

### BoardComponent.cs ✅
- [x] Создать папку `Assets/Scripts/Board/`
- [x] Создать `BoardComponent.cs`
- [x] События: OnElementSet, OnElementRemoved
- [x] SerializeField для GridComponent
- [x] Метод Initialize() - пустой массив
- [x] Метод Initialize(ElementComponent[,]) - с данными
- [x] Метод GetElement(Vector2Int)
- [x] Метод SetElement(Vector2Int, ElementComponent)
- [x] Метод RemoveElement(Vector2Int) - возвращает элемент
- [x] Метод IsEmpty(Vector2Int)
- [x] Метод GetEmptyPositions()
- [x] Метод GetEmptyRowsInColumn(int)
- [x] Метод GetElementType(Vector2Int)
- [x] Метод SwapElements(Vector2Int, Vector2Int)
- [x] Метод Clear()
- [x] Свойства Width, Height

### Интеграция ✅
- [x] Обновить InitialBoardSpawner - добавить ссылку на BoardComponent
- [x] Обновить InitialBoardSpawner - вызывать _board.Initialize(elements)

### Editor Setup ✅
- [x] Создать `BoardSystemSetup.cs`
- [x] MenuItem для автонастройки сцены

### Тестирование
- [ ] Запустить `Match3 → Setup Scene → Stage 4 - Board System`
- [ ] Play mode - доска инициализируется без ошибок

---

## 4.6 Примечания

### Почему BoardComponent отдельно от GridComponent?

**Single Responsibility:**
- `GridComponent` - геометрия сетки (размеры, конверсия координат)
- `BoardComponent` - состояние игры (какие элементы где)

Grid не меняется во время игры, Board меняется постоянно.

### Почему RemoveElement возвращает элемент?

Для пулинга. Когда удаляем элемент с доски - не уничтожаем его, а возвращаем в пул через `ElementFactory.Return()`.

### События нужны?

Да, для будущих систем:
- UI обновление при изменении доски
- Звуки при установке/удалении
- Аналитика

---

## 4.7 Использование

### Быстрый старт
```
1. Unity → Match3 → Setup Scene → Stage 4 - Board System
2. Play
```

### Ручная настройка (если нужно)
1. Добавить `BoardComponent` на GameObject с `GridComponent`
2. В Inspector `BoardComponent` → перетащить `GridComponent`
3. В Inspector `InitialBoardSpawner` → перетащить `BoardComponent`
