# Claude Development Notes

## Project Guidelines

### Development Philosophy: Foundation First, Content Later

**CRITICAL PRINCIPLE: Do NOT add more content (battles, cards, levels) at this stage.**

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

**ONLY AFTER** achieving this strong baseline should we pour in content. Adding battles, cards, or levels now would be building on a weak foundation.

**Reject suggestions** for new battles, campaign levels, or content expansion until the foundation is solid.

### Backwards Compatibility
**NEVER worry about backwards compatibility.** When implementing new features or changes, prioritize the new approach and remove old code paths. Don't keep fallback mechanisms or dual implementations.

Example: When implementing drag-and-drop for cards, remove click-to-play entirely rather than keeping both systems.

### Code Philosophy
- Prefer clean, single-path implementations
- Remove deprecated code immediately
- Don't hedge with "we can keep the old way too"

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

## Future Features / TODOs

### Campaign Level Editor (Dev-Only Tool)
A UI tool for developers to design and configure campaign battles.

**Purpose:**
- Allow designers to create/edit campaign battles without touching code
- Configure enemy decks, AI behavior, rewards, difficulty
- Test battles directly from the editor

**Design Approach:**
- **Access**: Dev-only tool (not accessible to players)
- **Location**: Separate scene, maybe accessible from main menu in debug builds or via dev console
- **Features**:
  - Drag-and-drop cards to build enemy deck
  - Set deck size (no player limits for enemies)
  - Configure AI behavior (aggression, card priority, play speed)
  - Set battle metadata (name, description, difficulty)
  - Define reward structure (fixed/choice/random cards)
  - Set unlock requirements (which battles must be completed first)
  - Preview/test battle
- **Storage**: Save battle definitions to `campaign_service.gd` or separate JSON files

**Current Status**: Not started - hardcoded decks in `campaign_service.gd` work fine for now

**Priority**: Low - Only needed when managing 20+ battles becomes cumbersome
