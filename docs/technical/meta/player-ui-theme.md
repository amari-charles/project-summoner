# Player UI Theme

Player-facing interface chrome uses one desaturated light-wood placeholder theme. This is
an implementation convention, not final art direction: keeping the structural
colors centralized lets the team evaluate layouts now and retheme them later
without rewriting individual screens.

## Sources

- `resources/visual/base_theme.tres` defines the default Godot styles for common
  controls: buttons, labels, panels, inputs, menus, separators, and progress bars.
- `scripts/shared/color_palette.gd` (`GameColorPalette`) defines colors used by
  controls that construct or override styles at runtime.
- `scripts/shared/button_style_factory.gd` builds the shared primary, secondary,
  and danger button variants from that palette.

Prefer the global theme. Add a local override only when a component communicates
a meaningful state or must remain readable over game art. Runtime-created styles
must use `GameColorPalette` rather than introducing another structural color.

## Semantic Color Exceptions

These colors retain gameplay meaning and are not replaced by the neutral theme:

- elemental identities and summoner/card art;
- health, mana, shield, success, warning, and error states;
- rarity and currency indicators;
- VFX, battlefield presentation, and modal dimming overlays.

Developer and debug tooling is outside the manual migration scope. It can inherit
global defaults where it has no explicit style, but does not need bespoke cleanup.

## Adding Player UI

Use standard Godot controls and allow `base_theme.tres` to style them. For custom
surfaces or dynamic controls, select the nearest `UI_*`, `TEXT_*`, or button-state
constant from `GameColorPalette`. If a new semantic color is necessary, add a
named palette constant that explains its meaning rather than embedding a screen-
specific blue, purple, gray, or white literal.
