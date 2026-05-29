# Water Content Working Notes

**Status:** Working notes / brainstorming  
**Last Updated:** 2026-05-26  
**Scope:** Initial content pass for Water units and spells

## Purpose

Track Water content decisions while we brainstorm the initial academy roster. This is not yet a polished implementation spec. Once the roster is stable, decisions from this note can be promoted into the formal element docs, course design, and runtime implementation plans.

## Initial Pass Context

The initial content pass is limited to:

- Fire
- Water
- Earth
- Wind
- Neutral

The working target is 10 units and 6 spells per element.

Current Water runtime status:

- Usable Water units: 6
- Usable Water spells: 3
- Water units still needed: 4
- Water spells still needed: 3

## Water Archetype Notes

Water should emphasize stability through flow.

Useful Water patterns:

- Cleanse and debuff removal.
- Small healing and long-fight sustain.
- Shields and damage smoothing.
- Slow, push, pull, or other movement control.
- Redistribution of pressure or health across a group.
- Keeping allies alive by flowing resources where they are needed.

## Spell Decisions

### Bubble Shield

Decision:

- Water should have a Bubble Shield-style spell.

Current shape:

- Type: Spell
- Role: Proactive protection / damage smoothing
- Effect: Applies temporary shields to allied units in a target area or to a selected ally.

Notes:

- This should feel proactive, unlike Cleanse, which is reactive.

### Whirlpool

Decision:

- Water should have a Whirlpool-style spell.

Current shape:

- Type: Spell
- Role: Area control / grouping
- Effect: Creates an area that pulls or drifts enemies toward the center, with optional slow or light damage.

Notes:

- This should be distinct from Water Jet. Water Jet is single-target knockback; Whirlpool is area flow control.

### Flow

Decision:

- Water should have a Flow-style ally buff spell.

Current shape:

- Type: Spell
- Role: Defensive mobility / counterpressure
- Effect: Allied units affected by the spell gain increased dodge chance and percent increased damage for a short duration.

Notes:

- This should stay simple for the first pass.
- The dodge component keeps it defensive and fluid; the damage bonus represents finding openings while flowing around attacks.

## Unit Decisions

### HP Redistribution Support

Decision:

- Water should have a support unit that redistributes HP among nearby allies.

Current shape:

- Type: Unit
- Role: Sustain / stability / group balancing
- Effect: Periodically moves HP from healthier nearby allies to wounded nearby allies.

Notes:

- This should feel like water equalizing pressure across a group.
- The effect should not kill donor allies.
- This is preferred over a basic shielding support unit because it feels more distinctly flowy.

### Slippery Melee Unit

Decision:

- Water should have a mobile melee unit that survives through flow rather than raw durability.

Current shape:

- Type: Unit
- Role: Mobile melee / evasive frontliner
- Effect: Moves well and is hard to pin down. It may briefly reduce incoming damage after moving, being hit, or changing targets.

Notes:

- This should feel like water flowing around pressure.
- It should be distinct from a tank: survivability comes from slipperiness, not a large HP pool.

### Basic Water Ranged Unit

Decision:

- Water should have a straightforward ranged attacker.

Current shape:

- Type: Unit
- Role: Basic ranged pressure
- Effect: Ranged attacks with no required special mechanic for the first version.

Notes:

- This fills a simple roster need.
- A slow-on-hit mechanic is optional later, but not part of the current decision.

### Inflating Barbed Defender

Decision:

- Water should have a pufferfish-inspired defender that inflates into a dangerous protective state.

Current shape:

- Type: Unit
- Role: Reactive defender / anti-swarm area denial
- Effect: Inflates when threatened, surrounded, or damaged. While inflated, it damages nearby enemies or punishes attackers, then deflates after a short duration.

Notes:

- This should feel defensive and reactive rather than aggressive.
- While inflated, it may move slower or otherwise lose flexibility.
- The unit gives Water a protective body that controls space without being a pure tank or healer.
