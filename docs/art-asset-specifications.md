# Art Asset Specifications

This document specifies the technical requirements for hand-drawn/painted art assets for Project Summoner.

---

## General Guidelines

### Art Style
- **Medium:** Hand-drawn/painted (digital or traditional scanned)
- **Style:** Painterly with clear silhouettes (inspired by Cult of the Lamb's approach)
- **Color Philosophy:** Muted base environments with vibrant character/effect accents
- **Resolution:** Create at 2x target size for quality, then scale down
- **Format:** PNG with transparency where needed

### Technical Requirements
- **DPI:** 150-300 DPI for crisp results
- **Color Mode:** RGB
- **Bit Depth:** 8-bit per channel minimum
- **Alpha Channel:** Include for any non-rectangular elements
- **File Naming:** Use lowercase with underscores (e.g., `player_base_main.png`)

---

## Priority 1: Battlefield Environment

### Background Sky Layer
**Purpose:** Atmospheric backdrop creating mood

**Specifications:**
- **Dimensions:** 1920x540px (full width, upper half of screen)
- **Layers Needed:**
  1. Deep sky (top): Dark purple-blue (#1a1528)
  2. Horizon glow (bottom 140px): Warm twilight (#4a3838)
  3. Optional: Stars, distant mountains, mystical elements
- **Style Notes:**
  - Highly muted to not compete with gameplay
  - Gradual transitions between layers
  - Painterly brushwork acceptable
  - Can be soft-focused for depth

**Reference Colors:** `ColorPalette.SKY_DARK`, `ColorPalette.SKY_HORIZON`

**File Deliverable:** `background_sky.png`

---

### Ground/Battlefield Floor
**Purpose:** Main playing surface where units move

**Specifications:**
- **Dimensions:** 1920x540px (full width, lower half) OR tileable 256x256px
- **Approach:** Either single large image or repeatable tile
- **Colors:** Earth tones (#4a3828 to #8b7355)
- **Style Notes:**
  - Subtle texture (grass, dirt, mystical runes)
  - Not too busy - must allow unit visibility
  - Can have subtle grid or pathway suggestions
  - Consider battle-worn or magical theme

**Reference Colors:** `ColorPalette.GROUND_DARK/MID/LIGHT`

**File Deliverable:** `ground_surface.png` or `ground_tile.png`

---

### Environmental Props (Optional Phase 1)
**Purpose:** Add life and atmosphere to battlefield edges

**Specifications:**
- **Placement:** Edges of battlefield (foreground/background)
- **Examples:**
  - Ancient stones/pillars
  - Glowing crystals
  - Mystical trees
  - Ruined structures
- **Style Notes:**
  - Keep to edges to not obstruct gameplay
  - Can be more detailed than background
  - Use depth (darker = further back)

**Reference:** See `docs/game-visual-style-references.md` - Cult of the Lamb environmental approach

---

## Priority 2: Base Structures (Future Phase)

### Player Base
**Purpose:** Replace current geometric base with hand-drawn structure

**Current Placeholder:** Polygon-based structure at 60x200px
**Recommended Size:** 120x320px (2x for detail)
**Theme:** Warm, welcoming, magical academy/tower aesthetic
**Colors:** Gold/bronze from `ColorPalette.PLAYER_ZONE_*`

**Elements to Include:**
- Main tower/structure body
- Distinct "roof" or top section
- Visible HP bar integration area
- Player team symbol area (◆)
- Magical/mystical details

---

### Enemy Base
**Purpose:** Replace current geometric base with hand-drawn structure

**Current Placeholder:** Polygon-based structure at 60x200px
**Recommended Size:** 120x320px (2x for detail)
**Theme:** Cool, ominous, dark fortress aesthetic
**Colors:** Steel blue from `ColorPalette.ENEMY_ZONE_*`

**Elements to Include:**
- Main tower/fortress body
- Distinct "roof" or top section
- Visible HP bar integration area
- Enemy team symbol area (▲)
- Dark/ominous details

---

## Priority 3: Units (Future Phase)

### Unit Specifications
**Purpose:** Replace colored rectangles with character art

**General Requirements:**
- **Size Range:** 48-96px tall (depending on unit type)
- **Format:** Sprite sheets OR individual PNGs
- **Style:** Clear silhouettes, simple shapes, bold outlines
- **Colors:** Element-based with team color accents

### Unit Categories Needed

#### 1. Warrior (Melee Tank)
- **Silhouette:** Wide, rounded, grounded stance
- **Height:** ~64px
- **Theme:** Heavy armor, shield, defensive posture
- **Element Variants:** Fire, Water, Nature, Storm, Earth

#### 2. Archer (Ranged)
- **Silhouette:** Tall, thin, weapon prominent
- **Height:** ~56px
- **Theme:** Light armor, bow/crossbow visible
- **Element Variants:** Fire, Water, Nature, Storm, Earth

#### 3. Wall/Defender
- **Silhouette:** Very wide, static, fortified
- **Height:** ~80px (taller than units)
- **Theme:** Stone/crystal barrier, imposing
- **Element Variants:** Primarily Earth/Stone

**Animation Notes:**
- Idle pose only for Phase 1
- Attack/death animations can be added later
- Sprites should be facing right (flipped in code for left-facing)

---

## Priority 4: Card Art (Future Phase)

### Card Frame
**Purpose:** Visual container for card information

**Specifications:**
- **Dimensions:** 160x240px (2x the 80x120px display size)
- **Elements:**
  - Decorative border/frame
  - Art window (centered, ~100x100px)
  - Mana cost area (top corner)
  - Name plate area (bottom)
- **Variants:** One per element type (5 total) or universal

---

### Card Illustrations
**Purpose:** Visual representation of each card's summon/spell

**Specifications:**
- **Dimensions:** 100x100px (fits in card art window)
- **Style:** Portrait-style illustration of unit/effect
- **Approach:** Painterly, focusing on character/effect essence
- **Count Needed:** 5-10 cards (start with main units)

**Cards to Illustrate (Priority Order):**
1. Warrior
2. Archer
3. Wall
4. Fireball
5. Training Dummy (tutorial unit)

---

## Asset Organization Structure

```
assets/
├── battlefield/
│   ├── backgrounds/
│   │   ├── sky_layer.png
│   │   ├── horizon_layer.png
│   │   └── ground_surface.png (or ground_tile.png)
│   ├── props/
│   │   ├── crystal_01.png
│   │   ├── pillar_01.png
│   │   └── ...
│   └── bases/
│       ├── player_base.png
│       └── enemy_base.png
├── units/
│   ├── warrior/
│   │   ├── warrior_fire_idle.png
│   │   ├── warrior_water_idle.png
│   │   └── ...
│   ├── archer/
│   │   └── ...
│   └── wall/
│       └── ...
├── cards/
│   ├── frames/
│   │   ├── card_frame_fire.png
│   │   └── ...
│   └── art/
│       ├── warrior_portrait.png
│       ├── archer_portrait.png
│       └── ...
└── vfx/
    ├── particles/
    └── effects/
```

---

## Import Settings (Godot 4)

### For Pixel-Perfect Art
```
Compress: Lossless
Filter: false (Nearest)
Mipmaps: false
```

### For Painterly/Hand-Drawn Art
```
Compress: Lossy (Quality: 0.9)
Filter: true (Linear)
Mipmaps: true (for distant elements)
```

### For Transparent Assets
```
Alpha: Premultiply Alpha: true
```

---

## Workflow Recommendations

### Phase 1 (Current): Battlefield Environment
1. Start with sky background (sets the mood)
2. Create ground surface (defines play space)
3. Add 2-3 environmental props if desired
4. Test in-game to ensure colors/contrast work

### Phase 2: Base Replacements
1. Player base illustration
2. Enemy base illustration
3. Integrate with existing HP bar system

### Phase 3: Units
1. Start with one unit type (Warrior recommended)
2. Create element variants
3. Test in battle for scale/visibility
4. Proceed to other unit types

### Phase 4: Card Art
1. Design card frame template
2. Create card portraits for existing units
3. Apply to card widget system

---

## Testing Checklist

After creating any asset:
- [ ] Import into Godot and check visual quality
- [ ] Test at actual game resolution (1920x1080)
- [ ] Verify colors match/complement ColorPalette
- [ ] Check visibility/contrast against background
- [ ] Ensure doesn't obstruct gameplay elements
- [ ] Confirm file size is reasonable (<500KB per asset)

---

## Color Palette Reference

Quick reference for matching game colors:

**Environment:**
- Sky: #1a1528 (deep purple) to #4a3838 (warm horizon)
- Ground: #4a3828 (dark) to #8b7355 (light)

**Player Territory:**
- Primary: #d4a574 (warm gold)
- Accent: #f5c75c (bright gold)

**Enemy Territory:**
- Primary: #5a7b8c (steel blue)
- Accent: #7a9bb0 (bright steel)

**Elements:**
- Fire: #e84a3f / #ff6b4a
- Water: #4a9eff / #6bb6ff
- Nature: #5fc75c / #7ed957
- Storm: #a78bff / #c4a3ff
- Earth: #c4834a / #d9a574

Full palette available in: `resources/visual/color_palette.gd`

---

*Document Version: 1.0*
*Last Updated: 2025-01-06*
