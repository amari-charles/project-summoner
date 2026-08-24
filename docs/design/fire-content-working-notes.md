# Fire Content Working Notes

**Status:** Working notes / brainstorming  
**Last Updated:** 2026-05-26  
**Scope:** Initial content pass for Fire units and spells

## Purpose

Track Fire content decisions while we brainstorm the initial Academy roster. This is not yet a polished implementation spec. Once the roster is stable, decisions from this note can be promoted into the formal element docs, quest design, and runtime implementation plans.

## Initial Pass Context

The initial content pass is limited to:

- Fire
- Water
- Earth
- Wind
- Neutral

The working target is 10 units and 6 spells per element.

Current Fire runtime status:

- Usable Fire units: 4
- Usable Fire spells: 0
- Fire units still needed: 6
- Fire spells still needed: 6

`Fireball` exists in the runtime catalog but is currently archived. We are treating it as a good Fire spell concept to revive or reimplement.

## Fire Archetype Notes

Fire should emphasize temporary intensity, pressure, escalation, and decisive endings.

Useful Fire patterns:

- Damage pressure that forces action.
- Burn or heat that builds toward a payoff.
- Effects that fizzle out, expire, or collapse into a final burst.
- Units or spells that grow stronger temporarily, then pay a cost.
- Finishers that convert ongoing pressure into immediate damage.

Not every Fire card needs to use these patterns, but the overall Fire roster should include them.

## Spell Decisions

### Fireball

Decision:

- Fireball will be part of the Fire spell roster.
- It lands at a target point and damages all units in a radius around the center of impact.

Current shape:

- Type: Spell
- Targeting: Area
- Role: Core area damage spell
- Extra effects: None for now

Notes:

- This should be Fire's clean, foundational area burst spell.
- No lingering burn field, chain detonation, or conditional payoff is currently attached to Fireball.

### Area Burn Spell

Decision:

- Fire should have a spell that applies burn to units in an area.

Current shape:

- Type: Spell
- Targeting: Area
- Role: Burn setup / pressure
- Effect: Applies burn to affected enemies in the target area.

Notes:

- This is the setup half of Fire's pressure-to-cashout loop.
- The first version should probably affect enemies only so the spell is easy to understand.

### Burn Cashout Spell

Decision:

- Fire should have a spell that consumes burn in a given radius and applies the remaining burn damage instantly with a bonus multiplier.

Current shape:

- Type: Spell
- Targeting: Area
- Role: Finisher / pressure conversion
- Effect: Consumes burn on affected enemies, deals the remaining burn damage instantly, and increases that instant damage by a bonus such as 1.5x.

Notes:

- The current working multiplier is 1.5x.
- The spell should create a real timing decision: let burn continue ticking, or cash it out now for immediate burst.
- Balance should avoid making this mandatory for every Fire deck.

## Current Fire Spell Set

The current six-spell Fire set is:

1. Fireball
2. Area Burn Spell
3. Burn Cashout Spell
4. Overheat
5. Ignition Mark
6. Flare Shield

### Overheat

Current leaning:

- Fire should have an Overheat-style spell.

Possible shape:

- Type: Spell
- Targeting: Allied unit or small allied area
- Role: Temporary power with a downside
- Effect: Grants a short burst of damage, attack speed, or both. When the effect ends, the affected unit pays a cost such as HP loss, reduced speed, or a short weakened state.

Notes:

- This strongly fits Fire's temporary intensity premise.
- This should feel like borrowed power, not generic support.

### Ignition Mark

Current leaning:

- Fire should have an Ignition Mark-style spell.

Possible shape:

- Type: Spell
- Targeting: Enemy unit
- Role: Delayed threat / tactical finisher
- Effect: Marks a target. If the target dies during the mark, it explodes. The explosion may scale from the target's HP when the mark was cast.

Notes:

- Scaling from target HP at cast time could make target selection more tactical.
- A high-HP target creates a bigger potential payoff, but may be harder to kill before the mark expires.
- A low-HP target is easier to trigger, but produces a smaller explosion.

### Flare Shield

Decision:

- Fire should have a Flare Shield-style spell in the initial six-spell set.

Possible shape:

- Type: Spell
- Targeting: Allied unit or small allied area
- Role: Defensive threat / counterburst
- Effect: Grants temporary protection. When the protection expires or breaks, it explodes around the protected unit.

Notes:

- This gives Fire a defensive spell that still behaves like Fire.
- The explosion may scale from damage absorbed, making enemy focus fire fuel the counterburst.
- If enemies ignore the protected unit, the player still gets protection but a weaker or no explosion.

## Future Candidate Pool

- Flamethrower-style spell
- Sacrifice or self-damage spell that creates area damage
- Fire movement or repositioning spell

Notes:

- Flamethrower is not settled. It may be redundant with Fireball unless it has a clearly different job, such as directional cone/line pressure.

## Open Questions

1. Should the area burn spell have an immediate damage component, or only apply burn?
2. Should burn cashout consume all burn stacks/value, or only a capped amount?
3. Should Fire have at least one spell that can harm allied units as a risk/reward expression?
4. Should Flamethrower be a cone, line, short channel, or moving sweep?
5. For Ignition Mark, should explosion scaling use target max HP, current HP at cast, or burn/mark value accumulated during the mark?

## Unit Decisions

### Burn Stack Ranged Unit

Decision:

- Fire should have a ranged unit that applies burn stacks through repeated attacks.

Current shape:

- Type: Unit
- Role: Burn setup / ranged pressure
- Effect: Basic ranged attacks apply small burn stacks or burn value.

Notes:

- This gives Fire's unit roster a natural way to support the burn cashout spell.
- Direct hit damage can be modest because the unit's value comes from building pressure over time.

### Bomb Carrier

Decision:

- Fire should have a fast, fragile unit that ends in an explosion.

Current shape:

- Type: Unit
- Role: Suicide burst / anti-clump pressure
- Effect: Runs toward enemies and explodes on contact, death, or another clear trigger.

Notes:

- The current leaning is contact detonation for readability.
- The explosion may deal area damage, apply burn stacks, or both.
- This unit fits Fire's temporary intensity and final-burst identity.

## Open Fire Unit Slots

Fire has 4 remaining unit slots after:

1. Burn Stack Ranged Unit
2. Bomb Carrier

### Kindling Swarm

Decision:

- Fire should have a small swarm unit whose value comes from body pressure and death/cleanup consequences.

Current shape:

- Type: Unit
- Role: Swarm pressure / cleanup tax
- Effect: Summons multiple small, fragile melee units. When individual swarm units die, they apply a tiny burn, leave a tiny short-lived flame patch, or otherwise add minor fire pressure.

Notes:

- This was chosen over a swarm that applies burn on every hit because Fire already has a burn-stack ranged unit.
- The swarm should make clearing it feel like putting out sparks: manageable, but not completely free.
- The first implementation can keep the death effect modest so the unit remains readable.

### Basic Fire Tank

Decision:

- Fire should have a straightforward tank unit.

Current shape:

- Type: Unit
- Role: Durable frontliner
- Effect: High HP, low or medium speed, moderate damage. No special ability is required for the first version.

Notes:

- This gives Fire a body that can hold space long enough for pressure and burn plans to matter.
- If it later needs more identity, it could punish attackers with small burn, but the current decision is to keep it basic.

### Overheating Fighter

Decision:

- Fire should have a fighter that grows stronger during combat, then pays a burnout cost.

Current shape:

- Type: Unit
- Role: Escalating generalist threat
- Effect: Gains attack damage, attack speed, or both while fighting. After reaching a threshold or duration, it starts losing HP or enters a weakened burnout state.

Notes:

- This captures Fire as borrowed intensity.
- This is the broader ramping Fire unit: it gets more dangerous the longer it lives or stays in combat.
- The unit should feel dangerous if ignored, but not stable enough to serve as a normal durable carry.

### Continuous Flame Channel Unit

Decision:

- Fire should have a short-to-mid range unit that channels continuous flame.

Current shape:

- Type: Unit
- Role: Focused single-target killer / positional burn engine
- Effect: Channels a steady flame stream at one target, dealing repeated tick damage and applying small burn over time.

Notes:

- This unit should reward protection and positioning.
- It should be strongest when allowed to maintain contact with the same target.
- It should be weaker when forced to move, retarget, or lose range.
- If the flame damage ramps up, the ramp should reinforce single-target commitment rather than make the unit a general-purpose carry.
