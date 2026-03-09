extends Resource
class_name BattleCameraProjectionProfile

## Shared profile interface for the battle camera (perspective-only).

@export var camera_transform: Transform3D = Transform3D.IDENTITY
@export var keep_aspect: int = Camera3D.KEEP_HEIGHT
@export var near_clip: float = 0.5
@export var far_clip: float = 260.0

## Perspective FOV values.
@export var default_zoom: float = 40.0
@export var min_zoom: float = 20.0
@export var max_zoom: float = 50.0

@export var vertical_pan_only_when_zoomed: bool = true
@export var horizontal_bounds_use_screen_sample: bool = false
@export_range(0.0, 1.0, 0.01) var horizontal_bounds_screen_y: float = 0.5
@export var vertical_far_clamp_margin: float = 0.0
