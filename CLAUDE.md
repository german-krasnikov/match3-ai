# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D Match-3 game project using Universal Render Pipeline (URP 14.0.12). Currently in early development stage with DOTween integrated for animations.

## Build & Run

Open in Unity Editor (2022.3 LTS recommended based on URP version). No command-line build scripts configured.

## Architecture Guidelines

**Follow Unity Way - Composition over Inheritance:**

1. **One Component = One Responsibility**
   - Each MonoBehaviour does one thing well
   - Avoid "God Objects" with multiple concerns

2. **Wire dependencies via SerializeField, not GetComponent()**
```csharp
// Good
[SerializeField] private LifeComponent _life;

// Bad
private void Start() { var life = GetComponent<LifeComponent>(); }
```

3. **Event-Driven Communication**
   - Components publish events, others subscribe
   - Use `Action` events for decoupling

4. **Depend on Interfaces**
   - Use `TryGetComponent(out IInterface)` pattern
   - Define contracts like `IDamageTaker`

5. **Conditions System**
   - Use `AndCondition` pattern for composable logic
   - Add conditions via `AddCondition(Func<bool>)` to extend without modifying

## Key Patterns

**Component Examples:**
- `LifeComponent` - health management with OnTakeDamage/OnEmpty events
- `MoveComponent` - movement with condition support
- `JumpComponent` - jump physics with OnJump event
- `ReloadComponent` - cooldown management
- `DamageMakerComponent` - collision-based damage dealing

**When to use inheritance (rare):**
- Template Method pattern with 80%+ shared logic
- Only 1-2 methods differ between implementations
- Clear "is-a" relationship

## Project Structure

```
Assets/
  Scripts/       # Game code (currently empty, to be created)
  Plugins/       # DOTween animation library
  Scenes/        # Game scenes
  Settings/      # URP settings
```

## Dependencies

- DOTween - tweening animations
- Universal Render Pipeline 2D
- TextMeshPro

See `AI/UNITY_COMPOSITION_GUIDE.md` for detailed composition patterns and examples.
