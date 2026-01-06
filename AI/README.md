# Unity Development Pipeline
## Автоматизированная система разработки с TDD и MCP

---

## 📁 Структура файлов

```
AI/
├── README.md                           ← Этот файл
├── PIPELINE_GUIDE.md                   ← Главный гайд по пайплайну
├── UNITY_MVP_ARCHITECTURE_GUIDE.md     ← Архитектурные паттерны MVP
├── UNITY_TDD_GUIDE.md                  ← Руководство по TDD
├── UNITY_MCP_GUIDE.md                  ← Справочник по MCP
├── PROMPTS.md                          ← Шаблоны промтов
│
├── plan.md                             ← [Создаётся] Глобальный план
├── step_1.md                           ← [Создаётся] Детализация шага 1
├── step_2.md                           ← [Создаётся] Детализация шага 2
└── ...

agents/                                  ← Скопировать в .claude/
├── unity-orchestrator.md               ← 🎯 ГЛАВНЫЙ координатор
├── unity-architect.md                  ← Агент архитектор
├── unity-programmer.md                 ← Агент программист
├── unity-parallel-coordinator.md       ← Координатор параллельной работы
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

**Всегда используй `@agent-unity-orchestrator`** — он координирует остальных:

```
@agent-unity-orchestrator детализируй шаг 1
→ Запустит unity-architect, создаст step_1.md

@agent-unity-orchestrator реализуй шаг 1 параллельно  
→ Запустит unity-parallel-coordinator
→ Тот запустит несколько unity-programmer параллельно

@agent-unity-orchestrator валидируй шаг 1
→ Запустит unity-mcp для проверки
```

**Или полный цикл:**
```
@agent-unity-orchestrator полный цикл шаг 1
→ Детализация → Реализация → Валидация (с паузами для подтверждения)
```

---

## 📋 Краткий справочник

### Агенты

| Агент | Модель | Назначение |
|-------|--------|------------|
| **unity-orchestrator** | sonnet | 🎯 ГЛАВНЫЙ — координирует всех, НЕ пишет код |
| unity-architect | opus | Проектирование, создание step_{N}.md |
| unity-programmer | sonnet | Реализация кода по спецификации |
| unity-parallel-coordinator | sonnet | Координация параллельных программистов |
| unity-mcp | sonnet | Валидация через Unity MCP |

### ⚠️ Важно: Оркестратор НЕ пишет код!

Если видишь что оркестратор делает `Write()` или пишет C# — останови его:
```
СТОП! Запусти @agent-unity-programmer для реализации.
```

### Параллельное выполнение

```
step_{N}.md содержит Parallel Execution Plan:

Group A (параллельно):     Group B (после A):     Group C (параллельно):
┌─────────┐ ┌─────────┐    ┌─────────────┐        ┌───────┐ ┌───────────┐
│ Models  │ │  Views  │ ─► │ Presenters  │ ─────► │ Tests │ │Scene Setup│
└─────────┘ └─────────┘    └─────────────┘        └───────┘ └───────────┘
  Agent 1     Agent 2          Agent 3            Agent 4     Agent 5
```

### MVP паттерн

```
Model (C#)      ← Логика, тестируемый
    ↕ events
Presenter (C#)  ← Связь, тестируемый с mock
    ↕ interface
View (MB)       ← UI, не тестируется
```

### Валидация

```
1. read_console      → Проверка компиляции
2. execute_menu_item → Настройка сцены
3. run_tests         → Запуск тестов
4. read_console      → Результаты тестов
```

---

## ⚠️ Важные правила

1. **Всегда используй `@agent-unity-orchestrator`** — он координирует всех
2. **Оркестратор НЕ пишет код** — только делегирует агентам
3. **Изоляция контекста** — не смешивай агентов в одном разговоре
4. **Plan is law** — программист реализует строго по спецификации
5. **Валидация обязательна** — не переходи к следующему шагу без ✅
6. **Тесты для Model и Presenter** — View не тестируется

---

## 📖 Полная документация

- [PIPELINE_GUIDE.md](./PIPELINE_GUIDE.md) — полное описание процесса
- [UNITY_MVP_ARCHITECTURE_GUIDE.md](./UNITY_MVP_ARCHITECTURE_GUIDE.md) — архитектура
- [UNITY_TDD_GUIDE.md](./UNITY_TDD_GUIDE.md) — тестирование
- [UNITY_MCP_GUIDE.md](./UNITY_MCP_GUIDE.md) — работа с MCP
- [PROMPTS.md](./PROMPTS.md) — шаблоны промтов

---

**Версия:** 1.0  
**Совместимость:** Unity 2021.3+, Claude Code, UnityMCP 8.x
