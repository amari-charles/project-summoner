# Battle Biomes

Battle biomes are authored battle data. They select environment presentation
without changing the shared battle runtime or duplicating battle scenes.

## Authoring Flow

1. A battle definition owns a typed `BiomeId`.
2. The launch boundary serializes that value as `biome_id`.
3. `BattleContext` retains the selected ID for the configured battle.
4. `BaseBattlefield3D` loads `res://resources/biomes/<biome_id>.tres`.
5. `BiomeConfig` applies the ground, lighting, and environment configuration.

Academy activities with `ExecutionKind.Battle` must author a battle config. The
Academy catalog validates that the config exists and references a known biome.
An omitted biome uses `BiomeIds.Default` (`summer_plains`).

## Visual Strategies

`BiomeConfig` supports two visual strategies while keeping its hidden
`Background` plane authoritative for gameplay and camera bounds:

- Standard ground: a flat material or generated checker pillars.
- Custom arena visual: an optional `PackedScene` mounted under
  `GroundLayer/BiomeVisuals` and configured with the logical ground size.

Custom arena scenes are presentation-only. They must not add gameplay bounds,
simulation rules, collision policy, or activity-specific behavior.

## Current Biomes

- `summer_plains`: default checker-pillar battlefield.
- `island_water`: Tiny Swords grass-and-water placeholder used by
  `magic_101_summon_practice` for visual testing.

The island arena adapts the campus placeholder construction to the battle
camera's opposite viewing direction. Its single cliff row therefore belongs on
the negative-Z edge; the left and right edges remain flat edge tiles rather
than repeated vertical stone walls.

## Adding a Biome

1. Add the typed ID to `BiomeIds` and the mirrored GDScript ID to `BiomeIDs`.
2. Add a matching resource under `resources/biomes/`.
3. If necessary, add a visual-only scene under
   `scenes/battle/battlefield/biomes/`.
4. Add the ID to both validation lists and cover the resource/visual contract
   with focused tests.
