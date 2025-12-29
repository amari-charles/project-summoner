# Claude Development Notes

## Project Guidelines

### Development Philosophy: Foundation First, Content Later

**CRITICAL PRINCIPLE: Do NOT add polished artwork or extensive content at this stage.**

**Placeholder Content vs Polished Content:**
- **ALLOWED**: Simple placeholder content (basic shapes, test units) needed to build and test core systems
- **NOT ALLOWED**: Polished artwork, detailed sprites, extensive level/battle content

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

### Code Philosophy
- Prefer clean, single-path implementations
- Remove deprecated code immediately
- Don't hedge with "we can keep the old way too"
- **Avoid bandaid fixes**: Don't suppress warnings with `@warning_ignore` or similar. Fix the root cause instead:
  - Use `has_method()` checks for duck typing
  - Properly type variables when possible
  - Address the underlying type safety issue
- **Always use localization**: All user-facing text must use the `Loc.t()` pattern for internationalization:
  - Campaign events: `Loc.t("campaign.event.event_id.name")` and `Loc.t("campaign.event.event_id.description")`
  - Battle names/descriptions: `Loc.t("campaign.battle.battle_id.name")` and `Loc.t("campaign.battle.battle_id.description")`
  - Add corresponding entries to `localization/data/en.json`
  - Never hardcode user-facing strings in GDScript files

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

### Documentation Updates
**ALWAYS update relevant documentation before merging a PR.**

Before finalizing any PR, ensure all related docs are updated:
- `docs/todos.md` - Mark tasks as completed or update status
- `docs/todos-completed.md` - Move completed tasks to archive
- `docs/bugs.md` - Update bug status if fixed
- `docs/bugs-resolved.md` - Move resolved bugs to archive

**IMPORTANT: When you see items marked as "✅ Fixed" or "✅ Completed" in bugs.md or todos.md, you MUST move them to the corresponding resolved/completed archive file. Do not leave fixed/completed items in the active docs.**

This keeps documentation in sync with code changes and prevents stale task lists.

### PR Reviews
When asked to review a PR, follow the guidelines in `docs/workflows/pr-review-guidelines.md`. Key points:
- Check for AI-typical issues (meta comments, suspicious fallbacks, magic numbers)
- Enforce repo conventions and structure
- Flag incomplete updates (tests, types, docs)
- Use the structured output format from the guidelines doc
- **Keep bug/todo additions**: If a PR adds new items to `docs/bugs.md` or `docs/todos.md`, keep them even if unrelated to the PR's main feature. These are valuable captures of future work.

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

