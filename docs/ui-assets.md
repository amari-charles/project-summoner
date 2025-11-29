# UI Asset Requirements

This document tracks all UI assets needed for the visual rework and their reuse locations across the game.

**Last Updated:** 2025-11-29

---

## 1. Button Assets

4 states (normal/hover/pressed/disabled) × 2-3 variants

| Asset | Reuse Locations |
|-------|-----------------|
| **Primary Button** | Main Menu, Game Mode Menu, Pause Menu, Reward Screen, Shop, Deck Builder, Collection, all modals |
| **Secondary Button** (smaller) | Back buttons, Close buttons, Tab buttons, filter toggles |
| **Icon Button** (circular/square) | Settings gear, Speed controls, Pause button |

**Screens using buttons:** 20+

---

## 2. Panel/Frame Assets

NinePatch textures for scaling

| Asset | Reuse Locations |
|-------|-----------------|
| **Modal Panel** | Card Detail, Card Level Up, Summoner Management, Pause Menu, Snapshot Manager, confirmations |
| **Content Panel** | Deck Builder panels, Collection grid, Campaign detail panel, Shop detail |
| **Card Frame** | CardWidget, CardVisual, OfferingCard, First Card Selection, Reward cards |
| **Summoner Frame** | SummonerCard, SummonerRosterItem portrait, SummonerIconWidget |
| **Tooltip/Info Panel** | Hover tooltips, stat displays, descriptions |

---

## 3. Progress Bar Assets

3 types, each needs frame + fill texture

| Asset | Reuse Locations |
|-------|-----------------|
| **HP Bar** | FloatingHPBar (all units), Summoner HP display |
| **Mana Bar** | ManaBar (battle HUD), Summoner stats display |
| **XP Bar** | Card Detail Modal, Card Level Up, Summoner Roster Item |

---

## 4. Element Icons

5 elements

| Asset | Reuse Locations |
|-------|-----------------|
| **Fire** | CardWidget badge, SummonerCard label, Summoner Icon, Collection filters |
| **Water** | Same as above |
| **Earth** | Same as above |
| **Air** | Same as above |
| **Neutral/Unknown** | Same as above |

---

## 5. Card Type Icons

4 types (partially exist in `/assets/icons/card_types/`)

| Asset | Status | Reuse Locations |
|-------|--------|-----------------|
| `sword.png` | Exists | CardWidget, CardVisual, Collection filters, Deck Builder |
| `bow.png` | Exists | Same |
| `tower.png` | Exists | Same |
| `wizard_hat.png` | Exists | Same |

---

## 6. Rarity Indicators

5 rarities - frame variants or glow effects

| Asset | Reuse Locations |
|-------|-----------------|
| **Common** | CardWidget, CardVisual, Card Detail, Offering Card, Reward Screen |
| **Uncommon** | Same |
| **Rare** | Same |
| **Epic** | Same |
| **Legendary** | Same |

---

## 7. UI Action Icons

| Asset | Reuse Locations |
|-------|-----------------|
| **Back Arrow** | All screens with back navigation (15+ screens) |
| **Close X** | All modals (6+ modals) |
| **Settings Gear** | Main Menu, Pause Menu |
| **Gold/Currency** | Shop, Reward Screen, Card Level Up, resource displays |
| **Mana Crystal** | CardWidget cost, ManaBar |
| **Plus/Add** | New Deck button, Add card buttons |
| **Trash/Delete** | Delete Deck, remove card |
| **Check/Confirm** | Confirmations, active indicators |
| **Lock** | Locked summoners, locked content |

---

## 8. Fonts

3 font families

| Font Type | Sizes | Reuse Locations |
|-----------|-------|-----------------|
| **Title/Display** | 48-56px | Screen titles, Victory/Defeat labels, modal headers |
| **Body/UI** | 14-32px | Buttons, labels, descriptions, stats |
| **Accent/Numbers** | Various | Mana costs, damage numbers, gold amounts |

---

## 9. Background Assets

| Asset | Reuse Locations |
|-------|-----------------|
| **Main Menu BG** | Main Menu (exists: `main_menu_background.png`) |
| **Dark Gradient/Pattern** | Game Mode Menu, Collection, Deck Builder, Campaign Map |
| **Battle Arena BG** | Battle scenes |
| **Modal Overlay** | All modals (semi-transparent dark) |

---

## 10. Summoner Portraits

5+ summoners, 3 size variants each

| Asset | Size | Reuse Locations |
|-------|------|-----------------|
| **Full Portrait** | 150x150+ | SummonerCard, Summoner Management Panel |
| **Icon Portrait** | 50x50 | SummonerIconWidget (4+ screens) |
| **List Portrait** | 60x60 | SummonerRosterItem |

---

## 11. Decorative Elements

| Asset | Reuse Locations |
|-------|-----------------|
| **Horizontal Separator** | Card Detail sections, modal dividers, info panels |
| **Corner Ornaments** | Panel decorations, card frames |
| **Glow/Highlight Effect** | Selected cards, active summoner, hover states |

---

## Priority Order

### Tier 1: Highest Impact (touch most screens)
- Button assets (3 variants × 4 states)
- Panel/Frame NinePatches (modal, content, card)
- Fonts (title, body, numbers)

### Tier 2: High Impact (core gameplay feel)
- Card frame + rarity variants
- Progress bars (HP, Mana, XP)
- Element icons (5)

### Tier 3: Medium Impact (polish)
- UI action icons (10+)
- Summoner portraits
- Separators/decorations

### Tier 4: Lower Impact (can iterate)
- Screen-specific backgrounds
- VFX/animation assets

---

## Summary

| Category | Asset Count | Screens Affected |
|----------|-------------|------------------|
| Buttons | 12 images | 20+ |
| Panels | 10 images | 15+ |
| Progress Bars | 6 images | 6 |
| Icons | 20+ images | 10+ |
| Fonts | 3 families | All |
| Portraits | 5+ images | 4 |

---

## Current Color Palette

For reference when creating assets:

**UI Backgrounds:**
- Dark primary: `#1a1a26` (0.1, 0.1, 0.15)
- Dark secondary: `#262633` (0.15, 0.15, 0.2)
- Black overlay: 60-85% opacity

**Accent Colors:**
- Gold/Currency: `#ffd933` (1, 0.85, 0.2)
- Active/Success: `#66e666` (0.4, 0.9, 0.4)
- Error: `#ff4d4d` (1, 0.3, 0.3)

**Element Colors:**
- Fire: `#e64d1a` (0.9, 0.3, 0.1)
- Water: `#3380e6` (0.2, 0.5, 0.9)
- Earth: `#996633` (0.6, 0.4, 0.2)
- Air: `#99ccff` (0.6, 0.8, 1)
- Neutral: `#b3b3b3` (0.7, 0.7, 0.7)

---

## Screen Inventory

All 26 UI screens for reference:

1. Main Menu
2. Game Mode Menu
3. Pause Menu
4. Campaign Map
5. Event Screen
6. Summoner Reveal
7. Deck Builder
8. Collection Screen
9. Card Detail Modal
10. Card Level Up Panel
11. Shop Screen
12. Reward Screen
13. Summoner Selection
14. First Card Selection
15. Summoner Management Panel
16. Dialogue Box (reusable component)
17. Hand UI
18. Mana Bar
19. Floating HP Bar
20. Card Widget
21. Card Visual
22. Offering Card
23. Summoner Card
24. Summoner Icon Widget
25. Summoner Roster Item
26. Snapshot Manager
