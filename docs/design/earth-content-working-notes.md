# Earth Content Working Notes

**Status:** Working notes / brainstorming  
**Last Updated:** 2026-05-26  
**Scope:** Initial content pass for Earth units and spells

## Purpose

Track Earth content decisions while we brainstorm the initial Academy roster. This is not yet a polished implementation spec. Once the roster is stable, decisions from this note can be promoted into the formal element docs, quest design, and runtime implementation plans.

## Initial Pass Context

The initial content pass is limited to:

- Fire
- Water
- Earth
- Wind
- Neutral

The working target is 10 units and 6 spells per element.

Current Earth runtime status:

- Usable Earth units: 8
- Usable Earth spells: 1
- Earth units still needed: 2
- Earth spells still needed: 5

## Earth Archetype Notes

Earth should emphasize endurance, structure, pressure absorption, and grounded control.

Useful Earth patterns:

- Damage reduction, shields, and armor-like protection.
- Heavy attacks that feel slow but decisive.
- Roots, stuns, slows, and weight-based suppression.
- Refusing to break: reforming, anchoring, and holding position.
- Control without relying on physical wall or terrain constraints for this pass.

## Spell Decisions

### Fortify

Existing runtime spell:

- Allied area flat damage reduction.
- Serves as Earth's basic group defensive spell.

### Quake

Decision:

- Earth should have a Quake-style spell.

Current shape:

- Type: Spell
- Role: Area disruption
- Effect: Deals area damage and briefly stuns affected enemies.

### Stone Spike / Stone Pillar

Decision:

- Earth should have a high single-target damage spell where stone erupts from the ground under the target.

Current shape:

- Type: Spell
- Role: Single-target punishment
- Effect: A stone spike, pillar, or block erupts beneath one enemy and deals high damage.

Notes:

- This should be distinct from Quake: one target, bigger hit, less broad control.

### Gravity Well

Decision:

- Earth should have a Gravity Well-style suppression spell.

Current shape:

- Type: Spell
- Role: Area suppression
- Effect: Weighs enemies down in an area, reducing movement speed and possibly attack speed.

Notes:

- This should feel heavier and more suppressive than Water's Rain Field.
- It should not rely on physical constraints or walls.

### Reform

Decision:

- Earth should have a pre-cast Reform spell for Earth units.

Current shape:

- Type: Spell
- Role: Resilience / refusal to break
- Effect: Target an allied Earth unit. For a short duration, if that unit dies, it reforms once at around 50% HP.

Notes:

- Pre-cast timing makes this more tactical and avoids corpse targeting complexity.
- This is rebuilding, not generic healing.

### Earthen Grip

Decision:

- Earth should have a single-target root spell.

Current shape:

- Type: Spell
- Role: Single-target lockdown
- Effect: Roots one enemy briefly. Small damage is optional.

Notes:

- This is distinct from Gravity Well because it stops one key unit instead of suppressing an area.

## Unit Decisions

### Shielding Support Unit

Decision:

- Earth should have a straightforward shielding support unit.

Current shape:

- Type: Unit
- Role: Formation support / protection
- Effect: Periodically grants a small shield to nearby allies.

Notes:

- This should feel like reinforcing a formation with stone or armor.
- It should not be a healer.

### Burrow Ambusher

Decision:

- Earth should have a burrowing ambusher unit.

Current shape:

- Type: Unit
- Role: Delayed engage / grounded disruption
- Effect: Burrows or emerges near a target, delivering a heavy first hit or brief disruption, then fights as a normal slow melee unit.

Notes:

- This gives Earth a proactive engage tool without making it fast like Wind.
- The movement fantasy is through the ground, not speed.
