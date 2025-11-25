# Known Bugs

This document tracks known bugs and issues in Project Summoner.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

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

### 🟡 MEDIUM PRIORITY

#### WAL (Write-Ahead Log) Uses Inconsistent Key Names
**Status:** Open
**Reported:** 2025-11-25
**Component:** Database / ProfileRepository
**Type:** Data Consistency Issue

**Description:**
The Write-Ahead Log in `json_profile_repository.gd` uses inconsistent key names for entries, which will break future WAL replay/sync functionality.

**Expected Behavior:**
- All WAL entries use consistent format: `{"action": "...", "params": {...}}`

**Current Behavior:**
- Some entries use `"action"` + `"params"` keys
- Other entries use `"op"` key (e.g., `{"op": "unlock_hero", "hero_id": hero_id}`)

**Impact:**
- WAL replay logic would need special handling for each format
- Inconsistency makes cloud sync harder to implement
- May cause bugs if WAL replay is implemented without accounting for both formats

**Related Files:**
- `scripts/data/json_profile_repository.gd:219-222` - uses "action"/"params"
- `scripts/data/json_profile_repository.gd:246` - uses "op"

**Proposed Solution:**
Standardize all WAL entries to use `{"action": "...", "params": {...}}` format.

---

#### UUID Generation Is Weak and Could Cause Collisions
**Status:** Open
**Reported:** 2025-11-25
**Component:** Database / ProfileRepository
**Type:** Data Integrity Risk

**Description:**
The `_generate_uuid()` function uses weak entropy sources that could produce duplicate IDs.

**Expected Behavior:**
- UUID collisions should be astronomically unlikely
- IDs should be unique across sessions

**Current Behavior:**
```gdscript
func _generate_uuid() -> String:
    var timestamp: int = Time.get_ticks_msec()  # Wraps after ~49 days
    var random: int = randi()  # Predictable without randomize()
    return "%x-%x" % [timestamp, random]
```

**Impact:**
- Two IDs generated in same millisecond with same randi seed = collision
- Could cause card/deck duplication bugs
- Not critical now but will be as player base grows

**Related Files:**
- `scripts/data/json_profile_repository.gd:989-993`

**Proposed Solution:**
Add more entropy: use `Time.get_unix_time_from_system()`, `Time.get_ticks_usec()`, and multiple `randi()` calls.

---

#### Backup Rotation Happens After Write Success
**Status:** Open
**Reported:** 2025-11-25
**Component:** Database / ProfileRepository
**Type:** Data Safety Issue

**Description:**
Backup files are rotated after the main write succeeds, meaning a crash between write and rotation loses a backup generation.

**Expected Behavior:**
- Backups should be rotated BEFORE the new write
- Pattern: rotate bak1→bak2, copy main→bak1, THEN write new main

**Current Behavior:**
```gdscript
if _atomic_write(_data, temp_path, main_path):
    _rotate_backups(...)  # Crash here = lost backup
```

**Impact:**
- Low probability but could reduce recovery options after crash
- Players could lose more progress than necessary

**Related Files:**
- `scripts/data/json_profile_repository.gd:810-811`

**Proposed Solution:**
Reorder to: rotate backups first, then atomic write.

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

*Last Updated: 2025-11-25 - Added database bugs from comprehensive review*
