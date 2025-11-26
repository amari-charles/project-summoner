# Battle Enemy Spawning Patterns

This document describes the two different ways enemies are spawned in battles.

## Pattern 1: Pre-loaded Enemy Deck (Standard Battles)

Most battles use a pre-loaded enemy deck that's configured in `campaign_service.gd`.

**Battle Configuration:**
```gdscript
_battles["first_trial"] = {
    "id": "first_trial",
    "enemy_deck": [
        {"catalog_id": "slime_green", "count": 1}
    ],
    "enemy_hp": 30.0,
    // Optional win condition (defaults to DESTROY_BASE)
    "win_condition": WinConditionIDs.DESTROY_BASE,
    "time_limit": 60.0,  // Required for SURVIVE_TIME, TIMED_DESTROY
    "kill_target": 10,   // Required for KILL_COUNT
    // ... other config
}
```

**How it works:**
1. `EnemyDeckLoader.load_enemy_deck_for_battle()` reads `enemy_deck` from battle config
2. Creates Card resources for each entry
3. `Summoner3D` (enemy) loads these cards using `BATTLE_CONTEXT` strategy
4. Enemy plays cards from deck according to AI behavior

**When to use:**
- Standard battles where enemy has a deck and plays cards like the player
- Arena battles, endless mode, practice mode

---

## Pattern 2: Event Sequence Spawning (Tutorial/Scripted Battles)

Some battles (especially tutorials) use the event sequence system to spawn enemies at specific moments via dialogue/events.

**Battle Configuration:**
```gdscript
_battles["charge_tutorial"] = {
    "id": "charge_tutorial",
    "enemy_deck": [],  // IMPORTANT: Empty array, NOT omitted!
    "enemy_hp": 50.0,
    "event_sequence": "res://resources/sequences/charge_tutorial.tres",
    // ... other config
}
```

**How it works:**
1. `enemy_deck` is intentionally set to `[]` (empty array)
2. `event_sequence` points to an EventSequence resource
3. `Summoner3D` auto-detects this pattern and switches to `DEFERRED` deck loading strategy
4. `BattleDialogueController` or `EventSequencer` spawns enemies manually via actions
5. Example: `_spawn_tutorial_enemy()` creates and spawns units directly

**When to use:**
- Tutorial battles with scripted enemy spawns
- Story battles with timed enemy waves
- Any battle where enemy spawn timing needs to be controlled by dialogue/events

---

## Critical Implementation Details

### Auto-Detection in Summoner3D

The enemy summoner automatically detects event_sequence battles:

```gdscript
// summoner_3d.gd:48-58
if team == Unit3D.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
    if BattleContext.battle_config.has("event_sequence") and
       BattleContext.battle_config.has("enemy_deck"):
        var enemy_deck_array: Array = BattleContext.battle_config.get("enemy_deck")
        if enemy_deck_array.is_empty():
            // Switch to DEFERRED - no deck needed
            deck_load_strategy = DeckLoadStrategy.DEFERRED
```

### Requirements for Event Sequence Battles

**MUST do:**
- Include `"enemy_deck": []` (empty array) in battle config
- Include `"event_sequence": "path/to/sequence.tres"` in battle config
- Spawn enemies manually via event actions

**DO NOT:**
- Omit the `enemy_deck` key entirely
- Put cards in `enemy_deck` if using event_sequence
- Expect enemy to play cards from a deck

---

## Common Mistakes to Avoid

❌ **Wrong:** Omitting enemy_deck key
```gdscript
_battles["my_battle"] = {
    "event_sequence": "res://my_sequence.tres",
    // Missing enemy_deck!
}
```

❌ **Wrong:** Having both populated deck and event_sequence
```gdscript
_battles["my_battle"] = {
    "enemy_deck": [{"catalog_id": "slime", "count": 1}],  // Confusing!
    "event_sequence": "res://my_sequence.tres",
}
```

✅ **Correct:** Empty deck with event_sequence
```gdscript
_battles["my_battle"] = {
    "enemy_deck": [],  // Empty - enemies spawned via events
    "event_sequence": "res://my_sequence.tres",
}
```

---

## Related Files

- `scripts/services/campaign_service.gd` - Battle definitions
- `scripts/core/summoner_3d.gd` - Deck loading strategy auto-detection
- `scripts/core/enemy_deck_loader.gd` - Loads enemy decks from battle config
- `scripts/core/battle_dialogue_controller.gd` - Handles event_sequence playback
- `scripts/core/game_controller_3d.gd` - Win condition handling
- `scripts/data/win_condition_ids.gd` - Win condition type constants
