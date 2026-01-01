# Step 7: Destroy System

## Goal

Remove matched gems from data and animate their destruction with cascade effect.

---

## Dependencies

| Step | Component | Usage |
|------|-----------|-------|
| 2 | `GemView` | Animate scale, get Transform |
| 3 | `BoardData.RemoveGem(pos)` | Remove gem from data (fires `OnGemRemoved`) |
| 3 | `BoardView.GetView(pos)` | Get GemView for animation |
| 3 | `BoardView.DestroyGem(pos)` | Destroy GameObject after animation |
| 6 | `MatchData.Positions` | List of positions to destroy |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      GameController                          │
│                 (orchestrates the flow)                      │
└─────────────────────────────────────────────────────────────┘
                    │                    │
        calls       │                    │ calls
                    v                    v
         ┌──────────────────┐   ┌──────────────────┐
         │  DestroyAnimator │   │  DestroySystem   │
         │  (MonoBehaviour) │   │   (C# class)     │
         │                  │   │                  │
         │ AnimateDestroy() │   │ DestroyGems()    │
         │ OnDestroyComplete│   │ OnGemsDestroyed  │
         └──────────────────┘   └──────────────────┘
                │                        │
                │ animates               │ modifies
                v                        v
         ┌──────────────┐        ┌──────────────┐
         │   GemView    │        │  BoardData   │
         │  (scale=0)   │        │ RemoveGem()  │
         └──────────────┘        └──────────────┘
```

### Flow (controlled by GameController)

```
1. MatchSystem.FindAllMatches() -> List<MatchData>
2. Collect all unique positions from matches
3. Get GemViews for all positions via BoardView.GetView()
4. DestroyAnimator.AnimateDestroy(views, cascadeDelay)
5. Wait for OnDestroyComplete
6. DestroySystem.DestroyGems(board, positions)
   -> Calls BoardData.RemoveGem() for each
   -> Fires OnGemsDestroyed event (for scoring)
7. BoardView auto-destroys GameObjects via OnGemRemoved listener
```

**Key insight:** Animation happens BEFORE data removal. This allows:
- Views still exist during animation
- BoardView's OnGemRemoved handler cleans up after animation
- Clean separation of visual and data concerns

---

## Files

```
Assets/Scripts/Destroy/
    DestroySystem.cs      # Pure C# class, data logic
    DestroyAnimator.cs    # MonoBehaviour, DOTween animations
```

---

## Component 1: DestroySystem

**File:** `Assets/Scripts/Destroy/DestroySystem.cs`

**Type:** Plain C# class (stateless)

**Responsibility:** Remove gems from BoardData, fire event for scoring.

### Code

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;

namespace Match3.Destroy
{
    /// <summary>
    /// Handles gem removal from board data.
    /// Does NOT handle animation — that's DestroyAnimator's job.
    /// </summary>
    public class DestroySystem
    {
        /// <summary>
        /// Fires after gems are removed from data.
        /// Use for scoring, combo counting, etc.
        /// </summary>
        public event Action<List<Vector2Int>> OnGemsDestroyed;

        /// <summary>
        /// Removes gems at given positions from board.
        /// Fires OnGemsDestroyed after all removed.
        /// </summary>
        /// <param name="board">Board data to modify</param>
        /// <param name="positions">Positions to clear</param>
        public void DestroyGems(BoardData board, List<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0)
                return;

            // Remove each gem from data
            // BoardData.RemoveGem fires OnGemRemoved -> BoardView.DestroyGem
            foreach (var pos in positions)
            {
                board.RemoveGem(pos);
            }

            // Fire event for scoring system
            OnGemsDestroyed?.Invoke(positions);
        }

        /// <summary>
        /// Extracts unique positions from list of matches.
        /// Handles overlapping positions (L/T-shapes).
        /// </summary>
        public List<Vector2Int> GetUniquePositions(List<MatchData> matches)
        {
            var unique = new HashSet<Vector2Int>();

            foreach (var match in matches)
            {
                foreach (var pos in match.Positions)
                {
                    unique.Add(pos);
                }
            }

            return new List<Vector2Int>(unique);
        }
    }
}
```

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| OnGemsDestroyed | `event Action<List<Vector2Int>>` | Fires after removal (for scoring) |
| DestroyGems | `void DestroyGems(BoardData board, List<Vector2Int> positions)` | Remove gems from data |
| GetUniquePositions | `List<Vector2Int> GetUniquePositions(List<MatchData> matches)` | Dedupe positions |

### Notes

- Stateless class, no MonoBehaviour needed
- Does NOT call BoardView.DestroyGem directly — BoardData.OnGemRemoved handles that
- OnGemsDestroyed provides hook for future ScoreSystem

---

## Component 2: DestroyAnimator

**File:** `Assets/Scripts/Destroy/DestroyAnimator.cs`

**Type:** MonoBehaviour (for DOTween sequence management)

**Responsibility:** Animate gem destruction with scale-to-zero cascade effect.

### Code

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Gem;

namespace Match3.Destroy
{
    /// <summary>
    /// Animates gem destruction using DOTween.
    /// Scale to zero with cascade delay between gems.
    /// </summary>
    public class DestroyAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _scaleDuration = 0.2f;
        [SerializeField] private float _cascadeDelay = 0.05f;
        [SerializeField] private Ease _scaleEase = Ease.InBack;

        /// <summary>
        /// Fires when all destruction animations complete.
        /// </summary>
        public event Action OnDestroyComplete;

        /// <summary>
        /// Duration of single gem destruction animation.
        /// </summary>
        public float ScaleDuration => _scaleDuration;

        /// <summary>
        /// Delay between each gem's animation start.
        /// </summary>
        public float CascadeDelay => _cascadeDelay;

        /// <summary>
        /// Animates destruction of gems with cascade effect.
        /// </summary>
        /// <param name="gems">List of GemViews to animate</param>
        /// <returns>Sequence tween (can be used for chaining)</returns>
        public Tween AnimateDestroy(List<GemView> gems)
        {
            return AnimateDestroy(gems, _cascadeDelay);
        }

        /// <summary>
        /// Animates destruction of gems with custom cascade delay.
        /// </summary>
        /// <param name="gems">List of GemViews to animate</param>
        /// <param name="cascadeDelay">Delay between each gem's animation</param>
        /// <returns>Sequence tween (can be used for chaining)</returns>
        public Tween AnimateDestroy(List<GemView> gems, float cascadeDelay)
        {
            if (gems == null || gems.Count == 0)
            {
                OnDestroyComplete?.Invoke();
                return null;
            }

            var sequence = DOTween.Sequence();

            for (int i = 0; i < gems.Count; i++)
            {
                var gem = gems[i];
                if (gem == null) continue;

                float delay = i * cascadeDelay;

                // Scale to zero with delay
                sequence.Insert(
                    delay,
                    gem.transform
                        .DOScale(Vector3.zero, _scaleDuration)
                        .SetEase(_scaleEase)
                );
            }

            sequence.OnComplete(() => OnDestroyComplete?.Invoke());

            return sequence;
        }

        /// <summary>
        /// Calculates total animation duration for given gem count.
        /// </summary>
        public float GetTotalDuration(int gemCount)
        {
            if (gemCount <= 0) return 0f;
            return _scaleDuration + (gemCount - 1) * _cascadeDelay;
        }

        /// <summary>
        /// Calculates total animation duration with custom cascade delay.
        /// </summary>
        public float GetTotalDuration(int gemCount, float cascadeDelay)
        {
            if (gemCount <= 0) return 0f;
            return _scaleDuration + (gemCount - 1) * cascadeDelay;
        }
    }
}
```

### Inspector Settings

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| _scaleDuration | float | 0.2f | Time to scale from 1 to 0 |
| _cascadeDelay | float | 0.05f | Delay between each gem start |
| _scaleEase | Ease | InBack | DOTween easing (InBack gives "pop" feel) |

### Public API

| Member | Signature | Description |
|--------|-----------|-------------|
| OnDestroyComplete | `event Action` | Fires when all animations done |
| ScaleDuration | `float` | Get animation duration |
| CascadeDelay | `float` | Get default cascade delay |
| AnimateDestroy | `Tween AnimateDestroy(List<GemView> gems)` | Animate with default delay |
| AnimateDestroy | `Tween AnimateDestroy(List<GemView> gems, float cascadeDelay)` | Animate with custom delay |
| GetTotalDuration | `float GetTotalDuration(int gemCount)` | Calculate total time |

### Animation Details

```
Timeline example (5 gems, delay=0.05s, duration=0.2s):

Gem 0: [====] 0.0s - 0.2s
Gem 1:  [====] 0.05s - 0.25s
Gem 2:   [====] 0.1s - 0.3s
Gem 3:    [====] 0.15s - 0.35s
Gem 4:     [====] 0.2s - 0.4s
                              ^ OnDestroyComplete

Total: 0.2s + (4 * 0.05s) = 0.4s
```

### Notes

- Uses `DOTween.Sequence()` with `Insert()` for parallel animations with offsets
- `Ease.InBack` gives satisfying "shrink and pop" effect
- Null checks for safety (gem might be destroyed externally)
- Returns Tween for optional chaining by caller

---

## Missing Import: MatchData

DestroySystem uses MatchData but needs proper import. Add:

```csharp
// At top of DestroySystem.cs
using Match3.Match;
```

**Note:** Ensure MatchData is accessible. If in different assembly, may need assembly reference.

---

## Integration with GameController (Step 8 preview)

```csharp
// GameController.cs - Destroy state handling

private DestroySystem _destroySystem;
private DestroyAnimator _destroyAnimator;
private BoardView _boardView;
private MatchSystem _matchSystem;

private void HandleMatchingState()
{
    var matches = _matchSystem.FindAllMatches(_boardView.Data);

    if (matches.Count == 0)
    {
        // No matches - swap back or go to Idle
        SetState(GameState.Idle);
        return;
    }

    // Get unique positions and views
    var positions = _destroySystem.GetUniquePositions(matches);
    var views = GetViews(positions);

    // Animate first, then destroy data
    _destroyAnimator.AnimateDestroy(views);
    SetState(GameState.Destroying);
}

private void OnDestroyAnimationComplete()
{
    // Now remove from data (BoardView auto-destroys GameObjects)
    var positions = _currentDestroyPositions; // saved from matching state
    _destroySystem.DestroyGems(_boardView.Data, positions);

    // Proceed to falling
    SetState(GameState.Falling);
}

private List<GemView> GetViews(List<Vector2Int> positions)
{
    var views = new List<GemView>(positions.Count);
    foreach (var pos in positions)
    {
        var view = _boardView.GetView(pos);
        if (view != null)
            views.Add(view);
    }
    return views;
}
```

---

## Integration with Scoring (Future)

```csharp
// Example ScoreSystem integration
public class ScoreSystem
{
    private int _score;
    private int _comboMultiplier = 1;

    public void Subscribe(DestroySystem destroySystem)
    {
        destroySystem.OnGemsDestroyed += HandleGemsDestroyed;
    }

    private void HandleGemsDestroyed(List<Vector2Int> positions)
    {
        int points = positions.Count * 10 * _comboMultiplier;
        _score += points;
        _comboMultiplier++;

        Debug.Log($"Score: +{points} (total: {_score})");
    }
}
```

---

## Namespace

```csharp
namespace Match3.Destroy
```

---

## Scene Setup

### Hierarchy

```
Scene
└── Board
    ├── BoardView (existing)
    └── DestroyAnimator (new component)
```

### Inspector Setup

1. Add `DestroyAnimator` component to Board GameObject (or child)
2. Configure settings:
   - Scale Duration: 0.2
   - Cascade Delay: 0.05
   - Scale Ease: InBack

---

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         DESTROY FLOW                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  GameController (Matching State)                                      │
│       │                                                               │
│       ├──► MatchSystem.FindAllMatches(boardData)                     │
│       │         │                                                     │
│       │         └──► Returns List<MatchData>                         │
│       │                                                               │
│       ├──► DestroySystem.GetUniquePositions(matches)                 │
│       │         │                                                     │
│       │         └──► Returns List<Vector2Int> (deduplicated)         │
│       │                                                               │
│       ├──► BoardView.GetView(pos) for each position                  │
│       │         │                                                     │
│       │         └──► Returns List<GemView>                           │
│       │                                                               │
│       └──► DestroyAnimator.AnimateDestroy(views)                     │
│                 │                                                     │
│                 ├──► DOTween Sequence                                │
│                 │         │                                           │
│                 │         └──► gem.transform.DOScale(0, 0.2s)        │
│                 │              with cascade delay 0.05s               │
│                 │                                                     │
│                 └──► OnDestroyComplete event                         │
│                                                                       │
│  GameController (Destroying State complete)                           │
│       │                                                               │
│       └──► DestroySystem.DestroyGems(boardData, positions)           │
│                 │                                                     │
│                 ├──► boardData.RemoveGem(pos) for each               │
│                 │         │                                           │
│                 │         └──► BoardData.OnGemRemoved event          │
│                 │                     │                               │
│                 │                     └──► BoardView.HandleGemRemoved│
│                 │                               │                     │
│                 │                               └──► DestroyGem(pos) │
│                 │                                     │               │
│                 │                                     └──► Destroy() │
│                 │                                                     │
│                 └──► OnGemsDestroyed event                           │
│                           │                                           │
│                           └──► ScoreSystem (future)                  │
│                                                                       │
│  GameController -> Falling State                                      │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Edge Cases

| Situation | Behavior |
|-----------|----------|
| Empty positions list | No-op, immediate OnDestroyComplete |
| Null gem in list | Skip that gem, continue others |
| Same position twice | HashSet in GetUniquePositions handles |
| Gem already destroyed | DOScale handles gracefully (null check) |
| Animation interrupted | Sequence killed, OnComplete may not fire |

---

## Testing Checklist

### DestroySystem Tests
- [ ] DestroyGems removes all positions from BoardData
- [ ] OnGemsDestroyed fires with correct positions list
- [ ] Empty list does nothing, no event
- [ ] GetUniquePositions deduplicates correctly
- [ ] L-shape match (5 positions, 2 overlapping) returns 5 unique

### DestroyAnimator Tests
- [ ] AnimateDestroy scales all gems to zero
- [ ] Cascade delay works (gems don't all start at once)
- [ ] OnDestroyComplete fires after last animation
- [ ] Empty list fires OnDestroyComplete immediately
- [ ] GetTotalDuration returns correct value

### Integration Tests
- [ ] Gems visually shrink before disappearing
- [ ] GameObjects are destroyed after animation
- [ ] No errors when destroying edge gems (0,0), (7,7)
- [ ] Multiple matches destroyed correctly
- [ ] L/T-shape matches handled (no double-destroy)

### Visual Tests
- [ ] Animation looks smooth
- [ ] Cascade effect is visible
- [ ] Ease.InBack gives satisfying "pop"
- [ ] No visual artifacts after destruction

---

## File Checklist

- [ ] Create `Assets/Scripts/Destroy/` folder
- [ ] Implement `DestroySystem.cs`
- [ ] Implement `DestroyAnimator.cs`
- [ ] Add `DestroyAnimator` component to scene
- [ ] Configure inspector settings (0.2s, 0.05s, InBack)
- [ ] Test with manual trigger (temporary test script)

---

## Temporary Test Script

```csharp
// TestDestroy.cs - DELETE AFTER TESTING
using System.Collections.Generic;
using UnityEngine;
using Match3.Board;
using Match3.Destroy;
using Match3.Gem;

public class TestDestroy : MonoBehaviour
{
    [SerializeField] private BoardView _boardView;
    [SerializeField] private DestroyAnimator _destroyAnimator;

    private DestroySystem _destroySystem = new DestroySystem();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DestroyRandomGems();
        }
    }

    private void DestroyRandomGems()
    {
        // Pick 5 random positions
        var positions = new List<Vector2Int>();
        for (int i = 0; i < 5; i++)
        {
            positions.Add(new Vector2Int(i, 0));
        }

        // Get views
        var views = new List<GemView>();
        foreach (var pos in positions)
        {
            var view = _boardView.GetView(pos);
            if (view != null) views.Add(view);
        }

        // Animate then destroy
        _destroyAnimator.OnDestroyComplete += () =>
        {
            _destroySystem.DestroyGems(_boardView.Data, positions);
            Debug.Log("Destroy complete!");
        };

        _destroyAnimator.AnimateDestroy(views);
    }
}
```

---

## Notes

- DestroySystem is pure C#, testable without Unity
- DestroyAnimator is MonoBehaviour for DOTween lifecycle
- Animation before data removal is intentional
- BoardView.OnGemRemoved handles GameObject cleanup
- OnGemsDestroyed is stub for scoring (Step 8+)
- Cascade delay creates satisfying "chain reaction" feel
