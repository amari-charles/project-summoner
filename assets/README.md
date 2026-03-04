# Assets Directory

This directory contains all visual and audio assets for Fateforged.

## Structure

```
assets/
├── battlefield/         # Battlefield environment assets
│   ├── backgrounds/     # Sky, horizon, and background layers
│   ├── props/           # Environmental decorations and props
│   └── bases/           # Player and enemy base structures
├── units/               # Character/unit sprites and animations
├── cards/               # Card-related visuals
│   ├── frames/          # Card frame/border graphics
│   └── art/             # Card illustration portraits
├── vfx/                 # Visual effects
│   ├── particles/       # Particle textures
│   └── effects/         # Effect sprites and animations
├── textures/            # General textures (legacy/utility)
├── tilesets/            # Tileset graphics (legacy)
└── dummy_v1/            # Placeholder/test sprites

## Asset Guidelines

### For Artists

See **[Art Asset Specifications](../docs/art/asset-specifications.md)** for:
- Detailed size and format requirements
- Color palette references
- Technical specifications
- Priority order for asset creation

### For Developers

**Adding New Assets:**
1. Place assets in appropriate subfolder
2. Follow naming convention: `lowercase_with_underscores.png`
3. Check import settings in Godot:
   - Painterly art: Filter=true, Mipmaps=true
   - Pixel art: Filter=false, Mipmaps=false
4. Update this README if adding new categories

## Current Status

| Category | Status | Notes |
|----------|--------|-------|
| Battlefield Backgrounds | 🔴 Placeholder | Using basic ColorRects |
| Bases | 🟡 Geometric | Polygon2D shapes, awaiting hand-drawn art |
| Units | 🔴 Placeholder | ColorRects only |
| Card Art | 🔴 None | Text-only cards |
| VFX | 🔴 Minimal | Basic projectiles only |

**Legend:**
- 🟢 Complete
- 🟡 In Progress / Temporary
- 🔴 Placeholder / Missing

## Priority Order

1. **Battlefield Environment** - Backgrounds and ground (sets mood)
2. **Base Structures** - Player and enemy bases (focal points)
3. **Units** - Character sprites (core gameplay)
4. **Card Art** - Card illustrations (UI polish)
5. **VFX** - Effects and particles (final polish)

See [Art Asset Specifications](../docs/art/asset-specifications.md) for detailed requirements.

## Import Presets

### Recommended Godot Import Settings

**Hand-Drawn/Painterly Art:**
```
Compress > Mode: Lossy
Compress > Quality: 0.9
Filter: true
Mipmaps: true (for backgrounds)
```

**Pixel Art:**
```
Compress > Mode: Lossless
Filter: false
Mipmaps: false
```

**UI Elements:**
```
Compress > Mode: Lossless
Filter: true
Mipmaps: false
```

## Resources

- [Visual Style References](../docs/design/visual-style-references.md) - Analysis of Mini Warriors & Cult of the Lamb
- [Color Palette](../scripts/shared/color_palette.gd) - Game color definitions
- [Art Specifications](../docs/art/asset-specifications.md) - Detailed asset requirements

---

*Last Updated: 2025-01-06*
