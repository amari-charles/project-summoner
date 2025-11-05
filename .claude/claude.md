# Claude Development Notes

## Project Guidelines

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
