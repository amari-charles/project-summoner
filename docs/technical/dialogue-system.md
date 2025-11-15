# Dialogue System

A modular, signal-based dialogue system for Godot 4 with typewriter effects and choice support.

## Setup Instructions

### 1. Add DialogueManager to Autoloads

DialogueManager is already configured in `project.godot` as an autoload singleton.

### 2. Test the System

1. Open `scenes/dialogue_test.tscn`
2. Press F6 to run the current scene
3. Click the buttons to test different dialogue scenarios:
   - **Simple Dialogue**: Basic multi-line dialogue with typewriter effect
   - **Choice Dialogue**: Dialogue with branching choices
   - **Chain Dialogue**: Multiple dialogues linked together

## Architecture

### Core Components

**DialogueData** (`scripts/dialogue/dialogue_data.gd`)
- Custom Resource containing dialogue content
- Properties: dialogue_id, character_name, portrait, lines, choices, next_dialogue_id

**DialogueChoice** (`scripts/dialogue/dialogue_choice.gd`)
- Custom Resource for choice options
- Properties: choice_text, next_dialogue_id, condition, action

**DialogueManager** (`scripts/services/dialogue_manager.gd`)
- Singleton service managing dialogue state and flow
- Handles dialogue progression, choice selection, variable tracking
- Emits signals for UI to react to

**DialogueBox** (`scripts/ui/dialogue_box.gd`)
- UI component displaying dialogue
- Listens to DialogueManager signals
- Implements typewriter effect
- Dynamically generates choice buttons

### Features

- ✅ Typewriter effect with configurable speed
- ✅ Click to skip animation or advance dialogue
- ✅ Multi-line dialogues
- ✅ Branching choices
- ✅ Chained dialogues (auto-advance to next)
- ✅ Character names and portraits
- ✅ Conditional choices (based on variables)
- ✅ Actions (set variables when choosing)
- ✅ Signal-based communication (decoupled)
- ✅ Resource-based data (editor-friendly)

## Creating Dialogues

### Option 1: In Godot Editor (Recommended)

1. Right-click in FileSystem → Create New → Resource
2. Search for `DialogueData` and select it
3. Fill in the properties in the Inspector
4. Save as `.tres` file in `resources/dialogue/`

### Option 2: Manually Edit .tres Files

See examples in `resources/dialogue/`:
- `simple_greeting.tres` - Basic dialogue
- `choice_example.tres` - Dialogue with choices
- `chain_start.tres` / `chain_end.tres` - Linked dialogues

## Integration

### Starting Dialogue from Code

```gdscript
# Get DialogueManager
var dialogue_manager = get_node("/root/DialogueManager")

# Start a dialogue by ID
dialogue_manager.start_dialogue("simple_greeting")
```

### Adding DialogueBox to Your Scene

1. Instance `scenes/ui/dialogue_box.tscn` in your scene
2. Add as child of a CanvasLayer (to render above game)
3. DialogueBox will automatically connect to DialogueManager

### Listening to Dialogue Events

```gdscript
var dialogue_manager = get_node("/root/DialogueManager")

dialogue_manager.dialogue_started.connect(_on_dialogue_started)
dialogue_manager.dialogue_ended.connect(_on_dialogue_ended)

func _on_dialogue_started(dialogue_data: DialogueData):
    # Pause game, hide UI, etc.
    pass

func _on_dialogue_ended():
    # Resume game, show UI, etc.
    pass
```

## Customization

### Typewriter Speed

Edit `DialogueBox` node → Inspector → Typewriter Speed (default: 0.05)

### UI Styling

Edit `scenes/ui/dialogue_box.tscn`:
- Modify Panel theme
- Adjust font sizes
- Change colors
- Reposition elements

### Adding Variables/Conditions

```gdscript
# Set a variable
DialogueManager.set_variable("has_sword", true)

# In DialogueChoice resource:
# condition = "has_sword"  # Only show if has_sword is true
# action = "talked_to_npc=true"  # Set variable when chosen
```

## Integration Examples

### Campaign Battle Intro

```gdscript
# In battle scene _ready():
func _ready():
    DialogueManager.dialogue_started.connect(_on_dialogue_started)
    DialogueManager.dialogue_ended.connect(_on_dialogue_ended)

    # Start intro dialogue
    DialogueManager.start_dialogue("battle_01_intro")

func _on_dialogue_started(_data):
    get_tree().paused = true  # Pause battle

func _on_dialogue_ended():
    get_tree().paused = false  # Resume battle
```

### NPC Interaction

```gdscript
# When player interacts with NPC:
func _on_npc_interacted():
    DialogueManager.start_dialogue("npc_greeting")
```

## Advanced Features (Future)

- Audio playback (character voices)
- Animated portraits
- Screen shake/effects during dramatic lines
- Localization support
- Save/load dialogue state
- Quest/objective integration

## File Structure

```
res://
├── resources/dialogue/        # Dialogue content (.tres files)
├── scenes/ui/dialogue_box.tscn    # UI prefab
├── scripts/
│   ├── dialogue/
│   │   ├── dialogue_data.gd        # Resource class
│   │   └── dialogue_choice.gd      # Choice resource class
│   ├── services/dialogue_manager.gd  # Singleton (autoload)
│   └── ui/dialogue_box.gd          # UI controller
└── docs/technical/dialogue-system.md  # This file
```
