# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D Match3 game project using Universal Render Pipeline (URP).

## Build & Run

Open project in Unity Editor (Unity 2022.3+). No CLI build commands available - use Unity Editor.

## Architecture Guidelines

Follow "Unity Way" composition approach from `AI/UNITY_COMPOSITION_GUIDE.md`:

### Core Principles
- **Composition over inheritance** - combine small components instead of deep class hierarchies
- **One component = one responsibility** - each MonoBehaviour does one thing well
- **Event-driven communication** - components communicate via C# events, not direct method calls
- **SerializeField dependencies** - wire components in Inspector, avoid runtime GetComponent

### Component Pattern
```csharp
public class LifeComponent : MonoBehaviour, IDamageTaker
{
    public event Action OnTakeDamage;
    public event Action OnEmpty;

    [SerializeField] private int _maxPoints;
    private int _hitPoints;

    public bool TakeDamage(int damage) { /* ... */ }
}
```

### Avoid
- God objects with 500+ lines handling everything
- Deep inheritance hierarchies
- GetComponent lookups in Update/runtime when SerializeField works

## Key Packages

- **DOTween** (`Assets/Plugins/DOTween/`) - animation tweening
- **Universal RP** - 2D rendering pipeline

## Code Style

- Use `_camelCase` for private fields with SerializeField
- Keep files under 200 lines
- Depend on interfaces, not concrete implementations
