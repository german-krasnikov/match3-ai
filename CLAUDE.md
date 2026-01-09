# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Match3 game using **MVP architecture** (Model-View-Presenter) with TDD approach.

## Development Pipeline

### Automated Workflow (`@step N`)

1. **Детализация** — unity-architect создаёт `AI/step_{N}.md`
2. **Реализация** — unity-programmer создаёт код по спецификации
3. **Валидация** — unity-mcp проверяет компиляцию + тесты
4. **Настройка сцены** — пользователь запускает `Setup/Step N` в Unity

### Agents

| Agent | Model | Role |
|-------|-------|------|
| unity-architect | opus | Создаёт `step_{N}.md` со спецификацией |
| unity-programmer | sonnet | Реализует код по спецификации |
| unity-mcp | haiku | Валидация: компиляция + тесты |

## Commands

### Run Tests
```bash
# Через Unity MCP
run_tests(testMode: "EditMode")

# Проверка консоли
read_console
```

### Scene Setup
Пользователь запускает вручную: `Unity Menu → Setup → Step N`

## Architecture (MVP + Service Locator)

```
Model (Pure C#)     ← Бизнес-логика, тестируемый
    ↕ events
Presenter (Pure C#) ← Связь M↔V, тестируемый с mock
    ↕ interface
View (MonoBehaviour) ← UI, не тестируется
```

### What Gets Tested
- **Model** — unit-тесты без моков
- **Presenter** — unit-тесты с mock View (NSubstitute)
- **View** — НЕ тестируется

## Project Structure

```
Assets/
├── Scripts/
│   ├── Game.asmdef          # Runtime assembly
│   ├── Core/                # ServiceLocator, Services
│   ├── Common/              # Shared types
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── {Feature}Model.cs
│   │       ├── {Feature}Presenter.cs
│   │       ├── {Feature}View.cs
│   │       └── I{Feature}View.cs
│   ├── Configs/             # ScriptableObjects
│   └── Editor/
│       └── Editor.asmdef    # Editor scripts, Scene Setup
└── Tests/
    └── EditMode/
        └── Tests.EditMode.asmdef  # NUnit + NSubstitute

AI/
├── plan.md                  # Global plan (READ-ONLY after creation)
├── step_{N}.md              # Step specifications
├── PIPELINE_GUIDE.md        # Pipeline documentation
├── UNITY_MVP_ARCHITECTURE_GUIDE.md
├── UNITY_TDD_GUIDE.md
└── UNITY_MCP_GUIDE.md

.claude/agents/
├── unity-architect.md
├── unity-programmer.md
└── unity-mcp.md
```

## Assembly References

- **Game.asmdef** — runtime код
- **Editor.asmdef** — references Game, Editor-only
- **Tests.EditMode.asmdef** — references Game, includes NSubstitute

## Key Conventions

### Naming
| Element | Convention | Example |
|---------|------------|---------|
| Private field | `_camelCase` | `_playerHealth` |
| Event | `On` + `PascalCase` | `OnHealthChanged` |
| Interface | `I` + `PascalCase` | `IPlayerView` |
| Namespace | `Features.{Feature}` | `Features.Combat` |

### File Rules
- Max 200 lines per file
- One class per file
- File name = class name
- Tests use same namespace as code (access internal without `[InternalsVisibleTo]`)

### Test Naming
`Method_Condition_ExpectedResult`
```csharp
public void TakeDamage_WhenDead_DoesNothing() { }
```

## Critical Rules

1. **План — закон** — programmer реализует строго по `step_{N}.md`
2. **Простота превыше всего** — SOLID, DRY, KISS без переусложнений
3. **Model без Unity** — чистый C#, никаких MonoBehaviour
4. **Presenter реализует IDisposable** — отписка от событий
5. **View — "глупый"** — только отображение, никакой логики
6. **MCP НЕ настраивает сцену** — только компиляция + тесты

## Dependencies

- Unity 2021.3+
- NUnit
- NSubstitute
- DOTween (optional)
- UnityMCP 8.x+ (для валидации)
