# Art Commission Brief

Non-summon art assets needed for Fateforged. This document is organized by asset category for studio reference.

**Game:** Fateforged
**Genre:** Card-based strategy with real-time battles
**Art Style:** Painterly/hand-drawn with clear silhouettes (Cult of the Lamb-inspired)
**Resolution:** 1920x1080 target

---

## 1. UI Elements

### Buttons

3 variants × 4 states each = 12 images

| Variant | Description | States Needed |
|---------|-------------|---------------|
| Primary Button | Main action buttons (Start, Confirm, Play) | Normal, Hover, Pressed, Disabled |
| Secondary Button | Smaller actions (Back, Cancel, Close) | Normal, Hover, Pressed, Disabled |
| Icon Button | Circular/square for icons (Settings, Pause) | Normal, Hover, Pressed, Disabled |

**Used in:** 20+ screens including Title, Pause Menu, Shop, Deck Builder, all modals

---

### Panels & Frames

NinePatch textures for flexible scaling

| Asset | Size | Description |
|-------|------|-------------|
| Modal Panel | Variable (min 400x300) | Overlay dialogs, confirmations, detail views |
| Content Panel | Variable | Main content containers, list backgrounds |
| Card Frame | 160x240px | Decorative frame for cards (includes art window, mana cost area, name plate) |
| Summoner Frame | 150x150px | Portrait frame for summoner displays |
| Tooltip Panel | Variable (min 200x100) | Hover info, stat displays |

**Used in:** Card Detail, Deck Builder, Collection, Shop, all modals

---

### Progress Bars

3 types, each needs frame + fill texture

| Bar Type | Dimensions | Colors |
|----------|------------|--------|
| HP Bar | ~100x12px | Red/green fill, dark frame |
| Mana Bar | ~200x16px | Blue fill, dark frame |
| XP Bar | ~150x8px | Gold/yellow fill, dark frame |

**Used in:** Battle HUD, Summoner stats, Card progression

---

## 2. Icons

### Element Icons

5 icons representing game elements

| Element | Color Reference |
|---------|-----------------|
| Fire | #e64d1a (orange-red) |
| Water | #3380e6 (blue) |
| Earth | #996633 (brown) |
| Air | #99ccff (light blue) |
| Neutral | #b3b3b3 (gray) |

**Size:** 32x32px and 64x64px variants
**Used in:** Cards, Summoner displays, Collection filters, element badges

---

### Action Icons

~10 icons for UI navigation and actions

| Icon | Description |
|------|-------------|
| Back Arrow | Navigation back |
| Close X | Modal/panel close |
| Settings Gear | Settings access |
| Gold Coin | Currency display |
| Mana Crystal | Card cost display |
| Plus/Add | Create new, add item |
| Trash/Delete | Remove/delete |
| Checkmark | Confirm, active state |
| Lock | Locked content |
| Hamburger Menu | Menu toggle |

**Size:** 24x24px and 48x48px variants
**Used in:** All screens with navigation, resource displays, list actions

---

### Card Type Icons

4 icons (placeholders exist, may need polish)

| Icon | Represents |
|------|------------|
| Sword | Warrior/Melee summons |
| Bow | Archer/Ranged summons |
| Tower | Wall/Defender summons |
| Wizard Hat | Spells |

**Size:** 24x24px
**Status:** Basic versions exist at `assets/icons/card_types/`

---

## 3. Card Assets

### Card Frame

| Spec | Value |
|------|-------|
| Dimensions | 160x240px (2x display size) |
| Art Window | ~100x100px centered area |
| Mana Cost Area | Top corner |
| Name Plate | Bottom area |

**Rarity Variants Needed:**

| Rarity | Frame Treatment |
|--------|-----------------|
| Common | Simple, muted |
| Uncommon | Subtle accent |
| Rare | Visible glow/decoration |
| Epic | Prominent styling |
| Legendary | Most ornate, golden accents |

---

### Card Illustrations

Portrait-style art for card display

| Spec | Value |
|------|-------|
| Dimensions | 100x100px |
| Style | Painterly, character/effect focus |
| Background | Transparent or element-themed |

**Priority Cards:**
1. Warrior (melee summon)
2. Archer (ranged summon)
3. Wall/Defender
4. Fireball spell
5. Training Dummy (tutorial)

---

## 4. Summoner Portraits

5 summoner characters, each needs 3 size variants

### Characters

| Summoner | Element | Brief Description |
|----------|---------|-------------------|
| Cole | Fire | Passionate, bold |
| Kai-Ise | Water | Calm, flowing |
| Mei | Earth | Grounded, steady |
| Selene | Air | Free-spirited, light |
| Teo | Neutral | Balanced, wise |

### Size Variants

| Size | Dimensions | Usage |
|------|------------|-------|
| Full Portrait | 150x150px | Summoner cards, management panel |
| Icon | 50x50px | In-game HUD, quick selection |
| List | 60x60px | Roster lists, small displays |

**Total:** 5 characters × 3 sizes = 15 portrait images

---

## 5. Battlefield & Environment

### Sky Background

| Spec | Value |
|------|-------|
| Dimensions | 1920x540px |
| Purpose | Atmospheric backdrop (upper screen half) |
| Colors | Deep purple-blue (#1a1528) to warm horizon (#4a3838) |
| Style | Muted, gradient, optional stars/distant elements |

---

### Ground Surface

| Option | Dimensions |
|--------|------------|
| Single Image | 1920x540px |
| Tileable | 256x256px |

| Spec | Value |
|------|-------|
| Purpose | Main battlefield floor |
| Colors | Earth tones (#4a3828 to #8b7355) |
| Style | Subtle texture (grass, dirt, mystical elements) |
| Note | Must not compete with unit visibility |

---

### Base Structures

| Base | Theme | Colors |
|------|-------|--------|
| Player Base | Warm, magical tower/academy | Gold/bronze (#d4a574) |
| Enemy Base | Cool, ominous fortress | Steel blue (#5a7b8c) |

| Spec | Value |
|------|-------|
| Dimensions | 120x320px each |
| Elements | Main structure, distinct roof, HP bar area, team symbol area |

---

### Environmental Props (Optional)

Decorative elements for battlefield edges

| Prop Types | Notes |
|------------|-------|
| Ancient stones/pillars | Background placement |
| Glowing crystals | Accent pieces |
| Mystical trees | Edge decoration |
| Ruined structures | Atmospheric |

**Placement:** Edges only, must not obstruct gameplay

---

## 6. VFX & Effects

### Spell Effect Sprites

Per-element spell animations (sprite sheets or individual frames)

| Element | Effect Style |
|---------|--------------|
| Fire | Flames, explosions, embers |
| Water | Splashes, waves, droplets |
| Earth | Rocks, dust, cracks |
| Air/Wind | Swirls, gusts, feathers |
| Lightning | Bolts, sparks, arcs |
| Ice | Crystals, frost, shards |
| Life/Nature | Leaves, vines, blooms |
| Holy | Light rays, halos, sparkles |
| Death/Dark | Shadows, skulls, decay |

**Note:** Fireball animation exists (6 frames) at `assets/textures/vfx/fireball/`

---

### Projectiles

| Projectile | Size | Notes |
|------------|------|-------|
| Arrow | 32x32px | Exists at `assets/textures/projectiles/` |
| Magic bolt | 32x32px | Per-element variants |
| Spell orb | 48x48px | Glowing effect |

---

### Status Indicators

Visual markers for buffs/debuffs

| Type | Examples |
|------|----------|
| Buff indicators | Shield, speed boost, damage up |
| Debuff indicators | Slow, poison, weakness |
| Area markers | Target zones, spell ranges |

---

## 7. Campaign & Events

### Map Elements

| Asset | Description |
|-------|-------------|
| Location markers | Node graphics for map navigation |
| Path indicators | Lines/roads connecting locations |
| Difficulty icons | Easy/Medium/Hard indicators |
| Event type icons | Battle, shop, treasure, mystery |

---

### Event Illustrations (If Needed)

| Type | Size | Notes |
|------|------|-------|
| Event backgrounds | Variable | Scene-setting imagery |
| NPC portraits | 100x100px | For dialogue/events |
| Reward displays | Variable | Treasure, card reveals |

---

## 8. Decorative Elements

### UI Decoration

| Asset | Usage |
|-------|-------|
| Horizontal separator | Section dividers in panels |
| Corner ornaments | Panel decoration, card frames |
| Glow/highlight effect | Selection states, hover feedback |
| Border trim | Screen edge decoration |

---

### Fonts

3 font families needed

| Font Type | Sizes | Usage |
|-----------|-------|-------|
| Title/Display | 48-56px | Screen titles, Victory/Defeat, modal headers |
| Body/UI | 14-32px | Buttons, labels, descriptions, stats |
| Accent/Numbers | Various | Mana costs, damage numbers, gold amounts |

**Style:** Should match painterly aesthetic, readable at small sizes

---

## Technical Specifications

### General Requirements

| Spec | Value |
|------|-------|
| Format | PNG with transparency |
| DPI | 150-300 DPI |
| Color Mode | RGB, 8-bit per channel |
| Naming | lowercase_with_underscores.png |

### Style Guidelines

- Painterly/hand-drawn with clear silhouettes
- Muted environments, vibrant character/effect accents
- Create at 2x target size, scale down for quality

---

## Color Palette Reference

### UI Colors
- Dark primary: `#1a1a26`
- Dark secondary: `#262633`
- Gold/Currency: `#ffd933`
- Active/Success: `#66e666`
- Error: `#ff4d4d`

### Element Colors
- Fire: `#e64d1a`
- Water: `#3380e6`
- Earth: `#996633`
- Air: `#99ccff`
- Neutral: `#b3b3b3`

### Environment Colors
- Sky (dark): `#1a1528`
- Horizon: `#4a3838`
- Ground (dark): `#4a3828`
- Ground (light): `#8b7355`

### Team Colors
- Player: `#d4a574` (warm gold)
- Enemy: `#5a7b8c` (steel blue)

---

## Asset Summary

| Category | Estimated Count |
|----------|-----------------|
| Buttons | 12 images |
| Panels/Frames | ~10 images |
| Progress Bars | 6 images |
| Element Icons | 10 images (2 sizes) |
| Action Icons | 20 images (2 sizes) |
| Card Type Icons | 4 images |
| Card Frames | 5 images (rarities) |
| Card Illustrations | 5+ images |
| Summoner Portraits | 15 images |
| Battlefield Assets | 4-6 images |
| VFX/Effects | Variable (sprite sheets) |
| Campaign Assets | Variable |
| Decorative | ~10 images |
| Fonts | 3 families |

**Estimated Total:** 100-150 individual assets (excluding VFX animation frames)

---

*Document Version: 1.0*
