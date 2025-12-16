# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022 Match3 game project using URP (Universal Render Pipeline).

## Build & Run

- Open in Unity Hub (2022.3.x LTS)
- Run tests: `Window > General > Test Runner` in Unity Editor
- Build: `File > Build Settings > Build`

## Architecture Guidelines

Follow Unity Way composition pattern (see `AI/UNITY_COMPOSITION_GUIDE.md`):

### Core Principles

1. **Composition over Inheritance** - Use small, reusable components instead of deep class hierarchies
2. **Single Responsibility** - One component = one job
3. **SerializeField Dependencies** - Link components via Inspector, not GetComponent() at runtime
4. **Event-Driven Communication** - Components communicate via C# events

### Component Structure

```csharp
// Good: Small focused component
public class LifeComponent : MonoBehaviour
{
    public event Action OnDeath;
    [SerializeField] private int _maxHealth;
}

// Bad: God object doing everything
public class Character : MonoBehaviour { /* 500 lines */ }
```

### Naming Conventions

- Components: `{Feature}Component.cs` (e.g., `LifeComponent`, `MoveComponent`)
- Use `_camelCase` for private serialized fields
- Use events with `On` prefix: `OnDeath`, `OnDamage`

## Key Dependencies

- DOTween - Animation library (`Assets/Plugins/DOTween`)
- URP 14.0 - Rendering pipeline
- 2D Feature Set - Sprites, tilemap, animation

## Project Structure

```
Assets/
├── Plugins/DOTween/   # Animation tweening
├── Scenes/            # Game scenes
└── Settings/          # URP settings
AI/
└── UNITY_COMPOSITION_GUIDE.md  # Architecture reference
```
