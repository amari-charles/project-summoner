# Spell System Audit

## Goal

Refactor the spell execution path so every spell applies through shared simulation-owned targeting and effect execution. Debug output should describe applied outcomes, not only spell emission or view events.

## Runtime Findings

- Spell cards already define effect payloads in `CardDefinitions` and convert to `SimCardData`.
- Immediate spells resolve targets in `Simulation.ExecuteSpellEffects`.
- Delayed and repeated spell effects are queued as `DelayedEffect` and currently rebuild targeting/effect specs separately in `SimEffects`.
- Projectile spells spawn simulated projectiles for single immediate damage effects; final damage is still applied by the simulation projectile path.
- Existing spell area resolution supports circle, square, line, and cone shapes.

## Spell Audit Matrix

| Element | Spell | Intended behavior | Current support | Audit focus |
| --- | --- | --- | --- | --- |
| Fire | Fireball | Projectile impact damages enemies in an area around impact. | Projectile damage with AoE radius. | Confirm impact, target count, and applied damage log. |
| Fire | Fire Area Burn | Applies stacking burn to enemies in an area. | `StatusApply` area effect. | Confirm stacks, duration, tick damage, and zero-target logging. |
| Fire | Burn Cashout | Consumes burn stacks in an area for amplified immediate damage. | `StatusConsume`. | Confirm no-stack skip and damage from remaining burn value. |
| Fire | Overheat | Buffs allies, then singes them after delay. | Ally buffs plus delayed true damage. | Confirm delayed damage targets allies at application time and logs clearly. |
| Fire | Ignition Mark | Marks one enemy, burns it, and bursts from target HP at cast/removal. | Timed buff removal effect plus burn. | Confirm scaling uses HP captured at application and burst target resolution is correct. |
| Fire | Flare Shield | Shields allies, then bursts on shield break or expiry. | Shield removal effect. | Confirm break/expire burst and readable shield outcome. |
| Water | Cleanse | Heals allies and removes negative effects in area. | Heal plus `Cleanse`. | Confirm both effects share the same target resolution. |
| Water | Water Jet | Single-target damage and knockback. | Immediate single-target effects. | Confirm explicit target and auto-target cases. |
| Water | Rain Field | Slows enemies and applies repeated water damage pulses. | Immediate slow plus delayed/repeated damage. | Confirm pulse count and each application resolves current area targets. |
| Water | Bubble Shield | Shields allies in area. | Area shield. | Confirm target count and shield amount/duration. |
| Water | Whirlpool | Repeatedly pulls enemies inward while damaging them. | Repeated displacement and damage. | Confirm pull direction, pulse count, and logs. |
| Water | Flow | Grants allies dodge chance and percent damage increase. | Evasion and damage buffs. | Confirm percentages display correctly and expire. |
| Earth | Fortify | Grants allies flat damage reduction in area. | Area buff. | Confirm reduction affects incoming damage and logs as flat reduction. |
| Earth | Quake | Damages and briefly stuns enemies in an area. | Damage plus stun. | Confirm both effects apply to same recipients. |
| Earth | Stone Spike | Heavy single-target earth damage. | Single-target damage. | Confirm explicit target behavior and fallback nearest enemy. |
| Earth | Gravity Well | Pulls enemies inward and slows attacks. | Repeated displacement plus attack speed debuff. | Confirm delayed/repeated pull uses cast center. |
| Earth | Reform Earth | Earth allies revive once at half HP while buffed. | Element-gated `ReviveOnDeath`. | Confirm non-Earth units are ignored and revive is consumed once. |
| Earth | Earthen Grip | Roots one enemy and deals light damage. | Root plus damage. | Confirm rooted unit cannot advance but can still attack if in range. |
| Wind | Tail Wind | Allies attack faster, enemies attack slower in square area. | Square ally/enemy attack speed effects. | Confirm square area resolution and percent logs. |
| Wind | Tornado | Repeatedly shoves and damages enemies in a vortex. | Repeated displacement and damage. | Confirm shove direction matches design and pulse logging. |
| Wind | Crosswind | Reduces enemy ranged damage in a long-lived area. | `RangedDamageModifier`. | Confirm ranged-only damage reduction and duration. |
| Wind | Air Bullet | Single-target wind damage and knockback. | Single-target damage plus knockback. | Confirm target and knockback direction. |
| Wind | Evacuate | Pushes enemies away from cast point. | Center-origin displacement. | Confirm center-origin push and zero-target logging. |
| Wind | Wind Shear | Damages a line and pushes targets off course. | Line area damage/displacement. | Confirm line orientation from summoner to cast point. |

## Refactor Acceptance

- Immediate, delayed, repeated, and projectile-backed spells use shared target resolution and effect spec construction.
- Spell debug logs are emitted from application outcomes. Cast-only logs are allowed only for zero-target or queued/delayed summaries.
- View-side spell VFX remain driven by simulation events and do not become the source of truth for spell success.
