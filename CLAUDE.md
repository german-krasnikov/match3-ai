# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Match3 game project. Early development stage.

- **Unity Version**: 2022.3+ (LTS)
- **Render Pipeline**: URP (Universal Render Pipeline)
- **Platform**: 2D mobile game

## Build & Run

```bash
# Open in Unity Hub or directly
open Match3.sln

# Build from command line (macOS)
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -buildTarget iOS -quit

# Run tests
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testResults ./TestResults.xml
```

## Architecture Guidelines

Follow **Unity Way** composition approach (see `AI/UNITY_COMPOSITION_GUIDE.md`):

### Core Principles
- **Composition over inheritance** - small reusable MonoBehaviour components
- **One component = one responsibility** - keep files <200 lines
- **SerializeField for dependencies** - explicit wiring in Inspector, not GetComponent at runtime
- **Event-driven communication** - components talk via C# events
- **Interfaces for contracts** - `IDamageTaker`, `IMoveable`, etc.

### Component Pattern
```csharp
public class LifeComponent : MonoBehaviour, IDamageTaker
{
    public event Action OnTakeDamage;
    public event Action OnEmpty;

    [SerializeField] private int _maxPoints;
    [SerializeField] private int _hitPoints;

    public bool TakeDamage(int damage) { /* ... */ }
}
```

### What NOT to do
- God objects with 500+ lines
- Deep inheritance hierarchies
- GetComponent in Update/Start
- Direct method calls between unrelated components

## Dependencies

- **DOTween** - Animation/tweening library (`Assets/Plugins/DOTween/`)

## Project Structure

```
Assets/
├── Scripts/       # Game code (to be created)
├── Scenes/        # Unity scenes
├── Settings/      # URP and project settings
└── Plugins/       # Third-party (DOTween)
```

## Code Style

- Use `_camelCase` for private fields with SerializeField
- Use events (`Action`, `Action<T>`) for component communication
- Prefer `TryGetComponent` over `GetComponent` for null safety
- не документируй код