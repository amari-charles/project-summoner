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

#### Moving Summoner Can Push Enemy Units
**Status:** Open — behavior decision required before fixing
**Reported:** 2026-08-16
**Component:** Compact ruin / moving-summoner combat

**Description:**
Moving the player summoner into nearby combat units can displace those units. The
player report did not establish whether friendly units are also affected.

**Expected Behavior:**
Summoner movement should not unintentionally function as a way to reposition
creatures. The intended interaction when a moving summoner meets a creature—such
as blocking, overlap, avoidance, or another rule—has not yet been selected.

**Current Behavior:**
The simulation keeps units outside the opposing summoner's melee-protection
bubble. Moving the summoner moves that exclusion area, which can force an enemy
unit outward on a later movement tick. Code inspection indicates this mechanism
targets opposing units; friendly-unit behavior still needs direct reproduction.

**Impact:**
The player may manipulate enemy positioning by walking into units, undermining
combat movement and making the moving-summoner experiment difficult to judge.

**Reproduction Steps:**
1. Open the Compact Ruin experimental room with summoner movement enabled.
2. Summon creatures and allow enemy creatures to approach the player summoner.
3. Walk the summoner into or through a nearby creature.
4. Observe whether the creature is displaced as the summoner advances.
5. Repeat with a friendly creature to determine whether the report affects both teams.

**Proposed Solution:**
Deferred until the intended summoner-versus-creature interaction is chosen. Do
not assume that blocking summoner movement is correct merely because it removes
the displacement.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs`
- `scripts/csharp/Battle/Simulation/Combat/SummonerMeleeBubble.cs`
- `scripts/csharp/Battle/Simulation/Commands/MoveSummonerCommand.cs`

**Notes:**
This behavior is exposed by the moving-summoner prototype; stationary battles do
not let the player deliberately move the exclusion bubble through units.

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
*Last Updated: 2026-08-16 - Added moving-summoner unit displacement bug from the compact ruin prototype*
