# Known Bugs

This document tracks known bugs and issues in Project Summoner.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

### 🔴 HIGH PRIORITY

#### Cards Cannot Be Played in Campaign Battles
**Status:** ✅ Fixed (2025-11-25)
**Reported:** 2025-11-25
**Component:** Cards / Battle System
**Type:** Critical Bug

**Root Cause:**
`BattlefieldDropZone._can_drop_data()` was checking `summoner.get("is_alive")`, but a previous refactor renamed this property to `is_enabled` in Summoner3D. Since the property didn't exist, `get()` returned null, which defaulted to `false`, blocking all drops.

**Fix:**
Changed `is_alive` to `is_enabled` in `battlefield_drop_zone.gd:116-118`.

**Note:** Move to bugs-resolved.md after verifying fix works.

---

#### Mission Rewards Auto-Accepted Without Player Choice
**Status:** Open
**Reported:** 2025-11-25
**Component:** Campaign / Rewards
**Type:** UX Bug

**Description:**
If a mission finishes and the player doesn't explicitly accept rewards (e.g., closes the game, crashes, or navigates away), the rewards may be auto-accepted. This is problematic for reward screens that require player choice (e.g., "pick 1 of 3 cards").

**Expected Behavior:**
- Rewards requiring choice should NOT be auto-accepted
- Player must explicitly make their selection
- If player leaves without choosing, reward should be pending on next session
- Or: prevent leaving the reward screen until choice is made

**Current Behavior:**
- Unclear what happens if player exits during reward selection
- May auto-grant rewards without player input
- Could grant wrong reward or first option by default

**Impact:**
- Player may miss out on preferred reward choice
- Frustrating experience if "wrong" reward is auto-selected
- Could break progression if reward is required for next mission

**Proposed Solution:**
- Track reward state: PENDING_CHOICE vs CLAIMED
- On mission complete, mark reward as PENDING_CHOICE
- Only mark CLAIMED after explicit player action
- On game load, check for PENDING_CHOICE rewards and show selection screen
- Consider: block navigation away from reward screen until choice is made

**Related Files:**
- `scenes/ui/reward_screen.tscn`
- `scripts/ui/reward_screen.gd` (if exists)
- `scripts/services/campaign_service.gd` - Progress tracking

**Notes:**
- Need to audit current reward flow to understand exact behavior
- Consider edge cases: app crash, force quit, alt+F4
- May need persistent "pending rewards" queue in save data

---

### 🟡 MEDIUM PRIORITY

#### Mana Bar Uses Hardcoded Values Instead of Hero System
**Status:** Open (Deferred)
**Reported:** 2025-01-14
**Component:** UI / Mana System
**Type:** Architecture Issue

**Description:**
The mana bar currently has hardcoded default values in the scene file and uses Summoner as the mana source. When the Hero system is implemented, mana should be a Hero property, not Summoner.

**Expected Behavior:**
- Mana bar should display values from Hero.mana and Hero.max_mana
- Hero should emit mana_changed signal
- No hardcoded mana values in scene files
- Mana max should be determined by Hero stats/equipment

**Current Behavior:**
- Mana is managed by Summoner class
- MANA_MAX is a constant (15.0) in Summoner
- Scene file has hardcoded "Mana: 15/15" text
- No hero system implemented yet

**Impact:**
- Creates technical debt for future Hero implementation
- Mana system needs refactoring when Hero is added
- Not critical for current functionality

**Proposed Solution:**
- Create Hero system with mana as a property
- Move mana management from Summoner to Hero
- Update ManaBar to listen to Hero.mana_changed signal
- Remove hardcoded values from mana_bar.tscn

**Related Files:**
- `scripts/ui/mana_bar.gd` - Has TODO comments
- `scripts/core/summoner_3d.gd:29` - MANA_MAX constant
- `scenes/ui/mana_bar.tscn` - Hardcoded display values

**Notes:**
- Can be deferred until Hero system implementation
- TODOs added to relevant files
- Part of larger Hero system feature work

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

*Last Updated: 2025-11-25 - Added card playing bug (investigating)*
