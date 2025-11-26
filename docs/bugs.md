# Known Bugs

This document tracks known bugs and issues in Project Summoner.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

### 🟡 MEDIUM PRIORITY

#### Exiting Battle Mid-Fight Incorrectly Completes Event
**Status:** Open
**Reported:** 2025-11-26
**Component:** Campaign / Battle System

**Description:**
When a player exits a battle in the middle of it (e.g., via pause menu or back button), the event/battle is incorrectly marked as completed.

**Expected Behavior:**
- Exiting mid-battle should NOT complete the event
- Player should be able to retry the battle
- Progress should only be saved on actual victory

**Current Behavior:**
- Exiting mid-battle marks the event as complete
- Player cannot replay the battle properly

**Impact:**
- Breaks campaign progression
- Players can accidentally skip content
- Corrupts save state

**Reproduction Steps:**
1. Start a campaign battle
2. Exit mid-battle (pause menu, back button, etc.)
3. Observe that the event is marked as completed

**Proposed Solution:**
- Only call `complete_battle()` on actual victory
- Ensure exit/quit paths don't trigger completion
- Add explicit "forfeit" vs "exit" distinction if needed

**Related Files:**
- `scripts/core/battle_context.gd`
- `scripts/ui/reward_screen.gd`
- `scripts/services/campaign_service.gd`

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

*Last Updated: 2025-11-26 - Fixed slime death state bug and mana bar hardcoded values*
