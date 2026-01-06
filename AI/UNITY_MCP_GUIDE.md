# Unity MCP Guide
## Руководство по работе с Unity через MCP

---

## 📋 Обзор

Unity MCP (Model Context Protocol) позволяет взаимодействовать с Unity Editor программно:
- Проверять состояние компиляции
- Читать консоль
- Запускать тесты
- Создавать объекты на сцене
- Выполнять меню команды

---

## 🔧 Доступные инструменты

### 1. read_console
Читает сообщения из Unity Console.

```json
{
  "action": "read_console"
}
```

**Возвращает:**
- Ошибки компиляции
- Warnings
- Debug.Log сообщения

**Использование:**
```
Проверка компиляции:
1. Вызови read_console
2. Ищи "[Error]" или "CS" коды
3. Если есть ошибки — компиляция не прошла
```

### 2. run_tests
Запускает тесты в Unity Test Runner.

```json
{
  "action": "run_tests",
  "testMode": "EditMode",
  "testFilter": "Step1"
}
```

**Параметры:**
| Параметр | Значения | Описание |
|----------|----------|----------|
| testMode | "EditMode", "PlayMode" | Тип тестов |
| testFilter | string | Фильтр по имени (опционально) |

**Важно:**
- Тесты выполняются асинхронно
- После запуска нужно дождаться завершения
- Проверяй статус через `editor_state`

### 3. execute_menu_item
Выполняет команду из меню Unity.

```json
{
  "action": "execute_menu_item",
  "menuPath": "Setup/Step 1"
}
```

**Примеры путей:**
- `"File/Save Project"`
- `"Setup/Step 1"`
- `"Window/General/Console"`

### 4. manage_gameobject
Управляет игровыми объектами.

```json
{
  "action": "manage_gameobject",
  "operation": "create",
  "name": "Player",
  "primitiveType": "Cube"
}
```

**Операции:**
- `create` — создать объект
- `delete` — удалить объект
- `find` — найти объект
- `modify` — изменить свойства
- `add_component` — добавить компонент

### 5. manage_scene
Управляет сценами.

```json
{
  "action": "manage_scene",
  "operation": "get_hierarchy"
}
```

**Операции:**
- `load` — загрузить сцену
- `save` — сохранить сцену
- `create` — создать сцену
- `get_hierarchy` — получить иерархию

### 6. validate_script
Проверяет синтаксис C# скрипта.

```json
{
  "action": "validate_script",
  "path": "Assets/Scripts/PlayerModel.cs",
  "level": "standard"
}
```

**Уровни:**
- `basic` — базовая проверка структуры
- `standard` — стандартная валидация
- `strict` — полная проверка (требует Roslyn)

---

## 📊 Ресурсы (Resources)

### editor_state
Текущее состояние редактора.

```json
{
  "isPlaying": false,
  "isPaused": false,
  "isCompiling": false,
  "activeScene": "Game.unity"
}
```

### unity_instances
Список запущенных Unity инстансов.

### tests
Список доступных тестов.

### project_info
Информация о проекте.

---

## 🔄 Workflow: Валидация шага

### Последовательность действий

```
┌─────────────────────────────────────────────────────┐
│               VALIDATION WORKFLOW                    │
├─────────────────────────────────────────────────────┤
│                                                      │
│  1. CHECK COMPILATION                                │
│     └─► read_console                                 │
│         └─► Есть ошибки? → STOP, вернуть список     │
│                                                      │
│  2. WAIT FOR COMPILATION                             │
│     └─► editor_state.isCompiling                    │
│         └─► true? → Подождать, повторить            │
│                                                      │
│  3. RUN SCENE SETUP                                  │
│     └─► execute_menu_item("Setup/Step N")           │
│         └─► Ошибка? → Проверить консоль             │
│                                                      │
│  4. RUN TESTS                                        │
│     └─► run_tests(testFilter: "StepN")              │
│         └─► Запускает асинхронно                    │
│                                                      │
│  5. WAIT FOR TESTS                                   │
│     └─► Повторять read_console                      │
│         └─► Искать "All tests passed" или ошибки   │
│                                                      │
│  6. REPORT RESULTS                                   │
│     └─► ✅ Все тесты прошли                         │
│     └─► ❌ Список failed тестов                     │
│                                                      │
└─────────────────────────────────────────────────────┘
```

### Пример сессии

```
>>> Проверяю компиляцию...
[read_console]
Результат: Нет ошибок

>>> Запускаю Scene Setup...
[execute_menu_item: "Setup/Step 1"]
Результат: OK

>>> Запускаю тесты...
[run_tests: testMode="EditMode", testFilter="Step1"]
Результат: Тесты запущены

>>> Ожидаю завершения...
[read_console] (через 2 сек)
Результат: "Running tests..."

[read_console] (через 2 сек)
Результат: "All tests passed: 5/5"

>>> ✅ Шаг 1 валидирован успешно
```

---

## ⚠️ Обработка ошибок

### Ошибки компиляции

```
[Error] CS0246: The type or namespace 'IPlayerView' could not be found
```

**Действия:**
1. Остановить валидацию
2. Вернуть список ошибок
3. Указать файлы с проблемами

### Failed тесты

```
[FAIL] PlayerModelTests.TakeDamage_WhenDead_DoesNothing
Expected: 0
But was: -10
```

**Действия:**
1. Собрать список failed тестов
2. Включить сообщения об ошибках
3. Вернуть отчёт

### Timeout тестов

Если тесты не завершаются более 60 секунд:
1. Проверить editor_state
2. Возможно PlayMode завис
3. Предложить перезапуск

---

## 📝 Шаблоны команд

### Полная валидация шага

```
1. read_console → проверить ошибки
2. Если isCompiling → ждать
3. execute_menu_item("Setup/Step {N}")
4. run_tests(testFilter: "Step{N}")
5. Ждать 2-5 сек
6. read_console → проверить результаты
7. Повторять п.6 пока тесты не завершатся
8. Вернуть отчёт
```

### Создание объекта на сцене

```
1. manage_gameobject(create, name="Player")
2. manage_gameobject(add_component, name="Player", component="PlayerView")
3. manage_scene(save)
```

### Проверка состояния проекта

```
1. project_info → версия Unity, путь
2. editor_state → режим, компиляция
3. tests → список доступных тестов
```

---

## 🔒 Ограничения

1. **Асинхронность тестов**
   - Тесты запускаются асинхронно
   - Нужно polling через read_console
   
2. **PlayMode**
   - Некоторые операции недоступны в PlayMode
   - Проверяй editor_state.isPlaying

3. **Компиляция**
   - Во время компиляции команды могут не выполняться
   - Жди editor_state.isCompiling = false

4. **Права доступа**
   - MCP не может редактировать файлы вне Assets/
   - Некоторые системные папки защищены

---

## 🎯 Best Practices

### DO ✅
- Всегда проверяй компиляцию первой
- Жди завершения асинхронных операций
- Логируй каждый шаг для отладки
- Проверяй editor_state перед операциями

### DON'T ❌
- Не запускай тесты при ошибках компиляции
- Не игнорируй warnings
- Не делай много операций без проверки статуса
- Не считай операцию завершённой без подтверждения

---

## 📊 Статусы валидации

| Статус | Описание | Следующий шаг |
|--------|----------|---------------|
| ✅ PASSED | Все проверки пройдены | Переход к следующему шагу |
| ⚠️ WARNINGS | Есть предупреждения | Ревью warnings, можно продолжить |
| ❌ COMPILE_ERROR | Ошибки компиляции | Исправить код |
| ❌ TEST_FAILED | Тесты не прошли | Исправить тесты или код |
| ⏳ PENDING | Операция выполняется | Ждать завершения |
| 🔄 RETRY | Временная ошибка | Повторить операцию |

---

**Версия:** 1.0  
**Совместимость:** UnityMCP v8.x+  
**Требования:** Unity 2021.3+
