# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Match3 game project using Universal 2D Render Pipeline template (Unity 2022.3+).

## Build & Run

```bash
# Open in Unity Editor (command line)
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -projectPath .

# Build from command line
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -buildTarget StandaloneOSX -executeMethod BuildScript.Build -quit
```

Tests run via Unity Editor: Window > General > Test Runner

## Architecture: Unity Way (Composition over Inheritance)

This project follows the **Unity Way** approach documented in `AI/UNITY_COMPOSITION_GUIDE.md`. Core principles:

### Component Design
- **One component = one responsibility** (SRP)
- Keep MonoBehaviours small (<200 lines)
- Components communicate via **events**, not direct method calls
- Use `[SerializeField]` for explicit dependencies in Inspector
- Prefer `TryGetComponent<T>()` over `GetComponent<T>()`

### Composition Pattern
```csharp
// Good: Small focused components
public class LifeComponent : MonoBehaviour, IDamageTaker
{
    public event Action OnTakeDamage;
    public event Action OnEmpty;
    [SerializeField] private int _hitPoints;

    public bool TakeDamage(int damage) { /* ... */ }
}

// Character combines components
public class Character : MonoBehaviour
{
    [SerializeField] private LifeComponent _life;
    [SerializeField] private MoveComponent _move;
}
```

### Interface-First Design
```csharp
// Define contracts
public interface IDamageTaker
{
    event Action OnTakeDamage;
    bool TakeDamage(int damage);
}

// Work with interfaces
if (collision.gameObject.TryGetComponent(out IDamageTaker target))
    target.TakeDamage(_damage);
```

### When to Use Inheritance (rare, ~5% cases)
- Template Method pattern with shared algorithm
- Clear "is-a" relationship
- Only 1-2 methods differ between implementations

## Code Style

- C# naming: `_privateField`, `PublicProperty`, `MethodName()`
- Events: `public event Action OnSomething;`
- Use `[SerializeField]` instead of public fields
- Subscribe/unsubscribe in `OnEnable()`/`OnDisable()`
- Null-conditional for events: `OnEvent?.Invoke();`

## Project Structure

```
Assets/
├── Scenes/          # Game scenes
├── Scripts/         # C# source (to be created)
│   ├── Components/  # Reusable MonoBehaviour components
│   ├── Interfaces/  # Contracts (IDamageTaker, etc.)
│   └── Core/        # Game-specific logic
├── Prefabs/         # Prefab assets
└── Settings/        # URP settings
```
