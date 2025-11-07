# Known Bugs

This document tracks known bugs and issues in Project Summoner.

---

## Active Bugs

### 🟡 MEDIUM PRIORITY

#### AI Scoring Magic Numbers Should Be Constants
**Status:** Open
**Reported:** 2025-01-06
**Component:** AI System
**Type:** Code Quality Enhancement

**Description:**
The HeuristicAI class uses many hardcoded magic numbers for card scoring and decision-making thresholds. These should be extracted to class-level constants for easier tuning and balancing.

**Expected Behavior:**
- Scoring values defined as named constants at class level
- Easy to adjust AI difficulty by tweaking a few values
- Clear documentation of what each value controls

**Current Behavior:**
- Magic numbers scattered throughout scoring functions (10.0, 15.0, 20.0, etc.)
- Difficult to tune AI behavior without searching through code
- Not immediately clear what each number represents

**Impact:**
- Low gameplay impact - AI still functions correctly
- Makes AI balancing more difficult for developers
- Harder to maintain and understand AI logic

**Proposed Solution:**
Extract to constants like:
```gdscript
const SCORE_MANA_EFFICIENCY: float = 10.0
const SCORE_SUMMON_BASE: float = 15.0
const SCORE_AGGRESSIVE_BONUS: float = 5.0
```

**Related Files:**
- `scripts/ai/heuristic_ai.gd` - Lines with scoring logic

**Notes:**
- Not urgent - can be done in future PR
- Would make AI easier to balance and tune
- Consider creating AI configuration files for different difficulty levels

---

## Resolved Bugs

### ✅ Battle Rewards Re-Granted on Replay
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Campaign / Rewards System

**Description:**
When replaying a completed battle, the player received reward cards again.

**Solution Implemented:**
- Added `is_replay` detection in `reward_screen.gd`
- Only grants rewards if battle not already completed
- Shows "Battle Already Completed" message on replay
- Uses `campaign.is_battle_completed()` check

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Enemy AI Not Spawning in Campaign Battles
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** AI / Campaign System

**Description:**
Enemy summoner was not playing cards during campaign battles, making them impossible to lose.

**Solution Implemented:**
- Fixed autoload name mismatch (CampaignService vs Campaign)
- Fixed AIController type signature to accept both Summoner and Summoner3D
- Added dynamic AI loading in GameController3D
- AI now properly instantiated from campaign config

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Cards Reference 2D Units Instead of 3D
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Cards / Units

**Description:**
Several card resources (archer, warrior, wall, training_dummy) referenced 2D unit scenes, breaking 3D battles.

**Solution Implemented:**
- Created 3D versions of all missing units
- Updated card resources to reference new 3D scenes
- All cards now work in 2.5D battlefield

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Debug Print Statements in Production Code
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Code Quality

**Description:**
Multiple files contained debug print statements that should not be in production.

**Solution Implemented:**
- Removed all debug prints from scripted_ai.gd
- Removed all debug prints from game_controller_3d.gd
- Removed debug helper function `_get_hand_names()`
- Kept only push_warning/push_error for actual issues

**Fixed In:** PR #fix/campaign-battle-cards

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

*Last Updated: 2025-01-06 - PR #fix/campaign-battle-cards ready for merge*
