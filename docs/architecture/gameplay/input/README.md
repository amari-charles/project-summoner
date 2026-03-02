# Input Layer

Captures player intent and converts it into Commands. Doesn't validate, doesn't execute — just packages what the player wants to do and calls `IGameSession.SubmitCommand()`.

## Overview

`InputCollector` watches for player gestures — card drag-and-drop, spell targeting, unit redirect — and produces the matching Command. One gesture, one Command, hand it off.

For the full design, see [target-architecture.md &sect;5](../../target-architecture.md#5-input).

## Key Types

| Type | Role |
|------|------|
| `InputCollector` | Gesture capture -> Command production |
| `PlayCardCommand` | Card dragged to battlefield |
| `CastSpellCommand` | Spell targeting confirmed |

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
