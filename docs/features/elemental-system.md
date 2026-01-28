# Elemental System - Fateforged

This document defines the elemental structure for Fateforged. It organizes all known elements into categories that balance worldbuilding clarity with gameplay purpose.

---

## Table of Contents

1. [Overview](#overview)
2. [Element Types](#element-types)
3. [Card Flavors (Variants & Hybrids)](#card-flavors-variants--hybrids)
4. [Technical Implementation](#technical-implementation)
5. [Design Guidelines](#design-guidelines)

---

## Overview

The elemental system in Fateforged consists of **13 distinct element types** organized into tiers based on their narrative and gameplay roles. Each tier serves a specific purpose in campaign progression, worldbuilding, and mechanical design.

**Important:** Variants (Ash, Coldfire, Mist, Smoke, Crystal, etc.) and Hybrids (Magma) are **NOT** separate element types. They are thematic card flavors that use their parent element's affinity.

---

## Element Types

### Core Elements (4)

The four primary campaign elements — the foundation of the world and the player's main path of progression. Each Core Element will receive a full campaign at launch.

| Element | Description | Gameplay Identity |
|---------|-------------|-------------------|
| **Fire** | Embodies vitality, passion, and transformation. | Erratic, burst damage, powerful up front |
| **Water** | Symbolizes adaptability, empathy, and memory. | Nurturing, healing, flowy |
| **Wind** | Represents motion, freedom, and volatility. | Fast, elusive, flowy |
| **Earth** | Stands for stability, structure, and endurance. | Strong, slow, enduring |

**Design Purpose:** Core Elements are the player's first exposure to the elemental system. Each offers a distinct aesthetic, mechanical identity, and story tone. These elements define early strategy diversity and form the backbone of deck and faction identity.

**Technical Implementation:**
```gdscript
# Core element constants
ElementTypes.FIRE    # "fire"
ElementTypes.WATER   # "water"
ElementTypes.WIND    # "wind"
ElementTypes.EARTH   # "earth"
```

---

### Outer Elements (5)

Outer Elements exist alongside the Core but are not part of the initial campaign set. They enrich the world and add complexity to future updates, advanced content, or special units.

| Element | Description |
|---------|-------------|
| **Lightning** | The element of pure energy, speed, and precision. Represents intensity and insight. |
| **Shadow** | The unseen, deceptive force. Governs secrecy, illusion, and reflection. |
| **Poison** | The element of corruption and persistence. Represents decay, mutation, and inevitability. |
| **Life** | The element of growth, restoration, and empathy. Represents vitality and connection. |
| **Death** | The element of endings and transition. Represents inevitability and silence. |

**Design Purpose:** Outer Elements expand the world beyond the Core campaigns. They are ideal for future story arcs, late-game unlocks, or unique event cards. They provide design flexibility without overextending launch scope.

**Technical Implementation:**
```gdscript
# Outer element constants
ElementTypes.LIGHTNING  # "lightning"
ElementTypes.SHADOW     # "shadow"
ElementTypes.POISON     # "poison"
ElementTypes.LIFE       # "life"
ElementTypes.DEATH      # "death"
```

---

### Occultist Element (1)

The Occultist element stands alone. It is the systemic inversion of all other forces — corruption, manipulation, and forbidden knowledge. Occultist units and powers disrupt or nullify elemental laws, often serving as antagonistic or endgame threats.

**Design Purpose:** Occultist acts as the counterweight to the entire system. It introduces asymmetry and unpredictability, both narratively (as the enemy domain) and mechanically (through corruption and inversion effects).

**Technical Implementation:**
```gdscript
# Occultist constant
ElementTypes.OCCULTIST  # "occultist"
```

---

### Elevated Elements (3)

Elevations are fundamental transformations — not stronger forms, but entirely new existential states. Only certain elements can elevate because true elevation requires an element to transcend its natural identity.

| Base Element | Elevated Form | Nature of Change | Description |
|--------------|---------------|------------------|-------------|
| **Fire** | **Holy** | Physical → Moral / Sacred | Flame becomes sanctity. Energy with purpose, divine intention, and cleansing light. |
| **Water** | **Ice** | Mutable → Immutable | Flow becomes control and stillness. Preservation through perfection. |
| **Earth** | **Metal** | Organic → Forged | Matter learns to shape itself — civilization and artifice emerge. |

**Design Purpose:** Elevated elements define world mythology and serve as long-term expansion potential. They represent philosophical transformation, not progression. Only certain forces can reach this state.

**Technical Implementation:**
```gdscript
# Elevated element constants
ElementTypes.HOLY    # "holy"   (Fire → Holy)
ElementTypes.ICE     # "ice"    (Water → Ice)
ElementTypes.METAL   # "metal"  (Earth → Metal)

# Check elevation relationships
if ElementTypes.can_elevate(ElementTypes.FIRE):
    var elevated_form = ElementTypes.get_elevation(ElementTypes.FIRE)  # Returns "holy"
```

---

## Card Flavors (Variants & Hybrids)

### Variants (Empowered Subtypes)

Variants are slightly stronger, reward-tier versions of base elemental cards. They appear as rare campaign rewards, achievement bonuses, or post-battle upgrades. **Variants maintain their parent element's typing** but feature enhanced effects or unique passives.

| Element | Variants | Status |
|---------|----------|--------|
| Fire | **Ash**, **Coldfire**, **Star** | Exploring options |
| Water | **Mist** | Confirmed |
| Wind | **Smoke**, **Tempest** | Exploring options |
| Earth | **Crystal** | Confirmed |

**Important:** Variants are **NOT separate element types**. They are card name/flavor only.

**Example:**
```gdscript
# Card: "Solar Warrior" (Fire variant)
var card_def = {
    "card_name": "Solar Warrior",  # Variant name in title
    "categories": {
        "elemental_affinity": "fire"  # Uses parent element (NOT "solar")
    }
}

# The modifier system sees this as a Fire card
var modifiers = ModifierSystem.get_modifiers_for("unit", {"elemental_affinity": "fire"}, {})
```

**Design Purpose:** Variants extend replayability and offer progression incentives. They make campaigns feel rewarding while preserving overall balance by staying within the same elemental synergy framework.

---

### Hybrids (Confirmed Fusions)

Hybrids represent natural fusions between two elements. Each hybrid embodies a distinct metaphysical theme that can exist narratively or mechanically.

| Hybrid Name | Composition | Description |
|-------------|-------------|-------------|
| **Magma** | Fire + Earth | Molten fury and grounded destruction. A balance of eruption and stability. |

**Important:** Hybrids are **NOT separate element types**. They pick one parent's elemental affinity for modifier matching.

**Example:**
```gdscript
# Card: "Magma Golem" (Fire+Earth hybrid)
var card_def = {
    "card_name": "Magma Golem",     # Hybrid name in title
    "categories": {
        "elemental_affinity": "fire"  # Chooses one parent (fire OR earth, not both)
    }
}

# Receives Fire bonuses (not Earth bonuses in this case)
var modifiers = ModifierSystem.get_modifiers_for("unit", {"elemental_affinity": "fire"}, {})
```

**Design Purpose:** Hybrids will appear sparingly. They expand creative card and unit design space without overcomplicating the elemental taxonomy.

---

## Technical Implementation

### Element Constants

All element types are defined in `scripts/core/element_types.gd` as a global autoload (`ElementTypes`).

**Usage:**
```gdscript
# Reference element constants
var element = ElementTypes.FIRE

# Validation
if ElementTypes.is_valid(element):
    print(ElementTypes.get_display_name(element))  # "Fire"

# Check element category
if ElementTypes.is_core(element):
    print("This is a core element")

# Get elevation
if ElementTypes.can_elevate(ElementTypes.FIRE):
    var elevated = ElementTypes.get_elevation(ElementTypes.FIRE)  # "holy"
```

### Card Integration

Cards use the `elemental_affinity` category to specify their element:

```gdscript
# In CardCatalog
_catalog["fireball"] = {
    "catalog_id": "fireball",
    "card_name": "Fireball",
    # ... other fields ...
    "categories": {
        "elemental_affinity": ElementTypes.FIRE  # Use constant for type safety
    }
}
```

### Modifier System Integration

The modifier system filters by elemental affinity:

```gdscript
# Hero provides Fire bonus
var modifier = {
    "source": "fire_hero",
    "conditions": {"elemental_affinity": ElementTypes.FIRE},
    "stat_mults": {"attack_damage": 1.1}
}

# Card requests modifiers
var categories = {"elemental_affinity": ElementTypes.FIRE}
var modifiers = ModifierSystem.get_modifiers_for("unit", categories, {})
# Returns modifiers that match Fire affinity
```

### Element Interactions (Combat Matchups)

When units deal damage, a multiplier is applied based on the attacker's element vs the defender's element. This is handled by `ElementMatchups` in `scripts/csharp/Constants/ElementMatchups.cs`.

**Current Matchups (Core Cycle):**

| Attacker | Defender | Multiplier | Notes |
|----------|----------|------------|-------|
| Fire | Wind | 1.25x | Fire burns through Wind |
| Fire | Water | 0.8x | Water extinguishes Fire |
| Wind | Earth | 1.25x | Wind erodes Earth |
| Wind | Fire | 0.8x | Fire consumes Wind |
| Earth | Water | 1.25x | Earth absorbs Water |
| Earth | Wind | 0.8x | Wind erodes Earth |
| Water | Fire | 1.25x | Water extinguishes Fire |
| Water | Earth | 0.8x | Earth absorbs Water |
| Lightning | Water | 1.25x | Conductivity |
| Lightning | Earth | 0.8x | Grounding |
| Life | Death | 1.25x | Life overwhelms entropy |
| Life | Shadow | 0.8x | Shadows drain vitality |
| Death | Life | 1.25x | Death claims the living |
| Shadow | Life | 1.25x | Shadows drain vitality |

**Usage in Code:**
```csharp
// Get damage multiplier
float multiplier = ElementMatchups.GetMultiplier(attackerElement, defenderElement);

// Check advantage/disadvantage
bool hasAdvantage = ElementMatchups.HasAdvantage(attackerElement, defenderElement);
```

**Important Notes:**
- Neutral elements have no advantages or disadvantages
- Same-element matchups return 1.0x (neutral)
- Multipliers are tunable via constants in `ElementMatchups.cs`
- This system is **separate from unit traits** - a unit can have both element matchup modifiers AND trait-based resistances that stack multiplicatively

---

## Design Guidelines

### When Creating New Cards

1. **Choose ONE element type** from the 13 available elements
2. **Variants/Hybrids are name-only** - use parent element's affinity
3. **Document the card's theme** in relation to its element

**Examples:**
- "Warrior" → Could be `earth` (stability) or neutral (no affinity)
- "Archer" → Could be `wind` (precision/speed) or neutral
- "Solar Warrior" → Name has "Solar" but uses `fire` affinity
- "Magma Golem" → Name has "Magma" but uses `fire` OR `earth` affinity (pick one)

### When Creating Modifiers

1. **Use ElementTypes constants** for elemental conditions
2. **Match on elemental_affinity** in the categories dictionary
3. **Variants receive parent bonuses** automatically (no special handling needed)

### Future Expansion

- **Core Elements:** Main campaign content, always available
- **Outer Elements:** Expansion packs, late-game unlocks, special events
- **Elevated Elements:** Endgame transformations, mythological content
- **Occultist:** Antagonist campaigns, corruption mechanics

---

## Summary

This elemental structure ensures that every element tier has a purpose — not just in lore, but in how the player experiences discovery, power, and growth throughout Fateforged.

**Element Counts:**
- Core Elements: 4 (fire, water, wind, earth)
- Outer Elements: 5 (lightning, shadow, poison, life, death)
- Occultist: 1 (occultist)
- Elevated Elements: 3 (holy, ice, metal)
- **Total: 13 distinct element types**

**Card Flavors (NOT element types):**
- Variants: Ash/Coldfire/Star (fire), Mist (water), Smoke/Tempest (wind), Crystal (earth)
- Hybrids: Magma (picks one parent affinity)
