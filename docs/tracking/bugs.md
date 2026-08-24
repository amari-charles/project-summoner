# Known Bugs

This document tracks known bugs and issues in Fateforged.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.
**Tracker Sync (2026-03-05):** Reviewed against `bugs-resolved.md`; moved Puff target-switch and Wisp multi-target bugs to resolved based on post-refactor validation.
**Audit Sync (2026-03-05, evening):** Re-opened Puff pivot/flip bug after repro confirmation; migrated to metadata-driven pivot alignment and moved to resolved after validation. Blocked-idle and headless leak issues remain active pending explicit repro closure.
**Tracker Sync (2026-03-08):** Blocked-idle issue moved to verification after movement pipeline + blocked-nav reset fixes and deterministic repro coverage landed; headless leak remains open/cosmetic.
**Tracker Sync (2026-03-08, late):** Added resolved biome/checkerboard regression caused by StringName coercion mismatch to `bugs-resolved.md` (PR `#290`).
**Tracker Sync (2026-03-08, final):** Closed blocked-idle bug after manual signoff; moved full entry to `bugs-resolved.md`.
**Tracker Sync (2026-03-12, quick-win wave):** Moved headless leak item to `bugs-resolved.md` after `JsonProfileStore` disposal fixes (`DirAccess`/`Json`) and validation runs with no `Leaked unsafe reference` / `ObjectDB instances leaked` shutdown signatures in the specified headless GUT command.
**Tracker Sync (2026-08-23, item tooling):** Moved the item debug grant/service interop bug to resolved after the complete retained item command contract was repaired and tested.

---

## Active Bugs

## Bug Report Template

```markdown
#### Bug Title
**Status:** Open/In Progress/Resolved
**Reported:** YYYY-MM-DD
**Component:** System/Feature

**Description:**
Brief description of the bug

**Expected Behavior:**
What should happen

**Current Behavior:**
What actually happens

**Impact:**
How this affects gameplay/experience

**Reproduction Steps:**
1. Step 1
2. Step 2
3. ...

**Proposed Solution:**
Potential fixes or approaches

**Related Files:**
- file1.gd
- file2.gd

**Notes:**
Additional context
```

---
*Last Updated: 2026-08-23 - Added broken item debug grant/service interop bug*
