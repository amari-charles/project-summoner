# Visual Overhaul TODO

This document tracks the premium visual upgrades for Project Summoner. Each item should be implemented in a separate PR with before/after screenshots unless coupling is necessary.

## Progress Tracking

- [ ] WorldEnvironment (bloom, tonemap, vignette, grain)
- [ ] Camera tilt (optional - test if it improves diorama feel)
- [ ] Full character-shaped shadows (replace current blob shadows)
- [ ] Baked AO + ground shading into terrain
- [ ] Color grading LUT
- [ ] Fog / atmospheric layers
- [ ] Post-processing polish (chromatic aberration for transitions)
- [ ] Dynamic lights for spells

## Implementation Notes

### WorldEnvironment
**Goal:** Add bloom, professional tonemapping, and subtle post-processing
**Changes:**
- Add WorldEnvironment node to battle_3d.tscn
- Configure Environment resource:
  - Bloom: intensity 0.1-0.25, threshold 1.0
  - Tonemap: ACES (filmic look)
  - Ambient light: warm tint (orange/fire theme)
  - Vignette: 3-5% intensity
  - Grain: 0.5-1% (if available)
- Test performance (maintain 60fps)

### Camera Tilt
**Goal:** Create diorama/HD-2D pop-up book feel
**Changes:**
- Test orthographic camera with 15-25° tilt
- Compare with current flat orthographic setup
- Keep if it improves visual depth without sacrificing gameplay clarity

### Full Character-Shaped Shadows
**Goal:** Replace blob shadows with character-shaped shadows like Mini Warriors Reborn
**Changes:**
- Create shadow textures per unit type matching sprite footprint
- Tint dark (rgba ~0,0,0,0.35)
- Squash scale.y to hug ground
- Slightly offset & stretch in light direction
- Blur edges via shader or baked soft edges
**Note:** This is marked as the #1 most important visual upgrade

### Baked AO + Ground Shading
**Goal:** Make terrain feel 3D and anchor shadows
**Changes:**
- Add soft AO under props, edges, typical unit areas
- Paint directional shading (darker opposite light)
- Integrate into terrain textures

### Color Grading LUT
**Goal:** Unified, cohesive art direction
**Changes:**
- Export screenshot from battle
- Apply color grade in image editor (warm/vibrant for fire theme)
- Create LUT texture
- Apply in Environment settings
- Consider different LUTs per biome/element

### Fog / Atmospheric Layers
**Goal:** Cinematic depth, tie layers together
**Changes:**
- Add fog planes behind units
- Light gradient across battlefield (subtle)
- Desaturate far layers slightly
- Creates atmosphere between depth layers

### Post-Processing Polish
**Goal:** Finished, authored look
**Changes:**
- 3-5% vignette (already in WorldEnvironment)
- 0.5-1% grain (already in WorldEnvironment)
- Tiny chromatic aberration during transitions only
- Keep subtle - should enhance, not distract

### Dynamic Lights for Spells
**Goal:** Combat feels alive without tanking performance
**Changes:**
- Add short-lived point light on spell impacts
- Add tiny color flash on strong hits
- Apply only to major spell effects (fireballs, explosions)
- Test performance with multiple simultaneous spells

## Completion Criteria

Delete this file once all items are checked off and merged to main.

## Notes

- Skipping parallax layers (not needed for current 2.5D setup)
- Focus on foundation polish before content expansion (per CLAUDE.md philosophy)
- Each upgrade should be visually testable and provide clear improvement
