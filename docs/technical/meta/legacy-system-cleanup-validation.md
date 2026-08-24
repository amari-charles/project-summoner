# Retired Progression Cleanup Validation

**Updated:** 2026-08-24

## Canonical Boundaries

| Capability | Owner |
|---|---|
| Quest acceptance, capacity, Journal, professors, ordered objectives, completion rewards | `QuestService` / `QuestApi` |
| Reusable preparation, loadout rules, battle configuration, completion summaries | `EncounterService` / `EncounterApi` |
| Direct authored debug battles, XP, first-clear rewards, durable attempt completion | `ProgressionAuthorityService` |
| Per-summoner quest and authored-battle persistence | `SummonerProgress` through `ProfileRepository` |
| Campus purchases | `ShopService`, using account resources; the Merriweathers own the Campus Shop |

## Removed

- graph catalogs, nodes, edges, route choices, unlock policy, and graph progress;
- the mixed progression service and its GDScript adapter/autoload;
- academic enrollment/activity catalogs and persistence;
- run-scoped gold and its reward/economy contracts;
- shared progression blobs and pre-quest compatibility adapters;
- dead event context/screen routing;
- indirect Debug Arena launch through a graph owner;
- obsolete background shader and traveling-merchant icon assets;
- tests that asserted removed behavior.

Pre-version-8 development saves receive empty `summoner_progress`. No reader or
translation layer preserves the retired progression schema.

## Documentation

The accepted transition is recorded in `docs/project/direction-log.md`. Wholly
superseded guidance is isolated under
`docs/archive/suspended-progression-models-2026-08/`; active architecture,
current-state, quest, shop, and summoner documents describe the retained model.
Superseded runtime implementation plans are isolated under
`docs/archive/superseded-runtime-architecture-2026-08/`.

## Required Validation

```bash
dotnet build --no-restore
godot --headless --path . --editor --quit
dotnet test --settings test.runsettings
./tools/run_tests.sh
```

The structural scan must find no retired product vocabulary in active runtime
code or content. Historical records and the explicitly suspended archive are
excluded from that assertion.

## Latest Validation

The 2026-08-24 final audit passed all phases of `./tools/run_tests.sh`:

- GDScript type/parse check: passed;
- .NET: 1,156 passed, 0 failed;
- GUT: 277 passed, 0 failed (2,588 assertions).

The runtime terminology scan found campaign/course references only in negative
regression assertions that verify their former files remain absent. No Caravan,
CampaignService, EventContext, campaign map, or course runtime remains.
