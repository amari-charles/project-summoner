# Input Layer

Captures player intent and converts it into Commands. Doesn't validate, doesn't execute — just packages what the player wants to do and calls `IGameSession.SubmitCommand()`.

## Overview

The Input layer has two responsibilities:

1. **Gesture capture → Command production** (`InputCollector`) — watches for player gestures (card drag-and-drop, spell targeting, unit redirect) and produces the matching Command. One gesture, one Command, hand it off.

2. **Gesture feedback visuals** (`SummonPreview`, `SpellPreview`, `SpawnZoneOverlay`, `RedirectIndicator`) — renders visual feedback for in-progress gestures. These components are visual, but they live in Input because their lifecycles are gesture-driven, not sim-driven.

### Why gesture feedback lives in Input

View components render simulation state — their lifecycles are managed by EntityManager, which diffs MatchState to spawn/destroy shells. Gesture feedback renders *input state* (drag position, targeting state) — data that doesn't exist in MatchState. EntityManager can't manage "drag in progress" because that concept isn't in MatchState. InputCollector must own gesture feedback lifecycles, so they live in Input. Gesture feedback CAN read `session.GetState()` for card data (Input already depends on Session).

See [documentation-guide.md principle #10](../../migration/documentation-guide.md) for the general rule.

For the full design, see [target-architecture.md &sect;5](../../target-architecture.md#5-input).

## Key Types

| Type | Role |
|------|------|
| `InputCollector` | Gesture capture → Command production |
| `SummonPreview` | Card drag → ghost unit feedback |
| `SpellPreview` | Spell targeting → circle/arrow feedback |
| `SpawnZoneOverlay` | Card drag → valid zone highlight |
| `RedirectIndicator` | Redirect gesture → circle/arrow feedback |
| `PlayCardCommand` | Card dragged to battlefield |
| `CastSpellCommand` | Spell targeting confirmed |

## Component Docs

- [Gesture Feedback](gesture-feedback/README.md) — SummonPreview and future gesture visuals

## Boundaries

Input knows nothing about View. It only talks to `IGameSession`.

## Decomposition Specs

Detailed migration plans for how each current file's Input responsibilities consolidate into InputCollector:
[../view/design-specs.md](../view/design-specs.md)

Covers: HandUI drag gesture, SpellTargetingManager state machine, RedirectManager gesture handling, BattlefieldDropZone drop logic, Summoner command production.

## Today

Input is scattered across:
- `HandUI` — handles the drag
- `BattlefieldDropZone` — handles the drop
- `Summoner.play_card_3d()` — builds the Command
- `SimulationNode.QueuePlayCard()` — submits it
- `SpellTargetingManager` — spell targeting

The target consolidates Command-production into `InputCollector`.

## Stub

```csharp
// scripts/csharp/Input/InputCollector.cs
public partial class InputCollector : Node
{
    private IGameSession? _session;

    public void Initialize(IGameSession session)

    // Gesture → Command translation
    public void OnCardDropped(int cardIndex, Vector3 position)
    public void OnSpellTargetConfirmed(int cardIndex, Vector3 position, int? targetUnitId)
    public void OnForfeitRequested()
}
```

Method bodies throw `NotImplementedException`. Each method produces the appropriate Command (`PlayCardCommand` or `ForfeitCommand`) and submits it via `_session.SubmitCommand()`.
