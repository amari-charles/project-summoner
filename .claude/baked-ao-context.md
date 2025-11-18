# Baked AO Ground - Context & Status

## Current State
Working but visually unsatisfying ground shader with ambient occlusion.

## What Works
- ✅ Shader compiles and renders
- ✅ Dark AO circles visible at spawn positions (-40, 0, -7.5) and (40, 0, -7.5)
- ✅ Edge darkening functional
- ✅ Directional gradient working

## What Doesn't Work
- ❌ Ground color looks gray/washed out despite attempts to brighten
- ❌ Overall visual result is "awful" (user feedback)

## Key Technical Changes
1. **Replaced PlaneMesh with CSGBox3D** (`scenes/battlefield/components/base_battlefield_3d.tscn:44-47`)
   - PlaneMesh wouldn't render due to backface culling/orientation issues
   - Using flat CSGBox3D (100x0.1x80) as ground instead

2. **Updated script reference** (`scripts/battlefield/base_battlefield_3d.gd:30`)
   - Changed from `MeshInstance3D` to `CSGBox3D` type

3. **Fixed shader compilation errors** (`shaders/ground_ao.gdshader`)
   - Added `varying vec3 world_pos` to pass position from vertex to fragment shader
   - Fixed `dot(vec3, vec2)` dimension mismatch on line 47

## Current Shader Parameters
```
ground_color = Vector3(0.4, 0.7, 0.35)  # Attempted bright green
ao_strength = 0.4
ao_radius = 15.0
edge_darkening = 0.2
directional_strength = 0.15
```

## Next Steps to Fix
1. **Color issue**: Ground appears gray despite bright green values. Possible causes:
   - AO/darkening too strong
   - Ambient lighting washing out color
   - Need different color space or approach

2. **Consider alternatives**:
   - Use texture instead of solid color
   - Adjust WorldEnvironment ambient lighting
   - Try different color values or disable some darkening effects

3. **Visual reference**: Look at Monster Chef / Mini Warriors Reborn ground rendering

## Files Modified
- `shaders/ground_ao.gdshader` (created, fixed)
- `scenes/battlefield/components/base_battlefield_3d.tscn` (CSGBox3D ground + shader)
- `scripts/battlefield/base_battlefield_3d.gd` (type update)
- `.claude/VISUAL_OVERHAUL_TODO.md` (tracking doc)

## Branch
`feature/baked-ao-ground`
