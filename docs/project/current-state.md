# Fateforged — Current State

**Last Updated:** 2026-08-24
**Version:** Pre-Alpha

Fateforged is a Godot 4.5 1v1 real-time tactical battler. Players summon
elemental creatures, cast spells, and deploy structures on a deterministic 3D
battlefield.

## Runtime Architecture

Battle runtime follows the four-layer stack documented under
`docs/architecture/gameplay/`:

`Simulation → Session → View → Input`

Meta progression is organized by responsibility:

| Boundary | Responsibility |
|---|---|
| `Quests` | Acceptance, ordered objectives, curriculum capacity, Journal state, professors, completion rewards |
| `Encounters` | Reusable preparation, fixed/owned/flexible loadouts, authored battle configuration, completion summaries |
| `ProgressionAuthority` | Direct authored-battle attempts, XP, first-clear rewards, idempotent completion |
| `ProfileRepo` | Versioned profile persistence, including per-summoner `summoner_progress` |
| `RewardService` | Universal reward resolution and claims |
| `Economy` | Account resources: gold, gems, essence, and fragments |
| `Shop` | Campus Shop offerings and purchases |

The walkable Academy campus is the meta-game home. Professors offer quests;
quest steps may open reusable encounters; completion returns through the shared
Results screen. The Merriweathers own the Campus Shop.

There is no map graph, run-scoped currency, academic enrollment browser, or
parallel activity-node flow. Debug battles are direct authored battles selected
from the Debug Arena catalog.

## Persistence

`ProfileData` version 8 stores quest and authored-battle state in
`summoner_progress`. Earlier progression schemas are unsupported in development
and start with empty summoner progress; no compatibility adapter is retained.

## Validation

```bash
dotnet build --no-restore
godot --headless --path . --editor --quit
dotnet test --settings test.runsettings
```

See [Quest System](../design/quest-system.md),
[System Architecture](../architecture/system-architecture.md), and
[Direction Log](direction-log.md).
