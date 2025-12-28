# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D Match-3 puzzle game using Universal Render Pipeline (URP).

- **Unity Version**: 2022.3.x LTS (template: com.unity.template.universal-2d@2.1.3)
- **Render Pipeline**: URP 14.0.12
- **Main Scene**: `Assets/Scenes/SampleScene.unity`

## Key Dependencies

- **DOTween**: Animation library (`Assets/Plugins/DOTween/`)
- **TextMeshPro**: UI text rendering
- **2D Feature Set**: Sprites, Tilemaps, Animation, SpriteShape, PSD Importer

## Build Commands

```bash
# Open project in Unity (macOS)
open -a Unity /Users/german/Work/Unity/MY/Match3

# Build from command line (example)
/Applications/Unity/Hub/Editor/2022.3.*/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -buildTarget StandaloneOSX -quit
```

## Architecture Guidelines

This project follows **Unity Way** - composition over inheritance. See `AI/UNITY_COMPOSITION_GUIDE.md` for full details.

### Core Principles

1. **One component = one responsibility**
   ```csharp
   // Good: Separate components
   public class LifeComponent : MonoBehaviour { }
   public class MoveComponent : MonoBehaviour { }

   // Bad: God object
   public class Character : MonoBehaviour { /* 500 lines */ }
   ```

2. **Compose via SerializeField, not GetComponent**
   ```csharp
   [SerializeField] private LifeComponent _life;  // Good
   GetComponent<LifeComponent>();                  // Avoid
   ```

3. **Event-driven communication**
   ```csharp
   public event Action OnDamage;
   OnDamage?.Invoke();
   ```

4. **Depend on interfaces**
   ```csharp
   public interface IDamageTaker { bool TakeDamage(int damage); }
   if (collision.gameObject.TryGetComponent(out IDamageTaker target))
       target.TakeDamage(_damage);
   ```

### Naming Conventions

- Components: `*Component` suffix (`LifeComponent`, `MoveComponent`)
- Interfaces: `I*` prefix (`IDamageTaker`, `IInteractable`)
- Private fields: `_camelCase`
- Events: `On*` prefix (`OnDamage`, `OnEmpty`)

### When to Use Inheritance

Only for Template Method pattern (5% of cases):
```csharp
public abstract class BasePatrolComponent : MonoBehaviour
{
    protected abstract Vector3[] InitPoints();
}
```

## DOTween Usage

```csharp
using DG.Tweening;

transform.DOMove(target, duration);
transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack);
sequence.Append(transform.DOMove(...)).Join(transform.DORotate(...));
```

## Project Structure

```
Assets/
├── Plugins/DOTween/    # Animation library
├── Scenes/             # Game scenes
└── Settings/           # URP settings, render assets
```
