# Elemental System - Project Summoner

This document defines the elemental structure for Project Summoner. It organizes all known elements into categories that balance worldbuilding clarity with gameplay purpose.

---

## Table of Contents

1. [Overview](#overview)
2. [Element Types](#element-types)
3. [Card Flavors (Variants & Hybrids)](#card-flavors-variants--hybrids)
4. [Technical Implementation](#technical-implementation)
5. [Design Guidelines](#design-guidelines)

---

## Overview

The elemental system in Project Summoner consists of **14 distinct element types** organized into tiers based on their narrative and gameplay roles. Each tier serves a specific purpose in campaign progression, worldbuilding, and mechanical design.

**Important:** Variants (Solar, Mist, Tempest, Crystal) and Hybrids (Magma) are **NOT** separate element types. They are thematic card flavors that use their parent element's affinity.

---

## Element Types

### Core Elements (4)

The four primary campaign elements — the foundation of the world and the player's main path of progression. Each Core Element will receive a full campaign at launch.

| Element | Variant Name | Description |
|---------|--------------|-------------|
| **Fire** | Solar | Embodies vitality, passion, and transformation. Solar Fire represents controlled, radiant heat — a tempered but fierce energy. |
| **Water** | Mist | Symbolizes adaptability, empathy, and memory. Mist is elusive and reactive, emphasizing concealment and transformation. |
| **Wind** | Tempest | Represents motion, freedom, and volatility. Tempest is chaotic, fast, and unpredictable. |
| **Earth** | Crystal | Stands for stability, structure, and endurance. Crystal reflects refinement, resilience, and clarity. |

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

### Elevated Elements (4)

Elevations are fundamental transformations — not stronger forms, but entirely new existential states. Only certain elements can elevate because true elevation requires an element to transcend its natural identity.

| Base Element | Elevated Form | Nature of Change | Description |
|--------------|---------------|------------------|-------------|
| **Fire** | **Holy** | Physical → Moral / Sacred | Flame becomes sanctity. Energy with purpose, divine intention, and cleansing light. |
| **Water** | **Ice** | Mutable → Immutable | Flow becomes control and stillness. Preservation through perfection. |
| **Earth** | **Metal** | Organic → Forged | Matter learns to shape itself — civilization and artifice emerge. |
| **Life** | **Spirit** | Biological → Metaphysical | Living energy transcends the body — consciousness becomes form. |

**Design Purpose:** Elevated elements define world mythology and serve as long-term expansion potential. They represent philosophical transformation, not progression. Only certain forces can reach this state.

**Technical Implementation:**
```gdscript
# Elevated element constants
ElementTypes.HOLY    # "holy"   (Fire → Holy)
ElementTypes.ICE     # "ice"    (Water → Ice)
ElementTypes.METAL   # "metal"  (Earth → Metal)
ElementTypes.SPIRIT  # "spirit" (Life → Spirit)

# Check elevation relationships
if ElementTypes.can_elevate(ElementTypes.FIRE):
    var elevated_form = ElementTypes.get_elevation(ElementTypes.FIRE)  # Returns "holy"
```

---

## Card Flavors (Variants & Hybrids)

### Variants (Empowered Subtypes)

Variants are slightly stronger, reward-tier versions of base elemental cards. They appear as rare campaign rewards, achievement bonuses, or post-battle upgrades. **Variants maintain their parent element's typing** but feature enhanced effects or unique passives.

| Element | Variant Name | Role |
|---------|--------------|------|
| Fire | **Solar** | Found in advanced campaign tiers. Stronger, radiant version of Fire units. |
| Water | **Mist** | Earned through performance milestones. Prioritizes control and evasion. |
| Wind | **Tempest** | Rewarded for mastery of Wind. Focuses on aggression and volatility. |
| Earth | **Crystal** | Rewarded for precision or defense milestones. Represents fortified endurance. |

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

---

## Design Guidelines

### When Creating New Cards

1. **Choose ONE element type** from the 14 available elements
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

This elemental structure ensures that every element tier has a purpose — not just in lore, but in how the player experiences discovery, power, and growth throughout Project Summoner.

**Element Counts:**
- Core Elements: 4 (fire, water, wind, earth)
- Outer Elements: 5 (lightning, shadow, poison, life, death)
- Occultist: 1 (occultist)
- Elevated Elements: 4 (holy, ice, metal, spirit)
- **Total: 14 distinct element types**

**Card Flavors (NOT element types):**
- Variants: Solar, Mist, Tempest, Crystal (parent element affinity)
- Hybrids: Magma (picks one parent affinity)
