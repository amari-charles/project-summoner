# Known Bugs

This document tracks known bugs and issues in Project Summoner.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

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

*Last Updated: 2025-11-24 - Moved resolved bugs to bugs-resolved.md*
