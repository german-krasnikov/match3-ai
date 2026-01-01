# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Match3 game project. Unity 2022.3.62f2 LTS with URP (Universal Render Pipeline) and 2D feature set. DOTween plugin included for animations.

## Architecture

This project follows **Unity Way** composition patterns documented in `AI/UNITY_COMPOSITION_GUIDE.md`:

- **Composition over inheritance** — combine small focused components instead of deep class hierarchies
- **One component = one responsibility** — each MonoBehaviour does one thing
- **Event-driven communication** — components communicate via C# `event Action` not direct method calls
- **SerializeField for dependencies** — wire components in Inspector, avoid runtime GetComponent

### Code Standards

- `_camelCase` for private fields
- Files under 200 lines
- Depend on interfaces (e.g., `IDamageTaker`), not concrete types
- Subscribe in OnEnable, unsubscribe in OnDisable

## Development Workflow

### Agent-Based Development

Two specialized agents work together:

1. **unity-architect** (Opus) — designs systems, creates plans
   - Creates `AI/plan.md` for new features (module breakdown, stubs, dependencies)
   - Creates `AI/step_{N}.md` for detailed implementation specs
   - Never writes implementation code

2. **unity-programmer** (Sonnet) — implements exactly what's planned
   - Reads step files and implements per spec
   - Follows UNITY_COMPOSITION_GUIDE.md patterns
   - Reports blockers if plan is incomplete

### Workflow
1. New feature → unity-architect creates global plan in `AI/plan.md`
2. Pick a step → unity-architect details it in `AI/step_{N}.md`
3. Implementation → unity-programmer implements step file exactly

## Project Structure

```
Assets/
  Plugins/DOTween/   # Animation library
  Scenes/            # Game scenes
  Settings/          # URP settings
AI/
  UNITY_COMPOSITION_GUIDE.md  # Architecture patterns (required reading)
  plan.md                     # Global feature plan (when exists)
  step_{N}.md                 # Detailed step specs (when exists)
```

## Build Commands

```bash
# Open in Unity (macOS)
open -a Unity Match3.sln

# Build via Unity CLI
/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -buildTarget StandaloneOSX -quit
```

## Key Packages

- URP 14.0.12 — rendering
- 2D Feature Set — sprites, tilemaps, animation
- DOTween — programmatic animations
- TextMeshPro — UI text
