# Painterly Grass Base Texture - Usage Guide

## Overview

A procedural, shader-based grass terrain system that creates a hand-painted, mobile game aesthetic (similar to Mini Warriors Reborn / Monster Chef). The grass appears as soft, organic color variations rather than individual blades - perfect as a clean background for characters and buildings.

## Files Created

- **Shader**: `shaders/painterly_grass.gdshader`
- **Scene**: `scenes/battlefield/grass_base.tscn`

## Quick Start

### Adding to Your Scene

1. Open your battlefield/game scene in Godot
2. Add the grass base:
   - **Option A**: Instance the scene: `Scene > Instance Child Scene` → select `scenes/battlefield/grass_base.tscn`
   - **Option B**: Add manually: Add a `ColorRect` node, set anchors to full rect, and assign the shader material

3. **Position in hierarchy**:
   - Place the GrassBase as one of the first children (before characters/buildings)
   - This ensures it renders behind everything else

4. **For 2.5D/3D games**:
   - Place the GrassBase in a `CanvasLayer` with a low layer value (e.g., -10)
   - Or add it directly to the `UI` node in your scene hierarchy

### Basic Setup Example

```
YourGameScene
├─ CanvasLayer (layer = -10)  # Background layer
│  └─ GrassBase (instance of grass_base.tscn)
├─ Game3D (Node3D with camera, units, etc.)
└─ UI (CanvasLayer with HUD, cards, etc.)
```

## Customization Guide

All parameters can be adjusted in the Inspector under `Material > Shader Parameters`.

### Color Palette

Change these to adjust the grass colors:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `dark_grass` | #5e8a32 | Darkest green (shadows/depth areas) |
| `mid_grass` | #75b441 | Main grass color (base) |
| `light_grass` | #9ddc5c | Lighter green (highlights) |
| `highlight_grass` | #b9e675 | Brightest yellow-green (sun-touched areas) |
| `tint_color` | White | Global color multiplier for biome variations |

**Examples:**
- **Darker grass**: Reduce RGB values on all colors by 20-30%
- **More yellowish**: Increase `highlight_grass` intensity, reduce `tint_color` blue channel
- **Fire biome**: Set `tint_color` to reddish (e.g., `Color(1.0, 0.6, 0.4)`)
- **Corrupted biome**: Set `tint_color` to purple (e.g., `Color(0.8, 0.5, 1.0)`)

### Noise Layers

The shader uses 3 noise layers for depth and variation:

#### Primary Layer (Large Patches)
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `primary_scale` | 2.0 | 0.1-10.0 | Size of main color patches (lower = bigger patches) |
| `primary_intensity` | 0.7 | 0.0-1.0 | How much primary layer affects final color |

**Tip**: This is your main "painterly blob" layer. Lower scale = larger brush strokes.

#### Secondary Layer (Medium Splotches)
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `secondary_scale` | 5.0 | 0.1-20.0 | Size of medium detail patches |
| `secondary_intensity` | 0.4 | 0.0-1.0 | Influence of secondary layer |

**Tip**: Adds visual interest and prevents monotony.

#### Tertiary Layer (Subtle Texture)
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `tertiary_scale` | 12.0 | 0.1-30.0 | Size of fine details |
| `tertiary_intensity` | 0.2 | 0.0-1.0 | Influence of subtle variations |

**Tip**: Very subtle. Adds a "canvas texture" feel.

### Visual Controls

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `contrast` | 1.0 | 0.0-2.0 | Color variation strength (higher = more contrast) |
| `color_spread` | 0.8 | 0.1-1.0 | How much of the color ramp to use (0.5-1.0 recommended) |
| `animation_speed` | 0.0 | 0.0-1.0 | Gentle wind/movement effect (0 = static) |

## Common Adjustments

### Make Patches Larger/Smaller

**Larger patches (less busy):**
- Decrease `primary_scale` to 1.0-1.5
- Decrease `secondary_scale` to 3.0-4.0
- Decrease `tertiary_scale` to 8.0-10.0

**Smaller patches (more detail):**
- Increase `primary_scale` to 3.0-4.0
- Increase `secondary_scale` to 7.0-10.0
- Increase `tertiary_scale` to 15.0-20.0

### Increase/Decrease Contrast

**More dramatic variation:**
- Increase `contrast` to 1.3-1.5
- Increase `primary_intensity` to 0.8-0.9
- Increase `color_spread` to 0.9-1.0

**More subtle/flat:**
- Decrease `contrast` to 0.6-0.8
- Decrease intensity values
- Decrease `color_spread` to 0.5-0.6

### Change Overall Color Theme

**Option 1: Adjust palette colors directly**
- Change the 4 grass color parameters

**Option 2: Use tint_color**
- Keep default grass colors
- Set `tint_color` to your desired hue
  - Fire: `Color(1.0, 0.7, 0.5)`
  - Ice: `Color(0.7, 0.9, 1.0)`
  - Shadow: `Color(0.6, 0.6, 0.7)`

**Option 3: Script-based (for dynamic biomes)**
```gdscript
# In your battlefield setup script
@onready var grass_material: ShaderMaterial = $GrassBase.material

func set_biome(biome_type: String) -> void:
    match biome_type:
        "fire":
            grass_material.set_shader_parameter("tint_color", Color(1.0, 0.6, 0.4))
        "nature":
            grass_material.set_shader_parameter("tint_color", Color(1.0, 1.0, 1.0))
        "corrupted":
            grass_material.set_shader_parameter("tint_color", Color(0.8, 0.5, 1.0))
```

## Tileability

**Current implementation:** Not perfectly seamless/tileable, but visually soft enough that seams are hard to notice.

The noise functions used create organic, non-repeating patterns. If you need to tile the grass (for very wide battlefields):

1. The ColorRect is set to fill the entire viewport by default
2. For infinite scrolling, you may need to adjust UV coordinates or use multiple instances
3. The low-frequency noise makes seams barely perceptible in most cases

**Future improvement**: Could be made perfectly tileable by using `NoiseTexture2D` with seamless mode, but current implementation prioritizes real-time flexibility.

## Technical Details

### Why Shader-Based?

**Advantages:**
- ✅ Real-time parameter tweaking in editor
- ✅ No texture memory overhead
- ✅ Infinite resolution (scales to any screen size)
- ✅ Can animate/pulse for magical effects
- ✅ Easy biome variations via `tint_color`

**Alternative (if needed):**
If you need to bake to a texture for performance:
1. Run your game with the grass visible
2. Screenshot or use `get_viewport().get_texture().get_image()`
3. Save as PNG: `image.save_png("res://assets/grass_baked.png")`

### Performance

Very lightweight - runs on a single fragment shader with simple noise functions. Should perform well even on mobile devices.

If you experience performance issues:
- Reduce the number of octaves in `fbm()` calls in the shader
- Simplify to 2 noise layers instead of 3 (remove tertiary)

## Integration with Grass Tufts

This shader creates the **base terrain**. You mentioned wanting to add separate 2D sprite grass tufts on top.

Suggested workflow:
1. Use this GrassBase as the foundational layer
2. Place grass tuft sprites at a higher z-index/layer
3. Use the base grass colors to inform your tuft sprite palette for cohesion

Example hierarchy:
```
CanvasLayer (layer = -10)
├─ GrassBase (this shader)
└─ GrassTufts (Node2D with sprite children at various positions)
```

## Troubleshooting

**Grass appears too uniform/boring:**
- Increase `contrast` to 1.2-1.5
- Increase `primary_intensity` to 0.8
- Ensure `color_spread` is at least 0.7

**Grass looks too noisy/busy:**
- Decrease all `_scale` values
- Decrease `contrast` to 0.7-0.8
- Decrease secondary/tertiary intensities

**Colors don't match my art style:**
- Use the color picker to sample colors from your existing art
- Update the 4 grass color parameters to match
- Or use `tint_color` to shift the entire palette

**Grass doesn't fill screen:**
- Check that ColorRect anchors are set to full (0,0,1,1)
- Ensure the GrassBase node is in a CanvasLayer or UI layer
- For 3D games, make sure it's in the UI hierarchy, not 3D space

## Example Presets

Copy these into the Inspector for different looks:

### **Classic Mobile Game**
```
primary_scale: 2.0
secondary_scale: 5.0
contrast: 1.0
color_spread: 0.8
```

### **Soft Watercolor**
```
primary_scale: 1.5
secondary_scale: 3.5
tertiary_scale: 8.0
contrast: 0.7
color_spread: 0.6
```

### **High Contrast Comic**
```
primary_scale: 2.5
secondary_scale: 6.0
tertiary_scale: 14.0
contrast: 1.5
color_spread: 0.9
```

### **Subtle Realistic**
```
primary_scale: 3.0
secondary_scale: 8.0
tertiary_scale: 16.0
contrast: 0.8
primary_intensity: 0.6
secondary_intensity: 0.3
tertiary_intensity: 0.15
```

## Next Steps

1. **Test it**: Open `grass_base.tscn` in Godot and preview
2. **Tweak**: Adjust parameters in the Inspector until it matches your vision
3. **Integrate**: Add to your main battlefield scene
4. **Iterate**: Fine-tune based on how it looks with your units/buildings on top

Good luck with your game! 🎮
