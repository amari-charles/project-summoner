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

This keeps documentation in sync with code changes and prevents stale task lists.

### PR Reviews
When asked to review a PR, follow the guidelines in `docs/workflows/pr-review-guidelines.md`. Key points:
- Check for AI-typical issues (meta comments, suspicious fallbacks, magic numbers)
- Enforce repo conventions and structure
- Flag incomplete updates (tests, types, docs)
- Use the structured output format from the guidelines doc

