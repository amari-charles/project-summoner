# Event-Driven Architecture - Technical Implementation Guide

**Status:** Phase 1 Foundation (In Progress)
**Last Updated:** 2025-01-20

## Table of Contents

1. [Overview](#overview)
2. [Technical Design Principles](#technical-design-principles)
3. [Current Problems](#current-problems)
4. [Architecture Components](#architecture-components)
5. [Phase 1: Foundation](#phase-1-foundation)
6. [Phase 2: Tutorial Migration](#phase-2-tutorial-migration)
7. [Phase 3: Game State Integration](#phase-3-game-state-integration)
8. [Phase 4-5: Future Phases](#phase-4-5-future-phases)
9. [Best Practices](#best-practices)
10. [Debugging Guide](#debugging-guide)

---

## Overview

We're migrating from a tightly-coupled, `get_tree().paused`-driven system to a **capability + event-driven architecture** that supports:

- **Story cutscenes** before, between, and during battles
- **Quest/mission tracking** with objective progress
- **Tutorial flows** with fine-grained control over game features
- **Dynamic gameplay modifications** (lock specific cards, trigger events based on conditions)
- **In-battle scripted events** (boss taunts, mid-battle tutorials, environmental changes)

### Core Philosophy

**Systems communicate via events and capabilities, not direct calls and global pause.**

### Goals

1. **Decouple systems** - Use events and capabilities instead of hard references
2. **Fine-grained control** - Replace global pause with specific capability blocking
3. **Scriptable content** - Define tutorials/cutscenes in `.tres` resources, not hardcoded logic
4. **Debuggable** - Answer "why can't I do X?" and "what events fired?" instantly
5. **Incremental migration** - No big-bang rewrite; old code coexists during transition

### Implementation Phases

- **Phase 1**: Core infrastructure - CapabilityManager, GameStateEvents, EventSequencer (4 step types)
- **Phase 2**: Migrate first_trial tutorial to event sequences
- **Phase 3**: Refactor GameController to use capabilities + events
- **Phase 4**: Quest system with event-driven objectives
- **Phase 5**: Advanced cutscenes (camera, animations, effects)

---

## Technical Design Principles

### 1. Event-Driven, Not Call-Driven

**Systems emit signals on specialized hubs and subscribe to those signals.** Direct calls are reserved for localized, immediate operations.

```gdscript
# ❌ Bad: Direct coupling
func _on_unit_died(unit: Unit3D) -> void:
    quest_system.check_kill_objective(unit)
    achievement_system.track_kill(unit)
    analytics.log_unit_death(unit)

# ✅ Good: Event-driven
func _on_unit_died(unit: Unit3D) -> void:
    GameStateEvents.unit_died.emit(unit, killer)
    # QuestSystem, AchievementSystem, Analytics all subscribe independently
```

### 2. Capabilities Instead of Global Pause

**"Can the player do X?" is a capability** with explicit block reasons. Multiple systems can block the same capability.

```gdscript
# ❌ Bad: Global pause
get_tree().paused = true  # Freezes EVERYTHING

# ✅ Good: Capability blocking
CapabilityManager.block_capability(Capability.PLAY_CARDS, BlockReason.DIALOGUE_ACTIVE)
# Camera still works, UI still animates, only card playing is blocked
```

### 3. Type-Safe Data-Driven Sequences

**Use strongly-typed Resource properties, not dictionaries.** This provides editor autocomplete and compile-time safety.

```gdscript
# ❌ Bad: Dictionary-based (typo-prone, no autocomplete)
@export var data: Dictionary = {"dialogue_id": "intro"}  # Can typo keys

# ✅ Good: Export groups (type-safe, editor-friendly)
@export_group("Dialogue")
@export var dialogue_id: String = ""  # Autocomplete, validation, no typos
```

### 4. Specialized Event Hubs, Not Monolithic Bus

**Multiple focused hubs prevent a single point of failure** and clarify dependencies.

```gdscript
# ❌ Bad: Monolithic event bus with 50+ signals
EventBus.battle_started.emit()
EventBus.quest_completed.emit()
EventBus.unit_spawned.emit()
# Hard to know what depends on what

# ✅ Good: Specialized hubs
GameStateEvents.battle_started.emit()  # Battle lifecycle only
QuestEvents.quest_completed.emit()      # Quest system only
# Clear dependencies, can load/unload independently
```

### 5. Phased Complexity

**Start simple, add features incrementally.** Phase 1 implements only 4 step types to prove the architecture before expanding.

---

## Current Problems

### Problem 1: Global Tree Pause is Fragile

**Current code:**
```gdscript
func freeze_game() -> void:
    get_tree().paused = true

func unfreeze_game() -> void:
    get_tree().paused = false
```

**Issues:**
- Every UI element that must stay interactive needs `PROCESS_MODE_ALWAYS` (easy to forget)
- ESC/pause menu can unpause in ways that break tutorial flow
- No partial pausing (e.g., block cards but allow camera movement)
- Debugging "why is this stuck?" requires manually checking process modes

**Real example from first_trial:**
```gdscript
# dialogue_box.gd - had to add this hack
process_mode = Node.PROCESS_MODE_ALWAYS  # Required to show during pause

# camera_controller_3d.gd - another hack
process_mode = Node.PROCESS_MODE_ALWAYS  # Required to pan during dialogue
```

### Problem 2: Tight Coupling Between Systems

**Current code:**
```gdscript
# BattleDialogueController directly controls GameController
func _show_dialogue(config: Dictionary) -> void:
    game_controller.freeze_game()  # Direct reference, hard to test

# BattleDialogueController directly grabs HandUI
func _action_show_hand() -> void:
    var hand_ui = get_node("/root/Battle3D/HandUI")  # Hard-coded path
    hand_ui.visible = true
```

**Issues:**
- Systems know too much about each other
- Hard to test in isolation (need full scene tree)
- Renames/refactors ripple through unrelated files
- Dialogue system can't be reused outside battles

### Problem 3: Limited Scriptability

**Current dialogue config:**
```gdscript
"dialogues": [
    {"trigger": "after_dialogue", "previous": "intro", "action": "show_hand"},
    {"trigger": "after_dialogue", "previous": "intro", "dialogue_id": "explain"},
]
```

**Issues:**
- Multi-step logic split across multiple entries
- No "wait for X" style triggers (enemy dies, base damaged, timer)
- No notion of sequences, branches, or cancellation
- Hard to visualize tutorial flow

### Problem 4: No Event Tracking

**Current limitation:**
No structured way to observe and react to game events for quests/tutorials.

**What we need:**
```gdscript
# Quest objective tracking
quest.track_objective("kill_10_slimes", enemy_kill_count)

# Tutorial conditional logic
on_event("card_played", filter: { card_type: "spell" }, do: unlock_feature())
```

---

## Architecture Components

### 1. CapabilityManager (Autoload)

**File:** `scripts/services/capability_manager.gd`

Central authority for "what can the player do right now?". Tracks blockers per capability with reasons.

**Key Features:**
- Enum-based capabilities (type-safe)
- Multiple systems can block same capability
- Emits signals when capabilities change
- Debug helpers to inspect current state

**Example:**
```gdscript
# Block during dialogue
CapabilityManager.block_capability(Capability.PLAY_CARDS, BlockReason.DIALOGUE_ACTIVE)

# Check before action
if CapabilityManager.is_enabled(Capability.PLAY_CARDS):
    play_card()

# Unblock when done
CapabilityManager.unblock_capability(Capability.PLAY_CARDS, BlockReason.DIALOGUE_ACTIVE)
```

### 2. GameStateEvents (Autoload)

**File:** `scripts/services/game_state_events.gd`

Specialized event hub for battle lifecycle and game state changes.

**Signals:**
- `battle_started`, `battle_ended` - Battle lifecycle
- `unit_spawned`, `unit_died` - Combat events
- `card_played`, `mana_changed` - Player actions
- `player_base_damaged`, `enemy_base_destroyed` - Objective events

**Note:** GameStateEvents currently hosts both high-level state events AND core combat events. In the future, combat events may be split into a separate `CombatEvents` hub if the scope becomes too wide.

**Why specialized hubs?**
- Clear dependencies (quest system only needs QuestEvents, not all game events)
- Can be loaded/unloaded independently
- No signal name collisions
- Easier to mock for testing

### 3. EventSequencer (Autoload)

**File:** `scripts/services/event_sequencer.gd`

Executes EventSequence resources step-by-step with proper async handling.

**Phase 1 supported step types:**
- `DIALOGUE` - Show dialogue and optionally wait for completion
- `WAIT_TIME` - Delay for specified duration
- `SET_CAPABILITY` - Enable/disable capabilities with reasons
- `SET_HAND_VISIBILITY` - Show/hide hand UI (needed for first_trial)

**Phase 2+ step types** (commented out for now):
- `WAIT_SIGNAL`, `SPAWN_UNIT`, `EMIT_SIGNAL`, `CUSTOM_FUNCTION`

**Current Limitations:**
- V1 supports only one sequence at a time (no queue or cancellation)
- `SET_HAND_VISIBILITY` requires HandUI node to be in group `"hand_ui"` (this step is tutorial-specific; longer-term UI steps may use CUSTOM_FUNCTION)
- **Phase 1:** EventSequencer can fall back to DialogueManager in group `"dialogue_manager"`
- **Phase 2+:** DialogueManager must be an autoload at `/root/DialogueManager` (standardized)

**Example:**
```gdscript
var sequence: EventSequence = load("res://sequences/first_trial_tutorial.tres")
EventSequencer.play_sequence(sequence)
await EventSequencer.sequence_finished
print("Tutorial complete!")
```

### 4. EventSequence & EventStep (Resources)

**Files:**
- `scripts/resources/event_sequence.gd`
- `scripts/resources/event_step.gd`

**Design choice: Export groups instead of dictionaries**

```gdscript
# EventStep uses export groups for type safety
@export var step_type: StepType = StepType.DIALOGUE

@export_group("Dialogue")
@export var dialogue_id: String = ""  # Only shown for DIALOGUE steps

@export_group("Wait Time")
@export var wait_duration: float = 1.0  # Only shown for WAIT_TIME steps
```

**Benefits:**
- Godot inspector shows only relevant fields per step type
- Autocomplete for property names
- No runtime dictionary parsing errors
- Can't typo property names

---

## Phase 1: Foundation

**Goal:** Get minimal event + capability infrastructure working without breaking existing game.
**Time:** ~6-8 hours
**Deliverable:** 4 core components that can execute simple sequences

### Implementation Files

**1. CapabilityManager** - `scripts/services/capability_manager.gd`

```gdscript
extends Node

enum Capability {
    PLAY_CARDS,      ## Can play cards from hand
    PAUSE_GAME,      ## Can open pause menu
    MOVE_CAMERA,     ## Can pan/zoom camera
    SKIP_DIALOGUE,   ## Can skip typewriter effect
    OPEN_MENU,       ## Can access game menus
}

enum BlockReason {
    DIALOGUE_ACTIVE,   ## Dialogue is currently playing
    CUTSCENE_PLAYING,  ## Cutscene in progress
    TUTORIAL_LOCKED,   ## Tutorial has locked this feature
    BATTLE_ENDING,     ## Battle ending (victory/defeat)
    LOADING,           ## Game is loading
}

signal capability_changed(capability: Capability, enabled: bool)

var _blocks: Dictionary = {}  # Capability -> Array[BlockReason]
@export var debug_mode: bool = false  # Toggle in inspector

func _ready() -> void:
    # Initialize all capabilities as enabled
    for cap: Capability in Capability.values():
        _blocks[cap] = []

func block_capability(cap: Capability, reason: BlockReason) -> void:
    if not _blocks.has(cap):
        _blocks[cap] = []

    var was_enabled: bool = is_enabled(cap)
    var reasons: Array = _blocks[cap]

    if reason not in reasons:
        reasons.append(reason)

        if debug_mode:
            print("CapabilityManager: Blocked %s (reason: %s)" % [
                Capability.keys()[cap],
                BlockReason.keys()[reason]
            ])

        if was_enabled and not is_enabled(cap):
            capability_changed.emit(cap, false)

func unblock_capability(cap: Capability, reason: BlockReason) -> void:
    if not _blocks.has(cap):
        return

    var was_enabled: bool = is_enabled(cap)
    var reasons: Array = _blocks[cap]
    var idx: int = reasons.find(reason)

    if idx >= 0:
        reasons.remove_at(idx)

        if debug_mode:
            print("CapabilityManager: Unblocked %s" % Capability.keys()[cap])

        if not was_enabled and is_enabled(cap):
            capability_changed.emit(cap, true)

func is_enabled(cap: Capability) -> bool:
    if not _blocks.has(cap):
        return true
    return _blocks[cap].is_empty()

func get_block_reasons(cap: Capability) -> Array:
    if not _blocks.has(cap):
        return []
    return _blocks[cap].duplicate()

## Debug helper: Print all capabilities and their block reasons
func print_debug_capabilities() -> void:
    print("\n=== Capability Debug State ===")
    for cap_name: String in Capability.keys():
        var cap: Capability = Capability[cap_name]
        var enabled: bool = is_enabled(cap)
        var reasons: Array = get_block_reasons(cap)

        var status: String = "✓ ENABLED" if enabled else "✗ BLOCKED"
        print("%s: %s" % [cap_name, status])

        if not enabled:
            print("  Reasons:")
            for reason: BlockReason in reasons:
                print("    - %s" % BlockReason.keys()[reason])
    print("=============================\n")
```

**2. GameStateEvents** - `scripts/services/game_state_events.gd`

```gdscript
extends Node

## GameStateEvents - Specialized event hub for battle lifecycle and game state
##
## Part of a specialized hub architecture:
##   - GameStateEvents: battle lifecycle, pause/resume, scene changes
##   - QuestEvents: (Phase 4) quest start/complete, objective progress
##   - CutsceneEvents: (Phase 5) cutscene triggers, camera control

## Battle lifecycle
signal battle_starting(battle_id: String)
signal battle_started()
signal battle_ending(victory: bool)
signal battle_ended(victory: bool)

## Unit lifecycle
signal unit_spawned(unit: Node3D, team: int, position: Vector3)
signal unit_damaged(unit: Node3D, damage: float, attacker: Node3D)
signal unit_died(unit: Node3D, killer: Node3D)

## Base/objective events
signal player_base_damaged(damage: float, current_health: float, max_health: float)
signal enemy_base_destroyed()

## Card/hand events
signal card_played(card: Card, position: Vector3, team: int)
signal mana_changed(current: int, max: int)

@export var debug_mode: bool = false

func _ready() -> void:
    if debug_mode:
        print("GameStateEvents: Initialized")
        _connect_debug_listeners()

func _connect_debug_listeners() -> void:
    battle_started.connect(func() -> void: print("[EVENT] battle_started"))
    unit_died.connect(func(unit: Node3D, killer: Node3D) -> void:
        print("[EVENT] unit_died: %s" % unit.name)
    )
    card_played.connect(func(card: Card, _pos: Vector3, team: int) -> void:
        print("[EVENT] card_played: %s (team %d)" % [card.get_card_name(), team])
    )
```

**3. EventStep Resource** - `scripts/resources/event_step.gd`

```gdscript
extends Resource
class_name EventStep

## EventStep - A single step in an EventSequence
##
## Uses export groups for type safety - only relevant fields shown per step type.

enum StepType {
    DIALOGUE,              ## Show dialogue
    WAIT_TIME,             ## Wait for specified duration
    SET_CAPABILITY,        ## Enable/disable a capability
    SET_HAND_VISIBILITY,   ## Show/hide hand UI

    # Phase 2+ step types (not yet implemented):
    # WAIT_SIGNAL,         ## Wait for a signal to emit
    # SPAWN_UNIT,          ## Spawn a unit on battlefield
    # EMIT_SIGNAL,         ## Emit a game event signal
    # CUSTOM_FUNCTION,     ## Call a custom GDScript function
    # MOVE_CAMERA,         ## Move camera to position
    # PLAY_ANIMATION,      ## Play an animation
    # FADE_SCREEN,         ## Fade screen in/out
}

@export var step_type: StepType = StepType.DIALOGUE
@export var description: String = ""  ## Human-readable description for debugging

## DIALOGUE
@export_group("Dialogue")
@export var dialogue_id: String = ""

## WAIT_TIME
@export_group("Wait Time")
@export var wait_duration: float = 1.0

## SET_CAPABILITY
@export_group("Set Capability")
@export var capability: int = 0  # CapabilityManager.Capability enum value
@export var enable: bool = false
@export var block_reason: int = 0  # CapabilityManager.BlockReason enum value

## SET_HAND_VISIBILITY
@export_group("Hand Visibility")
@export var hand_visible: bool = true
```

**4. EventSequence Resource** - `scripts/resources/event_sequence.gd`

```gdscript
extends Resource
class_name EventSequence

@export var sequence_id: String = ""
@export var description: String = ""
@export var steps: Array[EventStep] = []

func get_step_count() -> int:
    return steps.size()

func get_step(index: int) -> EventStep:
    if index < 0 or index >= steps.size():
        return null
    return steps[index]
```

**5. EventSequencer** - `scripts/services/event_sequencer.gd`

```gdscript
extends Node

## EventSequencer - Executes scripted EventSequences step-by-step
##
## Phase 1: Implements 4 step types (DIALOGUE, WAIT_TIME, SET_CAPABILITY, SET_HAND_VISIBILITY)
## Phase 2+: Will add WAIT_SIGNAL, SPAWN_UNIT, etc.

## Signals - pass both objects and IDs for rich logging
signal sequence_started(sequence: EventSequence, sequence_id: String)
signal sequence_finished(sequence: EventSequence, sequence_id: String)
signal step_started(step: EventStep, step_index: int, step_type: EventStep.StepType)
signal step_finished(step: EventStep, step_index: int)

var current_sequence: EventSequence = null
var current_step_index: int = -1
var is_playing: bool = false

@export var debug_mode: bool = false

func _ready() -> void:
    if debug_mode:
        print("EventSequencer: Initialized")
    _verify_dependencies()

func _verify_dependencies() -> void:
    var required: Array[String] = ["CapabilityManager", "GameStateEvents", "DialogueManager"]
    var missing: Array[String] = []

    for autoload_name: String in required:
        if not get_node_or_null("/root/" + autoload_name):
            missing.append(autoload_name)

    if not missing.is_empty():
        push_error("EventSequencer: Missing required autoloads: %s" % ", ".join(missing))

func play_sequence(sequence: EventSequence) -> void:
    if is_playing:
        push_warning("EventSequencer: Already playing sequence, ignoring new request")
        return

    if not sequence:
        push_error("EventSequencer: Tried to play null sequence")
        return

    if debug_mode:
        print("EventSequencer: Starting '%s' (%d steps)" % [
            sequence.sequence_id,
            sequence.get_step_count()
        ])

    is_playing = true
    current_sequence = sequence
    current_step_index = -1

    sequence_started.emit(sequence, sequence.sequence_id)

    # Execute all steps sequentially
    for i: int in range(sequence.get_step_count()):
        current_step_index = i
        var step: EventStep = sequence.get_step(i)

        if debug_mode:
            print("EventSequencer: Step %d/%d - %s" % [
                i + 1,
                sequence.get_step_count(),
                EventStep.StepType.keys()[step.step_type]
            ])

        step_started.emit(step, i, step.step_type)
        await _execute_step(step)
        step_finished.emit(step, i)

    # Sequence complete
    var finished_sequence: EventSequence = current_sequence
    var finished_id: String = finished_sequence.sequence_id
    current_sequence = null
    current_step_index = -1
    is_playing = false

    if debug_mode:
        print("EventSequencer: Sequence '%s' complete" % finished_id)

    sequence_finished.emit(finished_sequence, finished_id)

func _execute_step(step: EventStep) -> void:
    match step.step_type:
        EventStep.StepType.DIALOGUE:
            await _execute_dialogue(step)

        EventStep.StepType.WAIT_TIME:
            await _execute_wait_time(step)

        EventStep.StepType.SET_CAPABILITY:
            _execute_set_capability(step)

        EventStep.StepType.SET_HAND_VISIBILITY:
            _execute_set_hand_visibility(step)

        _:
            push_warning("EventSequencer: Step type not yet implemented: %s" %
                EventStep.StepType.keys()[step.step_type])

func _execute_dialogue(step: EventStep) -> void:
    if step.dialogue_id.is_empty():
        push_error("EventSequencer: DIALOGUE step missing dialogue_id")
        return

    # NOTE: DialogueManager owns capability blocking (PLAY_CARDS, PAUSE_GAME)
    # It blocks on start_dialogue() and unblocks on dialogue_ended
    # This centralizes responsibility and prevents double-blocking

    # Try to find DialogueManager (autoload or scene node)
    var dialogue_manager: Node = get_node_or_null("/root/DialogueManager")
    if not dialogue_manager:
        dialogue_manager = get_tree().get_first_node_in_group("dialogue_manager")

    if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
        dialogue_manager.call("start_dialogue", step.dialogue_id)
        if dialogue_manager.has_signal("dialogue_ended"):
            await dialogue_manager.dialogue_ended
        else:
            push_error("EventSequencer: DialogueManager doesn't have 'dialogue_ended' signal")
    else:
        push_error("EventSequencer: DialogueManager not found or doesn't have start_dialogue method")

func _execute_wait_time(step: EventStep) -> void:
    await get_tree().create_timer(step.wait_duration).timeout

func _execute_set_capability(step: EventStep) -> void:
    if step.enable:
        CapabilityManager.unblock_capability(step.capability, step.block_reason)
    else:
        CapabilityManager.block_capability(step.capability, step.block_reason)

func _execute_set_hand_visibility(step: EventStep) -> void:
    var hand_ui: Node = _find_hand_ui()
    if hand_ui and hand_ui is CanvasItem:
        (hand_ui as CanvasItem).visible = step.hand_visible
        if debug_mode:
            print("EventSequencer: Set hand visibility to %s" % step.hand_visible)
    else:
        push_warning("EventSequencer: Could not find HandUI")

func _find_hand_ui() -> Node:
    return get_tree().get_first_node_in_group("hand_ui")
```

### Autoload Registration

**Add to project.godot:**

```ini
[autoload]
CapabilityManager="*res://scripts/services/capability_manager.gd"
GameStateEvents="*res://scripts/services/game_state_events.gd"
EventSequencer="*res://scripts/services/event_sequencer.gd"
```

### Testing Phase 1

Create a simple test sequence:

**File:** `resources/sequences/test_sequence.tres`

```gdscript
# Create in Godot editor:
# 1. Create new EventSequence resource
# 2. Set sequence_id = "test_sequence"
# 3. Add 3 EventStep resources to steps array:
#    - Step 1: DIALOGUE, dialogue_id="test_greeting"
#    - Step 2: WAIT_TIME, wait_duration=1.0
#    - Step 3: DIALOGUE, dialogue_id="test_goodbye"
```

**Test in dev console:**
```gdscript
var seq: EventSequence = load("res://resources/sequences/test_sequence.tres")
EventSequencer.play_sequence(seq)
```

**Expected behavior:**
1. Dialogue appears and blocks card playing
2. After dismissing dialogue, 1 second delay
3. Second dialogue appears
4. Cards unblocked after sequence completes

---

## Phase 2: Tutorial Migration

**Goal:** Convert first_trial from inline dialogue config to EventSequence.
**Time:** ~8-10 hours
**Deliverable:** first_trial tutorial works with new system, old code still present as fallback

### Prerequisites

**DialogueManager must be registered as an autoload** in project.godot:
```
DialogueManager="*res://scripts/services/dialogue_manager.gd"
```

This is required because:
- EventSequencer looks for it via `/root/DialogueManager` first
- Phase 1 dependency verification checks for it

### Steps

1. **Update DialogueManager** to use CapabilityManager
   - DialogueManager **owns** capability blocking during dialogue
   - `start_dialogue()` blocks PLAY_CARDS and PAUSE_GAME with BlockReason.DIALOGUE_ACTIVE
   - `dialogue_ended` signal triggers unblock of capabilities
   - This prevents double-blocking issues if EventSequencer tried to manage it

2. **Create first_trial_tutorial.tres** sequence
3. **Update BattleDialogueController** to load event sequences
4. **Test thoroughly** before removing old code

*(Full implementation details to be added after Phase 1 is proven)*

---

## Phase 3: Game State Integration

**Goal:** Make GameController use capabilities + events instead of tree pausing.
**Time:** ~6-8 hours
**Deliverable:** Pause menu, battle lifecycle, scene transitions all use new architecture

*(Full implementation details to be added after Phase 2 is complete)*

---

## Phase 4-5: Future Phases

### Phase 4: Quest System
- QuestEvents hub (autoload)
- Quest and QuestObjective resources
- QuestSystem manager subscribing to game events
- Event-driven objective tracking

### Phase 5: Cutscene System
- Add WAIT_SIGNAL, SPAWN_UNIT, MOVE_CAMERA, etc. step types
- CutsceneEvents hub
- Advanced sequence features (branches, conditions, cancellation)

---

## Best Practices

### Event Naming

**Good:**
```gdscript
signal unit_died(unit: Unit3D, killer: Unit3D)
signal card_played(card: Card, team: int)
```

**Bad:**
```gdscript
signal event_happened()  # Too vague
signal OnUnitDied()      # Wrong naming convention
```

### Capability Checks

**Always check at input boundaries:**
```gdscript
func _on_card_clicked(card: Card) -> void:
    if not CapabilityManager.is_enabled(CapabilityManager.Capability.PLAY_CARDS):
        var reasons: Array = CapabilityManager.get_block_reasons(CapabilityManager.Capability.PLAY_CARDS)
        _show_cannot_play_tooltip(reasons)
        return

    play_card(card)
```

### Sequence Design

**Keep sequences focused:**
- ✅ `tutorial_intro.tres` - Just the intro
- ✅ `tutorial_summon_prompt.tres` - Just prompting to summon
- ❌ `tutorial_everything.tres` - 50 steps doing everything

---

## Debugging Guide

### Capability Issues

**Problem:** "I can't play cards, but I don't know why."

**Solution:** Use the debug helper:
```gdscript
CapabilityManager.print_debug_capabilities()
```

**Output:**
```
=== Capability Debug State ===
PLAY_CARDS: ✗ BLOCKED
  Reasons:
    - DIALOGUE_ACTIVE
    - TUTORIAL_LOCKED
MOVE_CAMERA: ✓ ENABLED
PAUSE_GAME: ✓ ENABLED
=============================
```

### Sequence Issues

**Problem:** "Tutorial is stuck at step 3."

**Solution:** Check logs for EventSequencer output:
```
EventSequencer: Starting 'first_trial_tutorial' (8 steps)
EventSequencer: Step 1/8 - DIALOGUE
EventSequencer: Step 2/8 - WAIT_TIME
EventSequencer: Step 3/8 - DIALOGUE
```

If stuck, check:
1. Is DialogueManager working? (`dialogue_ended` signal firing?)
2. Is the step type implemented? (Check match statement in `_execute_step`)
3. Enable `debug_mode = true` in inspector for EventSequencer

### Event Hub Debugging

**Enable debug mode for GameStateEvents:**
```gdscript
GameStateEvents.debug_mode = true
```

**Output:**
```
[EVENT] battle_started
[EVENT] unit_spawned: Slime
[EVENT] card_played: Fireball (team 0)
[EVENT] unit_died: Slime
```

---

## Migration Checklist

### Phase 1: Foundation
- [x] Implement CapabilityManager with debug helpers
- [x] Implement GameStateEvents hub
- [x] Implement EventStep and EventSequence resources
- [x] Implement EventSequencer (4 step types)
- [ ] Register autoloads in project.godot
- [ ] Create test_sequence.tres and verify it runs
- [ ] Test capability blocking/unblocking
- [ ] Test debug helpers work

### Phase 2: Tutorial Migration
- [ ] Update DialogueManager to use CapabilityManager
- [ ] Create first_trial_tutorial.tres
- [ ] Update BattleDialogueController to load sequences
- [ ] Test full first_trial flow
- [ ] Keep old code as fallback during testing

### Phase 3: Game State Integration
- [ ] Update GameController to emit GameStateEvents
- [ ] Refactor pause/resume to use capabilities
- [ ] Test pause menu doesn't conflict with tutorials

### Phase 4-5: Later
- [ ] Design and implement QuestEvents hub
- [ ] Implement quest tracking system
- [ ] Add Phase 2+ step types to EventStep
- [ ] Build advanced cutscene features

---

**End of Document**
