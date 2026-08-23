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

---

## Active Bugs

#### Item Debug Grant Command Calls a Missing Runtime Method
**Status:** Open
**Reported:** 2026-08-23
**Component:** Developer Tools / Item Service Interop

**Description:**
The developer-console item grant flow reaches `ItemsApi.grant_item()`, but the
adapter dynamically calls an item-service method that Godot does not expose
under the expected callable contract.

**Expected Behavior:**
Running `/items_grant <item_id>` grants a test item and returns its instance ID,
allowing the Inventory and equipment flows to be inspected with representative
data.

**Current Behavior:**
The command fails with:
`Invalid call. Nonexistent function 'GrantItem (via call)' in base 'Node (ItemService)'.`

**Impact:**
The Inventory prototype cannot be validated with item data through its intended
developer workflow. The same untested dynamic boundary is also used by the
item list, equip, unequip, and clear commands, so this should not be closed by
patching only the observed grant call.

**Reproduction Steps:**
1. Launch a debug build with an active summoner.
2. Open the developer console.
3. Run `/items_grant item_training_blade`.
4. Observe the invalid-call error instead of a granted item instance.

**Proposed Solution:**
Complete the linked item developer-tooling contract audit. Inventory every item
debug operation, verify the actual Godot-exposed signatures, and then either
repair the adapter as a tested boundary or replace the obsolete command path.

**Related Files:**
- `scripts/debug/dev_console.gd`
- `scripts/infrastructure/services/items_api.gd`
- `scripts/csharp/Meta/Services/Items/ItemService.cs`

**Notes:**
Keep this separate from the planned account-wide-to-summoner item ownership
migration. That migration changes product/runtime ownership; this bug concerns
broken developer tooling and an unverified interop contract.

---

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
