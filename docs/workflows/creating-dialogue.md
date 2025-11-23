# Creating Dialogue for Battles

This document explains how to create dialogue for battles, events, and tutorials.

## Quick Start

**For most cases, you only need to edit JSON - NO manual .tres file creation!**

### Step 1: Add dialogue text to localization

Edit `/localization/data/en.json` and add your dialogue under the `"dialogue"` section:

```json
{
  "dialogue": {
    "my_dialogue_id": {
      "text": "Welcome, brave summoner!",
      "speaker": "Merlin"
    }
  }
}
```

### Step 2: Run the dialogue generator

In Godot Editor:
1. Open `scripts/tools/dialogue_resource_generator.gd`
2. Click **File > Run** (or press Ctrl/Cmd+Shift+X)
3. Check the output console - it will show created/updated files

That's it! The `.tres` files are generated automatically in `resources/dialogue/`.

---

## Dialogue Resource Structure

The generator creates `.tres` files that look like this:

```tres
[gd_resource type="Resource" script_class="DialogueData" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/dialogue/dialogue_data.gd" id="1_dialogue"]

[resource]
script = ExtResource("1_dialogue")
dialogue_id = "my_dialogue_id"
text = "dialogue.my_dialogue_id.text"
speaker = "dialogue.my_dialogue_id.speaker"
portrait = ""
duration = 0.0
```

**Note:** The `text` and `speaker` fields reference localization keys, not literal strings. This allows for future translation support.

---

## Using Dialogue in Event Sequences

Once the dialogue resource exists, reference it in your EventSequence:

```gdscript
# In your .tres EventSequence file:
[sub_resource type="Resource" id="step_intro"]
script = ExtResource("2_step")
step_type = 0  # DIALOGUE
dialogue_id = "my_dialogue_id"
```

Or programmatically:

```gdscript
if DialogueManager:
    DialogueManager.start_dialogue("my_dialogue_id")
```

---

## Troubleshooting

### Dialogue not appearing in game

**Check:**
1. Did you run the dialogue generator after adding to JSON?
2. Is the dialogue_id spelled correctly in your event sequence?
3. Check console for errors like "Dialogue not found: ..."

**Common issue:** If you created `.tres` files manually before this tool existed, they might have wrong script paths. Run the generator to fix them automatically.

### Generator reports "missing 'text' or 'speaker'"

Your JSON entry is incomplete. Both fields are required:

```json
// ❌ Wrong
"my_dialogue": {
  "text": "Hello!"
  // Missing speaker!
}

// ✅ Correct
"my_dialogue": {
  "text": "Hello!",
  "speaker": "Merlin"
}
```

---

## Advanced: Manual .tres Creation (NOT RECOMMENDED)

If you absolutely must create dialogue resources manually:

1. **DO NOT** - Use the generator instead
2. If you insist: Ensure script path is `res://scripts/dialogue/dialogue_data.gd`
3. Use `script_class="DialogueData"` not `"Dialogue"`
4. Reference localization keys, not literal text

**Why not manual?** Easy to make mistakes:
- Wrong script path → resource fails to load → no dialogue appears
- Wrong class name → type errors
- Typos in dialogue_id → dialogue not found
- Forgetting to update localization → blank text

The generator prevents all these errors.

---

## Workflow Summary

```
1. Edit en.json (add dialogue text)
   ↓
2. Run dialogue_resource_generator.gd
   ↓
3. Use dialogue_id in event sequences
   ↓
4. Test in game
```

**Files you edit:** `localization/data/en.json`
**Files auto-generated:** `resources/dialogue/*.tres`
**Files you reference:** dialogue_id strings in event sequences

---

## Related Files

- `/localization/data/en.json` - Source of truth for all dialogue text
- `/scripts/tools/dialogue_resource_generator.gd` - Auto-generates .tres files
- `/scripts/dialogue/dialogue_data.gd` - DialogueData resource class
- `/scripts/services/dialogue_manager.gd` - Runtime dialogue system
- `/resources/dialogue/` - Generated .tres files (don't edit manually!)
