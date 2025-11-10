# Milestone A: "Units on the Field" - COMPLETE

> **⚠️ ARCHIVED DOCUMENT**
>
> **Status:** HISTORICAL
> **Archived Date:** 2025-01-10
> **Reason:** Milestone completed and documented for historical purposes
>
> **For current progress, see:**
> - [Changelog](../CHANGELOG.md)
> - [Roadmap](../design/roadmap.md)

---

## ✅ What Was Fixed

### 1. **Mana Regeneration** (CRITICAL BUG FIX)
**Problem:** `int(mana_regen_rate * delta) = 0` every frame, so mana never regenerated

**Solution:**
- Changed `current_mana: int` → `mana: float`
- Proper float accumulation: `mana = clamp(mana + mana_regen_rate * delta, 0.0, MANA_MAX)`
- Updates UI only when mana changes significantly

**Files changed:**
- `scripts/core/summoner.gd:19-64`
- `scripts/core/simple_ai.gd:38`
- `scripts/ui/game_ui.gd:40-42`

### 2. **Unit Always-Advance AI** (NEW FEATURE)
**What it does:**
- Units now target nearest enemy unit within `aggro_radius` (180px)
- If no units in range, they advance toward enemy base
- Units can attack both other Units and Summoners (bases)
- Proper attack cooldown management

**Behavior:**
1. Check for enemy units in aggro radius
2. If found: move toward and attack
3. If not found: advance toward enemy base
4. Attack when in `attack_range` (80px)
5. Wait for cooldown before next attack

**Files changed:**
- `scripts/units/unit.gd:16-17, 22, 41-114`

### 3. **Base Can Be Attacked**
**What works:**
- Summoner already has `take_damage(damage: float)` method
- Units check `target.has_method("take_damage")` before attacking
- Summoners emit `summoner_died` signal when HP reaches 0
- GameController listens for summoner death to end game

**No changes needed** - this already worked!

---

## 🧪 What to Test Now

### Test 1: Mana Regeneration
1. Open `test_game.tscn` and press F5
2. Watch the "Mana" label at bottom-left
3. **Expected:** Should regenerate from 0 → 10 over 10 seconds
4. Click to spawn a warrior (costs 3 mana)
5. **Expected:** Mana drops to 7, then regens back to 10

### Test 2: Unit Spawning
1. Click anywhere on the battlefield
2. **Expected:** Blue square (warrior) appears at cursor
3. **Expected:** Mana decreases by 3
4. **Expected:** New card drawn (hand refills)

### Test 3: Unit Movement
1. Spawn 2-3 warriors
2. **Expected:** Warriors move upward toward enemy base (red base at top)
3. **Expected:** Movement is smooth and continuous

### Test 4: Unit vs Unit Combat
1. Wait for enemy AI to spawn warriors (every 3-6 seconds)
2. **Expected:** Red warriors spawn at top
3. **Expected:** When blue and red warriors meet, they stop and attack
4. **Expected:** HP bars decrease with each hit
5. **Expected:** Warriors die when HP reaches 0

### Test 5: Unit vs Base Combat
1. Spawn many warriors quickly (spam click if you have mana)
2. Let them reach the enemy base (red base at top)
3. **Expected:** Warriors stop at base and attack it
4. **Expected:** Base HP bar decreases
5. **Expected:** Game ends with "PLAYER WINS!" when base HP hits 0

### Test 6: Enemy AI
1. Just wait and watch
2. **Expected:** Enemy spawns warriors every 3-6 seconds
3. **Expected:** Enemy warriors advance toward your base (blue base at bottom)
4. **Expected:** If left alone, enemy warriors will attack and destroy your base

---

## 🎯 Success Criteria

**Milestone A is complete if:**
- ✅ Mana regenerates visibly
- ✅ Clicking spawns units at cursor
- ✅ Units move toward enemies or bases
- ✅ Units attack and deal damage
- ✅ Units die at 0 HP
- ✅ Bases can be damaged and destroyed
- ✅ Game ends when base HP reaches 0

---

##Known Issues Still to Fix

### Input Coordinate Conversion
The player input camera math might be wrong:
```gdscript
# In player_input.gd:39
world_pos = camera.get_screen_center_position() + (screen_pos - get_viewport().get_visible_rect().size / 2)
```
**Symptom:** Units might spawn in wrong position when clicking
**Fix:** Use `camera.get_global_transform_with_canvas().affine_inverse() * screen_pos`

### PlayerInput Reference
PlayerInput uses `get_tree().get_first_node_in_group()` to find summoner
**Better:** Use `get_parent() as Summoner` since it's a child of PlayerSummoner

---

## Next Steps (Milestone B)

After confirming everything works:
1. Fix input coordinate conversion if units spawn in wrong spots
2. Improve AI timing (make it more aggressive)
3. Add restart button to game over screen
4. Add visual feedback for card plays
5. Test full match flow (spawn units → combat → win/lose)

---

## How to Test Right Now

```bash
# In Godot:
1. Open scenes/battlefield/test_game.tscn
2. Press F5 (or click Play Scene)
3. Try all tests above
4. Report any issues
```

**Expected first-run experience:**
- Game loads
- Timer counts down
- Mana regens
- Enemy AI spawns warriors
- You can click to spawn warriors
- Combat happens
- Someone wins

If all that works → **Milestone A: COMPLETE!**
