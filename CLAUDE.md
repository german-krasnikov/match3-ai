# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Match3 game project using Unity 2022.3+ LTS with Universal Render Pipeline (URP).

## Development Commands

```bash
# Open project in Unity (macOS)
open -a Unity Match3

# Run tests from command line
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -runTests -projectPath . -testResults ./test-results.xml -testPlatform EditMode

# Build player (example for macOS)
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -buildTarget StandaloneOSX -buildPath ./Build/Match3.app -quit
```

## Architecture: Unity Way (Composition over Inheritance)

This project follows a strict component-based architecture. See `AI/UNITY_COMPOSITION_GUIDE.md` for complete documentation.

### Core Principles

1. **One Component = One Responsibility**
   - Each `MonoBehaviour` does exactly one thing
   - Prefer small, focused components over "god objects"

2. **Composition via SerializeField**
   ```csharp
   // ✅ Correct: explicit dependencies in Inspector
   [SerializeField] private LifeComponent _lifeComponent;

   // ❌ Wrong: hidden runtime dependencies
   private void Start() => GetComponent<LifeComponent>();
   ```

3. **Event-Driven Communication**
   ```csharp
   public class SomeComponent : MonoBehaviour
   {
       public event Action OnSomething;

       public void DoSomething()
       {
           // logic
           OnSomething?.Invoke();
       }
   }
   ```

4. **Interface-Based Dependencies**
   ```csharp
   public interface IDamageTaker
   {
       bool TakeDamage(int damage);
   }

   // Use TryGetComponent with interfaces
   if (collision.gameObject.TryGetComponent(out IDamageTaker target))
       target.TakeDamage(_damage);
   ```

### Component Naming Convention

- `*Component` - base logic (LifeComponent, MoveComponent)
- `*Controller` - orchestrates multiple components
- `*Proxy` - delegates to another component (TakeDamageProxy)

### When to Use Inheritance (rare, <5% cases)

Only for Template Method pattern with 80%+ shared logic and 1-2 varying methods.

## Key Dependencies

- **DOTween** (`Assets/Plugins/DOTween/`) - animation tweening
- **URP** - Universal Render Pipeline for 2D rendering
- **TextMeshPro** - text rendering

## File Structure

```
Assets/
├── Plugins/DOTween/     # DOTween animation library
├── Scenes/              # Game scenes
└── Settings/            # URP and project settings

AI/
└── UNITY_COMPOSITION_GUIDE.md  # Architecture documentation (RU)
```
