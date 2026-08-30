# UI Designer Review Build

The UI designer review build enables the guided UI walkthrough without changing
the behavior of normal editor sessions or player builds.

For the canonical packaging, Windows validation, Google Drive destination, and
current downloadable build, see [UI Designer Handoff](ui-designer-handoff.md).

## Runtime mode ownership

`UiTutorialModeService` resolves the runtime mode once at process startup. It is
enabled when either:

- the exported build has the `ui_tutorial` custom feature, or
- a developer launches the project with the `--ui-tutorial` user argument.

The mode is intentionally immutable during a run. Walkthrough quest availability,
guidance, seeded upgrade examples, the showcase reset action, the welcome and
completion messages, and the fresh-profile muted default all use this authority.
Normal runs use the standard introduction quest and normal audio defaults.

## Exporting for the UI designer

Use the committed `UI Designer Review` Windows export preset. It includes the
`ui_tutorial` feature automatically, so the recipient does not need to enable a
setting or know a command-line argument.

```bash
dotnet build
/Applications/Godot_mono.app/Contents/MacOS/Godot \
  --headless \
  --path . \
  --export-release "UI Designer Review"
```

Distribute the complete review ZIP described in the handoff guide. Its
`Fateforged-UI-Review.exe` and matching .NET data directory must remain together.
Normal releases continue to use the `Windows Desktop` preset.

## Local walkthrough testing

To opt an editor or command-line run into the same mode without exporting:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot \
  --path . \
  --editor \
  -- \
  --ui-tutorial
```
