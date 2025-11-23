# Godot 4 Strict Typing Validation

## Overview

This document describes how to validate GDScript strict typing errors from the command line in Godot 4.

## The Problem

Godot 4's strict typing warnings (when set to error level) appear in the **Errors tab** of the editor GUI, but are NOT output to the console when using standard headless commands.

### What Doesn't Work

```bash
# ❌ Does NOT show strict typing errors
/path/to/Godot --headless --check-only --path /path/to/project
```

This command only validates basic syntax and outputs console messages, but **misses** the type checking errors that appear in the editor's Errors tab.

## The Solution

Use the **editor mode** with **auto-quit** and **verbose** flags:

```bash
cd /path/to/your/project
/path/to/Godot -e --quit --verbose 2>&1 | grep "SCRIPT ERROR"
```

### Command Breakdown

- `-e` or `--editor`: Opens the Godot Editor (not just the project selector)
- `--quit`: Automatically quits after loading the project
- `--verbose`: Outputs detailed information including script errors
- `2>&1`: Redirects stderr to stdout
- `| grep "SCRIPT ERROR"`: Filters output to show only script errors

### For macOS (Godot.app)

```bash
cd /Users/username/Code/your-project
/Users/username/Downloads/Godot.app/Contents/MacOS/Godot -e --quit --verbose 2>&1 | grep "SCRIPT ERROR"
```

### Save Full Output to File

```bash
cd /path/to/your/project
/path/to/Godot -e --quit --verbose 2>&1 > /tmp/godot_errors.txt
```

Then search the file for specific error patterns:

```bash
grep "Warning treated as error" /tmp/godot_errors.txt
grep "Parse Error" /tmp/godot_errors.txt
```

## Example Output

```
SCRIPT ERROR: Parse Error: The property "team" is not present on the inferred type "Node" (but may be present on a subtype). (Warning treated as error.)
SCRIPT ERROR: Parse Error: Variable "card" has no static type. (Warning treated as error.)
SCRIPT ERROR: Parse Error: Constant "MANA_MAX" has an implicitly inferred static type. (Warning treated as error.)
```

## Why This Works

The `-e` flag opens the actual Godot Editor process (not the headless runtime), which triggers the full GDScript parser and type checker. The `--quit` flag makes it exit immediately after loading, and `--verbose` ensures all diagnostics are printed to stdout/stderr.

## Limitations

- **Brief GUI flash**: The editor window may briefly appear before quitting
- **Slower than headless**: Takes longer to load the full editor vs headless mode
- **macOS specific**: Path to Godot.app binary may differ on other systems

## Integration with CI/CD

For automated testing, you can parse the output programmatically:

```bash
#!/bin/bash
cd /path/to/project
OUTPUT=$(/path/to/Godot -e --quit --verbose 2>&1)
ERRORS=$(echo "$OUTPUT" | grep -c "Warning treated as error")

if [ "$ERRORS" -gt 0 ]; then
    echo "Found $ERRORS strict typing errors:"
    echo "$OUTPUT" | grep "SCRIPT ERROR"
    exit 1
else
    echo "No strict typing errors found!"
    exit 0
fi
```

## Alternative Approaches Investigated

### GDScript LSP
- Requires running editor in background
- Complex setup with LSP client
- Performance issues on large projects
- **Not recommended** for this use case

### gdlint / gdscript-toolkit
- Only checks style/formatting, **not type errors**
- Does not validate Godot's strict typing system
- **Does not solve this problem**

### --headless --script-validation-only
- Not a valid flag in Godot 4
- **Does not exist**

## References

- [Godot Command Line Tutorial](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)
- [GDScript Warning System](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/warning_system.html)
- [GitHub Issue: Structured Error Output](https://github.com/godotengine/godot-proposals/issues/13048)

## Credits

Solution discovered from forum post suggesting `-e --verbose` as workaround for accessing editor diagnostics from command line.
