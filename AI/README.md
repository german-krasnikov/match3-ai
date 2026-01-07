# Unity Development Pipeline
## Автоматизированная система разработки с TDD и MCP

---

## 📁 Структура файлов

```
AI/
├── README.md                           ← Этот файл
├── PIPELINE_GUIDE.md                   ← Главный гайд по пайплайну
├── STEP_WORKFLOW.md                    ← Workflow для @step N
├── UNITY_MVP_ARCHITECTURE_GUIDE.md     ← Архитектурные паттерны MVP
├── UNITY_TDD_GUIDE.md                  ← Руководство по TDD
├── UNITY_MCP_GUIDE.md                  ← Справочник по MCP
│
├── plan.md                             ← [Создаётся] Глобальный план
├── step_1.md                           ← [Создаётся] Детализация шага 1
├── step_2.md                           ← [Создаётся] Детализация шага 2
└── ...

agents/                                  ← Скопировать в .claude/
├── unity-architect.md                  ← Агент архитектор
├── unity-programmer.md                 ← Агент программист
└── unity-mcp.md                        ← Агент MCP (валидация)
```

---

## 🚀 Быстрый старт

### 1. Настройка проекта

1. Скопируй папку `AI/` в корень своего Unity проекта
2. Скопируй агентов из `agents/` в `.claude/` (для Claude Code)
3. Настрой UnityMCP:
   - Window > MCP for Unity
   - Start Local HTTP Server

### 2. Создание плана

```
Создай план для [название игры]. Используй @AI/PIPELINE_GUIDE.md
```

### 3. Цикл разработки

```
@step N
```

Запускает полный цикл:
1. **Детализация** → unity-architect создаёт step_{N}.md
2. **Реализация** → unity-programmer создаёт код
3. **Валидация** → unity-mcp проверяет компиляцию + тесты
4. **Настройка сцены** → пользователь запускает Setup/Step N

---

## 📋 Краткий справочник

### Агенты

| Агент | Модель | Назначение |
|-------|--------|------------|
| unity-architect | opus | Проектирование, создание step_{N}.md |
| unity-programmer | sonnet | Реализация кода по спецификации |
| unity-mcp | sonnet | Валидация: компиляция + тесты |

### Разделение ответственности

| Что | Кто делает |
|-----|------------|
| Компиляция + тесты | MCP (автоматически) |
| Scene Setup | Пользователь (вручную) |

### MVP паттерн

```
Model (C#)      ← Логика, тестируемый
    ↕ events
Presenter (C#)  ← Связь, тестируемый с mock
    ↕ interface
View (MB)       ← UI, не тестируется
```

### Структура проекта

```
Assets/
├── Scripts/
│   ├── Game.asmdef
│   ├── Core/
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── {Feature}Model.cs
│   │       ├── {Feature}Presenter.cs
│   │       ├── {Feature}View.cs
│   │       └── I{Feature}View.cs
│   ├── Configs/
│   └── Editor/
│       └── Editor.asmdef
└── Tests/
    └── EditMode/
        └── Tests.EditMode.asmdef
```

---

## ⚠️ Важные правила

1. **Простота превыше всего** — SOLID, DRY, KISS без переусложнений
2. **План — закон** — программист реализует строго по спецификации
3. **Валидация обязательна** — не переходи к следующему шагу без ✅
4. **MCP НЕ настраивает сцену** — только компиляция + тесты
5. **Тесты для Model и Presenter** — View не тестируется

---

## 📖 Полная документация

- [PIPELINE_GUIDE.md](./PIPELINE_GUIDE.md) — полное описание процесса
- [STEP_WORKFLOW.md](./STEP_WORKFLOW.md) — workflow для @step N
- [UNITY_MVP_ARCHITECTURE_GUIDE.md](./UNITY_MVP_ARCHITECTURE_GUIDE.md) — архитектура
- [UNITY_TDD_GUIDE.md](./UNITY_TDD_GUIDE.md) — тестирование
- [UNITY_MCP_GUIDE.md](./UNITY_MCP_GUIDE.md) — работа с MCP

---

**Версия:** 2.0  
**Совместимость:** Unity 2021.3+, Claude Code, UnityMCP 8.x
