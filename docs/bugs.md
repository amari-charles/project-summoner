# Known Bugs

This document tracks known bugs and issues in Project Summoner.

---

## Active Bugs

### 🔴 HIGH PRIORITY

#### Battle Rewards Re-Granted on Replay
**Status:** Open
**Reported:** 2025-01-06
**Component:** Campaign / Rewards System

**Description:**
When replaying a completed battle, the player receives the reward cards again even though they should only be granted once. This allows infinite card farming by replaying missions.

**Expected Behavior:**
- Rewards should only be granted the first time a battle is completed
- Replaying a completed battle should not grant additional cards
- Need system to mark battles/rewards as "repeatable" or "one-time only"

**Current Behavior:**
- Every battle completion grants rewards, regardless of whether the battle was previously completed
- Cards accumulate infinitely on replay

**Impact:**
- Breaks game economy/progression
- Players can farm infinite cards from early battles
- Undermines card collection and deck building balance

**Reproduction Steps:**
1. Complete a campaign battle (e.g., battle_00)
2. Receive reward card(s)
3. Return to campaign screen
4. Play the same battle again
5. Win the battle
6. Observe that reward cards are granted again

**Proposed Solution:**
- Add reward system that checks if battle rewards have been claimed
- Track `rewards_claimed: bool` or `rewards_claimed_at: timestamp` per battle in campaign progress
- Only call `grant_battle_reward()` if rewards haven't been claimed
- Alternatively: Add `repeatable: bool` flag to battle definitions for missions that should give rewards on replay

**Related Files:**
- `scripts/services/campaign_service.gd` - Battle reward logic
- `scripts/ui/reward_screen.gd` - Reward display and granting
- Campaign progress tracking in profile data

**Notes:**
- Tutorial battles should definitely be non-repeatable for rewards
- Consider if any battles should be repeatable (daily challenges, farming levels)
- Need to decide: hide completed battles, or allow replay without rewards?

---

## Resolved Bugs

_(Empty - no resolved bugs yet)_

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

*Last Updated: 2025-01-06*
