# Start Here

Fateforged is a Godot 4.5 project with C# domain/services and GDScript scene,
input, and presentation adapters.

## Read First

1. [Current State](project/current-state.md)
2. [Product Direction Log](project/direction-log.md)
3. [System Architecture](architecture/system-architecture.md)
4. [Quest System](design/quest-system.md)
5. [Gameplay Architecture](architecture/gameplay/README.md)

## Runtime Entry Points

- `project.godot` — autoload and project configuration
- `scripts/csharp/Meta/Services/Quests/` — quest state and progression
- `scripts/csharp/Meta/Services/Encounters/` — encounter preparation/execution
- `scripts/csharp/Meta/Services/Progression/` — direct authored-battle authority
- `scripts/csharp/Infrastructure/Persistence/` — profile persistence
- `scripts/application/` — scene-level orchestration and battle context
- `data/quests/` and `data/encounters/` — authored progression content
- `tests/` — GDScript and C# validation

## Validation

```bash
dotnet build --no-restore
godot --headless --path . --editor --quit
dotnet test --settings test.runsettings
./tools/run_tests.sh
```

Documents under `docs/archive/` describe superseded or suspended designs and are
not implementation guidance.
