# Wind Content Working Notes

**Status:** Working notes / brainstorming  
**Last Updated:** 2026-05-26  
**Scope:** Initial content pass for Wind units and spells

## Purpose

Track Wind content decisions while we brainstorm the initial Academy roster. This is not yet a polished implementation spec. Once the roster is stable, decisions from this note can be promoted into the formal element docs, quest design, and runtime implementation plans.

## Initial Pass Context

The initial content pass is limited to:

- Fire
- Water
- Earth
- Wind
- Neutral

The working target is 10 units and 6 spells per element.

Current Wind runtime status:

- Usable Wind units: 5
- Usable Wind spells: 1
- Wind units still needed: 5
- Wind spells still needed: 5

## Wind Archetype Notes

Wind should emphasize speed, evasion, displacement, interrupts, and tempo control.

Useful Wind patterns:

- Quick direct hits.
- Knockback, push, and emergency spacing.
- Fast repositioning.
- Area disruption through chaotic movement.
- Evasion or ranged-pressure mitigation.
- Tempo buffs and debuffs.

## Spell Decisions

### Tail Wind

Existing runtime spell:

- Area tempo spell.
- Allies inside attack faster.
- Enemies inside attack slower.

### Tornado

Decision:

- Wind should have a Tornado-style area disruption spell.

Current shape:

- Type: Spell
- Role: Area disruption / anti-clump
- Effect: Creates a short-lived tornado at a target location. Enemies inside take light repeated damage and are jostled, interrupted, or knocked slightly.

Notes:

- First-pass version can be stationary.
- This should feel chaotic, not like Water's controlled pull effects.

### Crosswind

Decision:

- Wind should have a Crosswind-style defensive area spell.

Current shape:

- Type: Spell
- Role: Ranged pressure mitigation
- Effect: Creates a wind field that lasts for a longer duration, around 15 seconds, and reduces enemy ranged damage in the area.

Notes:

- This gives Wind a defensive spell that still feels like air and spacing.

### Gust / Air Bullet

Decision:

- Wind should have a simple direct damage spell.

Current shape:

- Type: Spell
- Role: Basic direct damage / small displacement
- Effect: Fires a focused air hit at one enemy, dealing damage and possibly applying a small knockback.

Notes:

- This can be called Gust or Air Bullet later.
- It should be Wind's simple low-complexity damage spell.

### Evacuate

Decision:

- Wind should have an emergency spacing spell.

Current shape:

- Type: Spell
- Role: Defensive displacement / peel
- Effect: Target an area and push enemies away from the center.

Notes:

- This should be mostly about creating space, not damage.
- It is distinct from Water Jet because it is area displacement rather than single-target knockback.

### Wind Shear

Decision:

- Wind should have a line or cone damage spell.

Current shape:

- Type: Spell
- Role: Directional damage / movement disruption
- Effect: Damages enemies in a line or cone and pushes them slightly.

Notes:

- Some overlap with other damage-and-push spells is acceptable; typing and shape matter.
- This gives Wind a more aggressive damage shape than Evacuate.

## Unit Decisions

### Fast Backline Diver

Decision:

- Wind should have a fragile melee unit that can quickly reach ranged or support enemies.

Current shape:

- Type: Unit
- Role: Backline pressure / target access
- Effect: Rushes past or around the front line toward ranged or support targets.

Notes:

- This should feel fast and dangerous, not durable.
- It gives Wind a way to punish exposed backline units.

### Attack Speed Aura Support

Decision:

- Wind should have a utility unit that buffs nearby allied attack speed.

Current shape:

- Type: Unit
- Role: Tempo support
- Effect: Nearby allies gain increased attack speed.

Notes:

- This should be attack speed only for the first pass.
- Low direct damage and fragile body are appropriate.

### Miss Chance Support

Decision:

- Wind should have a support caster that makes enemy attacks miss more often.

Current shape:

- Type: Unit
- Role: Defensive disruption / enemy tempo denial
- Effect: Every few seconds, targets an enemy or small enemy area. Affected enemies have reduced hit chance for a short duration.

Notes:

- This should deal little or no damage.
- The fantasy is gusts and distraction causing attacks to glance or miss.

### Wind Swarm

Decision:

- Wind should have a swarm of fast, fragile melee units.

Current shape:

- Type: Unit
- Role: Swarm / distraction / tempo body
- Effect: Summons several low-HP, low-damage melee units that move quickly and surround targets.

Notes:

- A small innate dodge chance is optional.
- This should benefit naturally from Wind attack-speed buffs.

### Flow Striker

Decision:

- Wind should have a fast melee unit that enters a brief flow state after landing hits.

Current shape:

- Type: Unit
- Role: Mobile melee damage / skirmisher
- Effect: On hit, briefly gains dodge chance and attack speed.

Notes:

- This should feel like hit-and-run pressure without implying a literal movement dash.
- It is distinct from the backline diver because it fights its current target rather than prioritizing the enemy backline.
