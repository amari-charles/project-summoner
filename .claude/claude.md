# Claude Development Notes

## Project Guidelines

### Development Philosophy: Foundation First, Content Later

**CRITICAL PRINCIPLE: Do NOT add polished artwork or extensive content at this stage.**

**Placeholder Content vs Polished Content:**
- **ALLOWED**: Simple placeholder content (basic shapes, test units) needed to build and test core systems
- **NOT ALLOWED**: Polished artwork, detailed sprites, extensive level/battle content

**Placeholder Naming Convention:**
When creating placeholder content (items, cards, UI elements, etc.), **always name them to be obviously temporary**:
- **DO**: "PLACEHOLDER EMOTE 1", "TEST CARD - Fire", "TEMP THEME - Debug"
- **DON'T**: "Laugh", "Crimson Theme", "Golden Card Back" (sounds like real content)

This prevents confusion on future reviews about what is intentional vs temporary. Plausible-sounding placeholder names risk being shipped as final content or causing wasted effort reviewing "content" that was never meant to be real.

The current priority is building an exceptional foundation:

1. **Core Game Mechanics** - Ensure all fundamental systems work flawlessly
   - Unit behavior, combat, movement, AI
   - Card playing, mana, deck management
   - Win/loss conditions, progression systems

2. **Visual Appeal & Polish** - Make existing mechanics look and feel amazing
   - VFX for abilities and combat
   - UI/UX refinements and juice
   - Animation quality and game feel
   - Camera work, transitions, feedback

3. **Quality Baseline** - Establish a strong standard of excellence
   - Every existing feature should be polished
   - Players should feel "this is a quality game"
   - Foundation systems should be robust and extensible

**ONLY AFTER** achieving this strong baseline should we create polished artwork and pour in extensive content. Adding detailed artwork now would be premature when core mechanics still need refinement.

**Reject suggestions** for polished artwork, extensive battle content, or detailed level design until the foundation is solid. Simple placeholders for testing are always acceptable.

### Backwards Compatibility
**NEVER worry about backwards compatibility.** When implementing new features or changes, prioritize the new approach and remove old code paths. Don't keep fallback mechanisms or dual implementations.

Example: When implementing drag-and-drop for cards, remove click-to-play entirely rather than keeping both systems.

### Test Philosophy
**Treat tests as the source of truth.** Do not modify, weaken, delete, or bypass tests unless the task explicitly says to change the tests.

- **Prefer fixing product logic, not test logic.** Assume test failures indicate a bug or missing behavior in the implementation.
- **Do not introduce "fake green" fixes**, such as:
  - Swallowing exceptions / broad try-catch just to pass
  - Adding sleeps, randomness, or timing hacks
  - Changing configuration to disable validations
  - Stubbing/mocking real functionality in production code
- **Do not change public contracts** unless explicitly requested. Preserve API shape, semantics, and backwards compatibility.
- **Keep behavior correct, not just passing.** If test behavior feels wrong or ambiguous, explain the concern instead of hacking around it.
- **Prefer minimal, targeted changes** that clearly satisfy the intent of the system and the test.
- **Run and satisfy the full suite**, not just the failing test in isolation.
- **Maintain quality**: no regressions, no reduction in safety checks, type weakness, or silent failures.
- If a test appears incorrect or incomplete, state why and propose a fix, but do not change it without instruction.

### Running Tests

```bash
# Headless (fast, skips Godot-runtime tests):
dotnet test --settings test.runsettings

# Full suite including Godot-runtime tests:
# Run via Godot editor's gdUnit4 panel
```

Test suites that create `Godot.Collections.Dictionary`, `Godot.Collections.Array`, or load catalogs containing `Resource` subclasses (e.g., `CardConfig`) crash the headless test host. These suites have `[TestSuite]` commented out with a note to run via the editor.

### Persistence Philosophy
**NEVER give up on a task without explicit permission.** When something doesn't work:
1. Debug and investigate the root cause
2. Try alternative approaches
3. Research if needed
4. Only ask the user if you want to abandon the current approach

Do NOT just revert to a previous working state when encountering errors. Fix the problem.

### Code Philosophy
- Prefer clean, single-path implementations
- Remove deprecated code immediately
- Don't hedge with "we can keep the old way too"
- **Prefer interfaces over flags**: When different types need different behavior, create an interface with type-specific implementations rather than adding boolean flags to a shared object.

### Configurability Over Flags

**AVOID:** Adding boolean flags to differentiate behavior between types.

```gdscript
# BAD: Accumulates flags, creates complex conditionals
var event_data = {
    "requires_deck": true,
    "show_rewards": true,
    "has_preview": false,
    "enable_deck_edit": false,
    # ...flags multiply as features grow
}

# Then scattered throughout the code:
if event_data.requires_deck:
    deck_column.visible = true
if event_data.show_rewards:
    rewards_panel.visible = true
```

This pattern leads to:
- Flag proliferation as features grow
- Complex conditional logic scattered throughout code
- Difficulty understanding what combination of flags each type uses
- Bugs when flags interact unexpectedly

**PREFER:** Interfaces with type-specific implementations.

```gdscript
# GOOD: Each type defines its own behavior
class_name NodeDetailModal extends Control

func _get_sections() -> Array[Control]:
    push_error("Subclass must implement _get_sections()")
    return []

# ---

class_name BattleNodeModal extends NodeDetailModal

func _get_sections() -> Array[Control]:
    return [_create_info_section(), _create_deck_section(), _create_rewards_section()]

# ---

class_name CaravanNodeModal extends NodeDetailModal

func _get_sections() -> Array[Control]:
    return [_create_info_section(), _create_shop_preview_section()]
```

This pattern provides:
- Clear, explicit behavior per type
- No flag combinations to reason about
- Easy to add new types without touching existing code
- Each implementation is self-contained and testable
- **Avoid bandaid fixes**: Don't suppress warnings with `@warning_ignore` or similar. Fix the root cause instead:
  - Use `has_method()` checks for duck typing
  - Properly type variables when possible
  - Address the underlying type safety issue
- **Always use localization**: All user-facing text must use the `Loc.t()` pattern for internationalization:
  - Campaign events: `Loc.t("campaign.event.event_id.name")` and `Loc.t("campaign.event.event_id.description")`
  - Battle names/descriptions: `Loc.t("campaign.battle.battle_id.name")` and `Loc.t("campaign.battle.battle_id.description")`
  - Add corresponding entries to `localization/data/en.json`
  - Never hardcode user-facing strings in GDScript files

### GDScript/C# Enum Interop

When GDScript needs C# enum values, use the **Mirror Enum Pattern**:

1. Define a matching enum in `scripts/infrastructure/data/unit_constants.gd`
2. Add a comment noting the C# source file it must match
3. Cast to `int()` when passing to C# methods

**Examples:**
- `UnitConstants.Team` mirrors `scripts/csharp/Infrastructure/Data/Units/Enums.cs`
- `UnitConstants.GameState` mirrors `scripts/csharp/Battle/View/BattleScene.cs`

**Never hardcode C# enum int values directly** (e.g., `const CATEGORY: int = 2`). Always use the mirror enum for type safety and maintainability.

### GDScript/C# Method Interop

**Nullable default parameters aren't exposed to GDScript.** C# methods with nullable defaults like:

```csharp
public FloatingHPBar? create_bar_for_unit(Node3D unit, Dictionary? settings = null)
```

Appear to GDScript as requiring both parameters (the `default_args` array is empty in the method binding). **Always pass `null` explicitly** when calling such methods from GDScript:

```gdscript
# Wrong - will fail with "Nonexistent function"
hp_service.create_bar_for_unit(unit)

# Correct - pass null explicitly
hp_service.create_bar_for_unit(unit, null)
```

This is a Godot limitation with C# nullable reference types.

### When to Use C# vs GDScript

**Use C# for:**
- **Core game systems** - Combat, targeting, projectiles, units, stats
- **Performance-critical code** - Anything running in `_PhysicsProcess` for many objects
- **Interfaces and capabilities** - `IDamageable`, `IRangedAttacker`, etc.
- **Typed domain objects** - Data structures with fixed schemas (configs, DTOs)
- **Services** - Game services that manage state or coordinate systems
- **Complex algorithms** - Pathfinding, spatial queries, damage calculations

**Use GDScript for:**
- **UI components and screens** - Menus, HUD, dialogs, panels
- **Scene scripts** - Autoloads, controllers, scene-specific logic
- **High-level orchestration** - Game flow, state machines, event handling
- **Editor tooling** - `@tool` scripts, custom inspectors

**Migration direction:** When refactoring flag-based GDScript configs into typed structures, prefer migrating to C#. This gives type safety, better IDE support, and aligns with the codebase direction.

**Example:** Event/battle configuration dictionaries should become C# classes:
```csharp
// scripts/csharp/Infrastructure/Data/BattleEventConfig.cs
public class BattleEventConfig
{
    public string BiomeId { get; set; }
    public int Difficulty { get; set; }
    public bool IsTutorial { get; set; }
    public List<string> EnemyDeck { get; set; }
}
```

### Exporting the Game

**Prerequisites:**
- Use `Godot_mono.app` (NOT `Godot.app`) - this is a C# project
- Windows export templates must be installed (via Godot Editor > Editor > Manage Export Templates)

**Command-line export (headless):**
```bash
# Build C# first
dotnet build

# Export Windows release
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --export-release "Windows Desktop"
```

**Output files:**
- `Fateforged.exe` - Main game executable
- `Fateforged.console.exe` - Console wrapper (for debug output)
- `data_Fateforged_windows_x86_64/` - .NET runtime and dependencies

**Distribution:**
To share the build, zip the exe files AND the data folder together:
```bash
zip -r Fateforged-windows.zip Fateforged.exe Fateforged.console.exe data_Fateforged_windows_x86_64/
```

All three components are required for the game to run on Windows.

### Git Workflow
**ALWAYS use feature branches and PRs for non-trivial changes.**

Process:
1. Create feature branch for the work
2. Make commits on the branch
3. Push branch and create PR
4. **WAIT for user approval** - do NOT merge
5. User will review, test, and approve
6. Only merge after explicit user approval

Exceptions (can commit directly to main):
- Trivial changes (typos, minor tweaks)
- Quick fixes explicitly approved by user
- Changes user says "can go straight to main"

**Never merge PRs without user approval.**

**PRs can fix multiple issues.** Don't artificially separate changes into multiple PRs. If there are uncommitted changes in the working directory, include them in the PR - don't leave work uncommitted just because it seems like a "different issue." A single PR with multiple fixes is better than leaving work uncommitted.

### Documentation Updates
**ALWAYS update relevant documentation before merging a PR.**

Before finalizing any PR, ensure all related docs are updated:
- `docs/todos.md` - Mark tasks as completed or update status
- `docs/todos-completed.md` - Move completed tasks to archive
- `docs/bugs.md` - Update bug status if fixed
- `docs/bugs-resolved.md` - Move resolved bugs to archive
- `docs/technical/*.md` - Update technical docs when modifying complex systems

**IMPORTANT: When you see items marked as "✅ Fixed" or "✅ Completed" in bugs.md or todos.md, you MUST move them to the corresponding resolved/completed archive file. Do not leave fixed/completed items in the active docs.**

**Technical Documentation**: When making significant changes to complex systems (projectiles, targeting, visual rendering, etc.), update or create docs in `docs/technical/`. These systems have subtle interactions that are easy to break. Key technical docs:
- `docs/technical/projectile-system.md` - Projectile movement, paths, acceleration
- `docs/technical/projectile-targeting.md` - Target position calculation, tracking, common pitfalls

This keeps documentation in sync with code changes and prevents stale task lists.

### PR Reviews
When asked to review a PR, follow the guidelines in `docs/workflows/pr-review-guidelines.md`. Key points:
- Check for AI-typical issues (meta comments, suspicious fallbacks, magic numbers)
- Enforce repo conventions and structure
- Flag incomplete updates (tests, types, docs)
- Use the structured output format from the guidelines doc
- **Keep bug/todo additions**: If a PR adds new items to `docs/bugs.md` or `docs/todos.md`, keep them even if unrelated to the PR's main feature. These are valuable captures of future work.

### PR Polish Policy
**Complete ALL major and minor fixes before merging a PR.** Do not defer polish work to future PRs.

When a PR review identifies issues:
- **Major issues**: Must be fixed before merge (blocking)
- **Minor issues**: Must also be fixed before merge (not blocking, but required)
- **Do NOT** leave TODOs, unclear comments, naming inconsistencies, or other polish items for "later"
- **Do NOT** create follow-up PRs for polish that should have been done in the original PR

The goal is that every merged PR represents complete, polished work. This prevents technical debt accumulation and ensures the codebase maintains a high quality bar at all times.

Exceptions:
- Intentional phased implementations where functionality is explicitly deferred (e.g., "Phase 2: Connect to service X")
- These should be clearly documented with the specific phase/task where they'll be addressed

### Completing Beneficial Review Items
**Complete ALL beneficial items identified in PR reviews**, including those marked as "optional" or "nice to have."

If an item would improve code quality, test coverage, or maintainability, it should be done as part of the PR - not deferred. The "optional" label means it's not blocking merge, not that it should be skipped.

Examples of items that should always be completed:
- Adding tests for new behavior (even if marked optional)
- Removing dead code or unused variables
- Fixing misleading comments or docstrings
- Small refactors that improve clarity

### Design Context Documentation
**Document decisions and context in every PR - not just code, but product and lore too.**

When making decisions, ensure context is captured across three dimensions:

1. **Architectural Context** - Technical decisions, patterns, system relationships
2. **Product Context** - Why features exist, user goals, game design intent
3. **Lore Context** - Story elements, world-building, character backgrounds

**Documentation practices:**
- Capture reasoning in relevant docs (e.g., `docs/features/`, `docs/lore/`)
- PRs should include context for non-obvious design choices
- Keep related docs consistent and updated together
- If docs are inconsistent and it's unclear which is correct, **ask the user**

**Key design contexts documented:**
- **Shop vs Caravan**: Caravan = in-campaign card purchases during gameplay events. Premium Store = meta-progression outside campaigns (summoners, cosmetics, emotes)
- **Currency**: Gold for gameplay progression, potentially gems for premium purchases (TBD)
- **Summoners**: Elemental characters with progression (levels, traits, boons) - each has lore and personality

### Skeletal Animation Principles

**Walk Cycle Limb Synchronization:**

When animating skeletal rigs with mirrored limbs (legs, arms), the math works as follows:

1. **Sprite offset direction determines rotation effect:**
   - Left limbs typically have positive X sprite offset from pivot
   - Right limbs typically have negative X sprite offset from pivot
   - Positive rotation moves a positive-X-offset sprite LEFT
   - Positive rotation moves a negative-X-offset sprite RIGHT

2. **For alternating leg motion:**
   - Both legs at SAME keyframe times with OPPOSITE rotation values
   - Example: Left leg `[+0.12, -0.12, +0.12]`, Right leg `[-0.12, +0.12, -0.12]`
   - This creates one leg forward while the other is back

3. **For proper cross-body arm motion:**
   - Arms must swing OPPOSITE to their same-side leg
   - If left leg is `[+A, -A, +A]` (back, forward, back), left arm should be `[-A, +A, -A]` (forward, back, forward)
   - If right leg is `[-A, +A, -A]` (forward, back, forward), right arm should be `[+A, -A, +A]` (back, forward, back)

4. **Attack animations must reset ALL animated properties:**
   - Include tracks for leg rotation (reset to 0.0)
   - Include tracks for leg position (reset to neutral)
   - Otherwise legs stay in whatever walk position they were in when attack started

