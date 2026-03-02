# AI Implementation Guide

> **Purpose**: How to execute the simulation rewrite given AI context limitations. Read this document at the start of every new session working on the rewrite.
>
> **Status**: Updated — 2026-02-26 (aligned with user-confirmed requirements)

---

## Session Handoff Protocol

### When Starting a New Session

Read these documents **in this order**:

1. **`docs/rewrite-research/architecture-decisions.md`** — The six invariants. Non-negotiable.
2. **`docs/rewrite-research/implementation-plan.md`** — Find the current phase (look for unchecked items).
3. **`docs/rewrite-research/requirements.md`** — Reference for gameplay features the sim must support.
4. **`docs/rewrite-research/problem-analysis.md`** — Only if you need context on why a decision was made.

### When Ending a Session

Before ending, update `implementation-plan.md`:
- Check off completed steps
- Note any issues encountered
- Add any new discoveries to the relevant doc
- Commit progress with a descriptive message

---

## No-Go Rules

These are things the AI must **NEVER** do during the rewrite. Violating any of these re-introduces the bugs we're fixing.

### Absolute Prohibitions

| # | Rule | Why |
|---|------|-----|
| 1 | **Never add MatchState writes outside simulation code during Battle phase** (`Simulation.Tick(fixedDelta)` + `SnapshotApplier.Apply()` on clients are the only allowed writers) | This is the root cause of all desyncs. The entire rewrite exists to close these paths. |
| 2 | **Never let the client run `Simulation.Tick()`** | Dual-authority causes immediate divergence. Client is a renderer, not a simulator. |
| 3 | **Never bypass the command queue for gameplay actions** | Direct writes (DeductMana, StartCasting) are what broke the current branch. |
| 4 | **Never have `Unit3D` or `summoner.gd` compute gameplay state during Battle** | They read from MatchState and react to events. They don't decide damage, targeting, or death. |
| 5 | **Never add Godot dependencies to simulation files** | `Simulation.cs`, `SimBehavior.cs`, etc. must remain pure C#. Godot types stay in the bridge/presentation layer. |
| 6 | **Never modify tests to make them pass** | Fix the product code. Tests are the source of truth (per CLAUDE.md). |
| 7 | **Never add `@warning_ignore` or broad try-catch** | Fix root causes, not symptoms. |
| 8 | **Never change the command interface without updating both host and client** | Commands are the contract between host and client. |

### Warning Signs

If you find yourself doing any of these, stop and reconsider:

- Adding a `SimulationNode.SyncXYZ()` method that writes to MatchState → Should be a command or handled inside `Tick()`
- Adding `if is_host:` guards in `Unit3D._PhysicsProcess()` → Unit3D shouldn't differ between host and client
- Creating a "temporary" direct write with a `# TODO: move to command` comment → Do it right now
- Adding snapshot correction for a value that should be deterministic → Fix the determinism, don't patch with corrections

---

## File Ownership Map

### Simulation-Owned (Pure C#, no Godot)

These files are mutated **only** by `Simulation.Tick(fixedDelta)` (and `SnapshotApplier.Apply()` on clients):

```
scripts/csharp/Simulation/
├── MatchState.cs           # The state (ONLY Tick + SnapshotApplier write to this)
├── UnitData.cs             # Per-unit state
├── SummonerData.cs         # Per-summoner state
├── GamePhase.cs            # Phase enum
├── Simulation.cs           # The Tick() function
├── SimBehavior.cs          # Unit behavior FSM
├── SimDamage.cs            # Damage calculation
├── SimMovement.cs          # Movement calculation
├── SimSteering.cs          # Separation/flanking
├── SimTargeting.cs         # Target acquisition
├── SimProjectile.cs        # Projectile simulation
├── SimEvent.cs             # Event types
├── DeterministicRng.cs     # Seeded RNG
├── Commands/
│   ├── ICommand.cs         # Command interface
│   ├── PlayCardCommand.cs  # Play card command
│   ├── ForfeitCommand.cs   # Forfeit command
│   └── CommandValidator.cs # Validation logic
└── CardDefinitions.cs      # Card data lookup (read-only)
```

**Rule**: No `using Godot;` in any of these files.

### Bridge-Owned (Connects sim to engine)

```
scripts/csharp/Simulation/
├── SimulationNode.cs       # Godot Node wrapping Simulation
│                           # Exposes READ-ONLY API to GDScript
│                           # Emits signals from SimEvents
│                           # Feeds commands into Tick()
```

**Rule**: `SimulationNode` has NO public methods that write to MatchState. It provides:
- Read methods: `GetUnitData()`, `GetSummonerHp()`, `GetHand()`, etc.
- Command submission: `SubmitCommand(ICommand cmd)` → adds to queue
- Signals: Godot signals emitted from SimEvents

### Multiplayer-Owned

```
scripts/csharp/Multiplayer/
├── Authority/
│   ├── HostRunner.cs       # Drives Tick() on host, broadcasts results
│   └── ...
├── Client/
│   ├── ClientRunner.cs     # Receives events+snapshots, applies to presentation
│   └── ...
├── Core/
│   ├── LocalPlayer.cs      # Network identity
│   └── ...
├── Sync/
│   ├── StateSnapshotBuilder.cs
│   ├── StateSnapshot.cs
│   └── MessageSerializer.cs
└── Transport/
    └── ...
```

**Rule**: `HostRunner` calls `Simulation.Tick()`. `ClientRunner` does NOT.

### Presentation-Owned (GDScript + Godot nodes)

```
scripts/units/unit_3d.gd (or Unit3D.cs)
scripts/core/summoner.gd
scripts/ui/...
scripts/battlefield/...
```

**Rule**: During Battle, these are READ-ONLY consumers. They react to signals and read from MatchState via `SimulationNode`'s read API.

---

## Phase Checklist Format

Each phase in `implementation-plan.md` follows this structure:

```markdown
## Phase N: [Name]

### Entry Criteria
- [ ] Phase N-1 completed and verified
- [ ] `dotnet build` passes
- [ ] Single-player battle works (if applicable)

### Changes
1. [Specific file change with description]
2. [Specific file change with description]

### Acceptance Criteria
- [ ] `dotnet build` passes with zero errors
- [ ] Single-player battle: start → play cards → units fight → win/loss
- [ ] [Phase-specific verification]

### Files Modified
- `path/to/file.cs` — [what changed]

### Invariants to Verify
- [ ] No new MatchState writes outside Tick() (grep check)
- [ ] Client doesn't call Tick() (grep check)
- [ ] [Phase-specific invariants]
```

---

## Invariant Verification

After **every phase**, run these checks:

### Build Check

```bash
dotnet build
```

Must pass with zero errors. Warnings are acceptable during transition but should be resolved before the phase is marked complete.

### Mutation Audit

Search for any MatchState writes outside `Simulation.Tick()`:

```bash
# In C# files, look for writes to State. fields outside of Simulation.cs
grep -rn "State\.\w* =" scripts/csharp/ --include="*.cs" | grep -v "Simulation.cs" | grep -v "// init" | grep -v "test"

# In GDScript, look for direct SimulationNode mutation calls
grep -rn "sim_node\.\(Sync\|Apply\|Set\|Force\|Deduct\|Start\|Increment\)" scripts/ --include="*.gd"
```

### Single-Player Smoke Test

After each phase (where applicable):
1. Launch single-player battle
2. Play 3+ cards
3. Watch units fight
4. Verify win/loss triggers
5. No crashes or error spam in console

### Multiplayer Test (Phase 4+)

After Phase 4 and beyond:
1. Launch host
2. Connect client
3. Both play cards
4. Verify units appear on both sides
5. Verify damage/death syncs
6. Verify win/loss on both sides
7. Check for hash mismatch warnings in console

---

## Debugging Tips

### "Units don't move/attack"
- Check that `Simulation.Tick()` is being called (add a print every 60 ticks)
- Check that units are registered in `MatchState.Units`
- Check that units have `Lifecycle == Active`
- Check that `SimBehavior.TickBehavior()` is processing the unit

### "Damage not applying"
- Check that `SimDamage` is being called (not the legacy `DamageSystem`)
- Check that units have valid targets (`TargetNetworkId != null`)
- Check that attack cooldown has expired

### "Cards don't work"
- Check that `PlayCardCommand` is being created and queued
- Check that `Tick()` is processing the command (add validation logging)
- Check that mana is sufficient and hand index is valid

### "Client doesn't see units"
- Check that host is broadcasting `UnitSpawned` events
- Check that `ClientRunner` is handling the events
- Check coordinate remapping (canonical → local)

### "Hash mismatches everywhere"
- Check for any remaining external MatchState writes (run mutation audit)
- Check that the client isn't running `Tick()`
- Check that init ordering is correct (summoners registered before first tick)

---

## Commit Conventions

Each phase gets its own commit(s) with clear messages:

```
feat(sim-rewrite): Phase 0 — host-only tick, disable client Tick()
feat(sim-rewrite): Phase 1 — command-based card play, close summoner mutations
feat(sim-rewrite): Phase 2 — read-only Unit3D, close presentation mutations
...
```

Use `feat(sim-rewrite):` prefix for all commits in this rewrite. This makes it easy to see all rewrite commits in git log.

---

*Last updated: 2026-02-26*
