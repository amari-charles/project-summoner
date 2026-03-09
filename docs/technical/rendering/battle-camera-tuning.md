# Battle Camera Tuning Cheat Sheet

Purpose: quick, non-technical guidance for changing battle camera framing without guessing.

## Where To Edit

Primary camera profile values:
- `resources/camera_profiles/battle_perspective.tres` -> `camera_transform`
- `resources/camera_profiles/battle_orthographic.tres` -> `camera_transform`

Scene default (keep aligned with perspective profile):
- `scenes/battle/battlefield/components/base_battlefield_3d.tscn` -> `Camera3D.transform`

## Plain-English Controls

Current camera tilt is fixed by basis values:
- `Transform3D(..., 0, 0.819152, 0.573576, 0, 0.573576, -0.819152, ...)`

Do not change those basis numbers unless you want to change camera angle.

Use only the position part `(x, y, z)` for framing:

| Goal | Change |
|---|---|
| Move view left/right | Change `x` (`-` = left, `+` = right) |
| Move camera higher/lower | Change `y` (`+` = higher, `-` = lower) |
| Push view deeper into the field (toward far edge) | Increase `z` |
| Pull view back toward player side (near edge) | Decrease `z` |

## Important: True "Forward" Move

Because camera is tilted, true forward motion is not `z` alone.

If you want to move exactly in camera-facing direction by `d` world units:
- `y = y - (0.573576 * d)`
- `z = z + (0.819152 * d)`

Example for `d = 20`:
- `y` changes by `-11.47`
- `z` changes by `+16.38`

Reverse direction by negating `d`.

## Zoom Behavior (Not Position)

Zoom limits come from camera profile zoom values:
- Perspective: `default_zoom`, `min_zoom`, `max_zoom` in `battle_perspective.tres`
- Orthographic: `default_zoom`, `min_zoom`, `max_zoom` in `battle_orthographic.tres`

In controller:
- `default_fov`/`max_fov` (perspective): higher value shows more map (zoom out)
- `default_ortho_size`/`max_ortho_size` (orthographic): higher value shows more map

## Field Bounds Vs Zoom Bounds

In `scripts/battle/battlefield/camera_controller_3d.gd`:
- `map_rect_xz`: live pan/clamp area (actual arena)
- `zoom_limit_rect_xz`: zoom-solver area (how far camera is allowed to zoom out)
- `horizontal_oversize_clamp_mode` / `vertical_oversize_clamp_mode`: oversized-view policy per axis:
  - `CENTER` (default): centers oversized footprint
  - `PIN_MIN_EDGE`: pins view to map min edge
  - `PIN_MAX_EDGE`: pins view to map max edge
- `vertical_pin_min_edge_margin` / `vertical_pin_max_edge_margin`: offset from pinned edge in world units
- `vertical_center_reference_screen_y`: when vertical mode is `CENTER`, this is the screen row that should align to map-center depth

Use this when arena size changes but you want old zoom restrictions.

In `scripts/battle/battlefield/base_battlefield_3d.gd`:
- `camera_bounds_padding_x`: expands camera clamp beyond mesh left/right
- `camera_bounds_padding_z`: expands camera clamp beyond mesh near/far
- `camera_bounds_padding_left`: extra room only on left edge
- `camera_bounds_padding_right`: extra room only on right edge
- `camera_bounds_padding_toward_camera`: extra room only on the near edge
- `camera_bounds_padding_away_from_camera`: extra room only on the far edge
- `include_startup_camera_footprint_in_bounds_x`: preserves startup framing horizontally
- `include_startup_camera_footprint_in_bounds_z`: preserves startup framing in depth

Plain-English:
- Increase `camera_bounds_padding_z` if the camera feels stuck to arena depth edges and you want to see outside the field.
- Increase `camera_bounds_padding_away_from_camera` if startup keeps snapping backward because the far edge of the view is hitting the field limit.
- Increase `camera_bounds_padding_toward_camera` if the camera cannot move close enough to the player side.
- Turn on `include_startup_camera_footprint_in_bounds_z` if the match starts in the right depth but snaps on load.
- Leave `include_startup_camera_footprint_in_bounds_x` off if you want horizontal clamp bounds to stay tight to the arena.

## Debug Overlay

With the camera overlay enabled:
- Blue outline = live camera clamp bounds
- Gold outline = actual arena floor mesh bounds

This makes it obvious when the camera is allowed to show space outside the playable floor.

## Quick Safety Checklist

1. Keep profile and scene camera transforms in sync.
2. Keep basis unchanged unless intentionally changing tilt.
3. Test both projection modes (Perspective + Orthographic).
4. Toggle debug overlay in Debug Menu to verify clamp footprint.
