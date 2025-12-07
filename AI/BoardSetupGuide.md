# Board System - Инструкция по настройке в Unity

## 1. Создание TileData ассетов

1. `Assets/Data/Tiles/` → ПКМ → Create → Match3 → TileData
2. Создать 5-6 ассетов:
   - `TileData_Red.asset` → Type: Red, Color: Red
   - `TileData_Blue.asset` → Type: Blue, Color: Blue
   - `TileData_Green.asset` → Type: Green, Color: Green
   - `TileData_Yellow.asset` → Type: Yellow, Color: Yellow
   - `TileData_Purple.asset` → Type: Purple, Color: Magenta

> Спрайты можно добавить позже, пока будет цвет

---

## 2. Создание Cell.prefab

1. Hierarchy → Create Empty → назвать "Cell"
2. Добавить компоненты:
   - `SpriteRenderer` (Sorting Layer: Default, Order: 0)
   - `CellComponent`
3. В CellComponent:
   - `_backgroundRenderer` → перетащить SpriteRenderer
4. Установить спрайт (квадрат) или оставить пустым
5. Перетащить в `Assets/Prefabs/Cell.prefab`

---

## 3. Создание Tile.prefab

1. Hierarchy → Create Empty → назвать "Tile"
2. Добавить компоненты:
   - `SpriteRenderer` (Sorting Layer: Default, Order: 1)
   - `TileComponent`
3. В TileComponent:
   - `_spriteRenderer` → перетащить SpriteRenderer
4. Установить спрайт (круг/квадрат) для визуала
5. Scale: (0.8, 0.8, 1) — чтобы был меньше ячейки
6. Перетащить в `Assets/Prefabs/Tile.prefab`

---

## 4. Создание Board на сцене

### Структура:
```
Board (Empty GameObject)
├── Grid (Empty)
└── Tiles (Empty, контейнер для тайлов)
```

### Настройка:

1. **Board** объект:
   - Добавить `BoardController`
   - `_grid` → Grid объект
   - `_spawner` → TileSpawner (добавить на Board)

2. **Grid** объект:
   - Добавить `GridComponent`
   - `_width`: 8
   - `_height`: 8
   - `_cellSize`: 1
   - `_cellPrefab` → Cell.prefab

3. **TileSpawner** (на Board):
   - Добавить `TileSpawner`
   - `_tilePrefab` → Tile.prefab
   - `_grid` → Grid объект
   - `_tileContainer` → Tiles объект
   - `_availableTiles` → все TileData ассеты

---

## 5. Настройка камеры

Для поля 8x8 с cellSize=1:
- Camera Position: (3.5, 3.5, -10)
- Camera Size: 5 (Orthographic)

Или добавить offset в GridComponent:
- `_originOffset`: (-3.5, -3.5, 0) — центрирование

---

## 6. Тест

1. Play → должна появиться сетка 8x8
2. Каждая ячейка с тайлом случайного цвета
3. Не должно быть 3+ одинаковых в ряд (начальных матчей)

---

## Чеклист

```
□ TileData ассеты созданы (5-6 цветов)
□ Cell.prefab создан с CellComponent
□ Tile.prefab создан с TileComponent
□ Board объект на сцене
□ GridComponent настроен
□ TileSpawner настроен с TileData
□ Камера настроена
□ Play → поле генерируется
□ Нет начальных матчей
```

---

## Отладка

### Поле не появляется:
- Проверить что Cell.prefab назначен в GridComponent
- Проверить что TileData массив заполнен в TileSpawner

### Тайлы не видны:
- Проверить Sorting Order (Tile должен быть выше Cell)
- Проверить что _spriteRenderer назначен в TileComponent
- Проверить цвет в TileData (не чёрный/прозрачный)

### Тайлы в неправильных позициях:
- Проверить _cellSize в GridComponent
- Проверить _originOffset
