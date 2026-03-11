# Summon Traits V1 (Curated Pass)

**Status:** Draft  
**Date:** 2026-03-10  
**Owner:** Combat Design / Progression

## 1. Goal

Replace placeholder summon trait progression (generic stat ladders) with curated summon-focused lines that support Fateforged's core battle fantasy:

- Preparation-phase planning should matter.
- Battles should feel like army doctrine clashing, not raw stat inflation.
- Trait choices should create distinct summon behaviors (frontline hold, pressure, execution).

This pass is intentionally scoped to the **existing unified trait runtime** (no schema changes required).

## 2. Scope

### In Scope (Now)

- Re-theme and rebalance existing summon trait IDs:
  - `fortitude`, `fortitude_ii`, `fortitude_iii`, `fortitude_iv`
  - `power`, `power_ii`, `power_iii`, `power_iv`
  - `swiftness`, `swiftness_ii`, `swiftness_iii`, `swiftness_iv`
  - `agility`
- Adjust prerequisites/level gates so lines feel intentional.
- Update localization names/descriptions to match gameplay intent.

### Out of Scope (Later)

- New trait acquisition surfaces.
- Oaths (campaign-level irreversible choices).
- New runtime trigger types or new stat keys.
- New trait IDs (candidate for V2).

## 3. Design Principles

1. Summon traits must change **battle posture**, not just DPS curves.
2. Each line should answer a tactical question:
   - "How do I hold space?"
   - "How do I collapse faster?"
   - "How do I close the match?"
3. Every line needs a visible battlefield outcome by mid-fight.
4. Keep numbers conservative in V1; prefer clarity over spikes.

## 4. Curated V1 Trait Lines (Summon Cards)

## 4.1 Bulwark Line (Frontline Control)

Mapped IDs: `fortitude` -> `fortitude_iv`

Intent:
- Increase line-holding and prevent early formation collapse.
- Keep prep-phase tank placement meaningful.

Proposed effects:

| Trait ID | New Name Intent | Proposed Effect Direction |
|---|---|---|
| `fortitude` | Bulwark I | `max_hp` up, small `armor` up |
| `fortitude_ii` | Bulwark II | additional `max_hp` + `armor` |
| `fortitude_iii` | Brace Discipline | `max_hp` up + conditional toughness (`below_hp_percent`) |
| `fortitude_iv` | Unbroken Front | strongest survivability package, modest anti-summoner pressure (`soul_strength`) |

Notes:
- This line should be the "formation anchor" pick, not a damage race pick.

## 4.2 Assault Line (Kill Pressure)

Mapped IDs: `power` -> `power_iv`

Intent:
- Create decisive picks for players who want fewer, higher-impact kills.
- Improve ability to break defended pockets and punish exposed elites.

Proposed effects:

| Trait ID | New Name Intent | Proposed Effect Direction |
|---|---|---|
| `power` | Assault I | `attack_damage` up |
| `power_ii` | Assault II | `attack_damage` up + light `attack_range` or `crit_chance` |
| `power_iii` | Finisher Doctrine | `attack_damage` up + `on_kill` sustain (`heal_on_kill`) |
| `power_iv` | Execution Protocol | highest `attack_damage` + stronger `soul_strength` |

Notes:
- End of line should feel like "win condition threat", not only better trading.

## 4.3 Tempo Line (Initiative and Collapse Speed)

Mapped IDs: `swiftness` -> `swiftness_iv`

Intent:
- Help armies reach contact faster, retarget faster, and convert local wins into map momentum.

Proposed effects:

| Trait ID | New Name Intent | Proposed Effect Direction |
|---|---|---|
| `swiftness` | Tempo I | `attack_speed` up + small `move_speed` |
| `swiftness_ii` | Tempo II | stronger `attack_speed`, stronger `move_speed` |
| `swiftness_iii` | Momentum Engine | `attack_speed` + `move_speed` + light `aggro_radius` |
| `swiftness_iv` | Overrun Tempo | peak tempo package + conditional burst (`on_kill`) |

Notes:
- This line is "snowball tempo" without hard lane mechanics.

## 4.4 Mobility Node (Cross-Line Utility)

Mapped ID: `agility`

Intent:
- Keep one flexible utility pick for summon cards that need reposition value without full Tempo commitment.

Proposed effects:

| Trait ID | New Name Intent | Proposed Effect Direction |
|---|---|---|
| `agility` | Flank Instinct | `move_speed` + small `aggro_radius` |

Proposed gating:
- Keep at mid-early level.
- Optional prerequisite in later pass (`swiftness` or `power`) if tree density increases.

## 5. Suggested Progression/Gating (V1)

- Line entry (I): level 2
- Line mid (II): level 3
- Line commitment (III): level 5
- Line capstone (IV): level 7
- `agility`: level 3 or 4, independent utility node

This keeps meaningful differentiation before late-campaign power spikes.

## 5.1 Candidate Numeric Package (First Tuning Pass)

These are intentionally conservative first-pass values for implementation.

| Trait ID | Candidate Stats |
|---|---|
| `fortitude` | `max_hp x1.08`, `armor +4` |
| `fortitude_ii` | `max_hp x1.12`, `armor +6` |
| `fortitude_iii` | `max_hp x1.15`, trigger `below_hp_percent(0.45)` -> `armor +8` (duration `4.0`, cooldown `6.0`) |
| `fortitude_iv` | `max_hp x1.18`, `armor +10`, `soul_strength +0.10` |
| `power` | `attack_damage x1.08` |
| `power_ii` | `attack_damage x1.12`, `crit_chance +0.05` |
| `power_iii` | `attack_damage x1.15`, trigger `on_kill` -> `heal_on_kill +6` |
| `power_iv` | `attack_damage x1.18`, `soul_strength +0.20` |
| `swiftness` | `attack_speed x1.06`, `move_speed x1.03` |
| `swiftness_ii` | `attack_speed x1.09`, `move_speed x1.05` |
| `swiftness_iii` | `attack_speed x1.11`, `move_speed x1.07`, `aggro_radius x1.08` |
| `swiftness_iv` | `attack_speed x1.13`, `move_speed x1.10`, trigger `on_kill` -> `attack_speed x1.10` (duration `3.0`, cooldown `2.0`) |
| `agility` | `move_speed x1.08`, `aggro_radius x1.10` |

Implementation note:
- If trigger stacking is too noisy in swarm tests, strip trigger terms first and keep only static stats for V1.

## 6. Balance Guardrails

1. Do not stack unconditional multiplicative offense and defense on same node.
2. Cap most single-node multiplicative boosts to low-double-digit percentages.
3. Keep `on_kill` and `below_hp_percent` effects short-window and test for swarm abuse.
4. Preserve role identity:
   - Swarm cards should not become tank line best-in-slot by default.
   - Elite cards should not become pure speed swarm substitutes.

## 7. Validation Scenarios (Design-Level)

1. **Formation Half-Life Check**
   Bulwark-heavy summon deck should preserve frontline shape longer than baseline.
2. **Collapse Speed Check**
   Tempo-heavy summon deck should reach and convert first advantage faster than baseline.
3. **Win Condition Pressure Check**
   Assault-heavy summon deck should increase meaningful summoner threat when frontline opens.
4. **No Dominant Line Check**
   Across 40/80-unit scenarios, no single line should dominate all matchups.

## 8. Implementation Checklist

1. Update summon trait definitions in `TraitDefinitions` for curated effects.
2. Update trait display copy in `localization/data/en.json` (`trait.*` keys).
3. Re-run trait tree eligibility + spend validation tests.
4. Add at least one simulation-focused regression scenario per line outcome.
5. Run a tuning pass after first playtest packet.

## 9. V2 Candidates (Post V1)

- Add additional mobility line depth (`agility_ii`+).
- Add creature-type-specific summon traits (`beast`, `construct`, `aerial`) once card tags are fully authored.
- Add element-linked summon trait forks once summon card trait tags include element consistently.

## 10. Per-Summoner Trait Lines (V1 Level-Trait Draft)

This section defines the first per-summoner identity lines for level traits.  
Scope here is design and naming; implementation can use existing runtime primitives (`Tags`, `RequiredTags`, `StatMults`, `StatAdds`, existing triggers).

### 10.1 Offer Budget (Keep Choice Count Tight)

To avoid upgrade bloat while preserving build planning:

- Give each core summoner exactly two identity lines.
- Each line has 4 tiers (`I -> IV`) with strict prerequisite chains.
- Level-up offer target: 3 options total.
- Suggested mix per offer:
  - 1 option from summoner-exclusive lines (`TraitTags.<SummonerId>`)
  - 1 option from element/global summoner pool
  - 1 flexible fallback from global pool

This keeps the planning depth without flooding players with low-signal picks.

## 11. Summoner Identity Lines

### 11.1 Cole (Fire) - Aggressive Tempo and Finish Pressure

Personality anchor: arrogant, competitive, first into challenge.  
Gameplay identity: force early trades, then convert lane break into summoner pressure.

#### Line A: Ember Command (consistent fire pressure)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Ember Command I | `fire_damage_bonus +6%`, fire-unit `attack_damage x1.05` |
| II | Ember Command II | `fire_damage_bonus +10%`, fire-unit `attack_damage x1.08` |
| III | Ember Command III | `fire_damage_bonus +14%`, fire-unit `attack_speed x1.06` |
| IV | Ember Command IV | `fire_damage_bonus +18%`, fire-unit `soul_strength +1` |

#### Line B: Duelist's Edge (risk-forward execution)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Duelist's Edge I | all units `soul_strength +1` |
| II | Duelist's Edge II | all units `attack_damage x1.06`, `soul_strength +1` |
| III | Duelist's Edge III | trigger `OnKill` -> all units `attack_speed x1.08` (short window) |
| IV | Duelist's Edge IV | `damage_bonus +10%` (summoner outgoing), all units `soul_strength +2` |

### 11.2 Selene (Water) - Sustain, Stability, and Recovery

Personality anchor: gentle, caring, calm under pressure.  
Gameplay identity: absorb pressure, maintain formation health, win extended fights.

#### Line A: Tideguard (defensive backbone)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Tideguard I | `max_health +120`, `damage_reduction +2` |
| II | Tideguard II | `max_health +220`, `damage_reduction +3` |
| III | Tideguard III | water-unit `max_hp x1.10`, `magic_resist +4` |
| IV | Tideguard IV | `max_health +350`, water-unit `max_hp x1.15`, `soul_guard +8` |

#### Line B: Flow Renewal (economy and sustain throughput)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Flow Renewal I | `mana_regen +8%`, `healing_bonus +6%` |
| II | Flow Renewal II | `mana_regen +12%`, `healing_bonus +10%` |
| III | Flow Renewal III | all units `heal_on_kill +3`, water-unit `attack_speed x1.04` |
| IV | Flow Renewal IV | `mana_regen +18%`, all units `heal_on_kill +5`, water-unit `max_hp x1.08` |

### 11.3 Mei (Wind) - Initiative, Reposition, and Opportunism

Personality anchor: elusive, selective, opportunistic.  
Gameplay identity: dictate pace, reposition quickly, exploit moments rather than attrition.

#### Line A: Slipstream Control (initiative and cadence)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Slipstream I | `cast_speed +8%`, wind-unit `move_speed x1.05` |
| II | Slipstream II | `cast_speed +12%`, wind-unit `move_speed x1.08` |
| III | Slipstream III | wind-unit `attack_speed x1.08`, `aggro_radius x1.06` |
| IV | Slipstream IV | `cast_speed +18%`, wind-unit `attack_speed x1.12`, `move_speed x1.10` |

#### Line B: Opportunist Circuit (burst windows)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Opportunist I | all units `crit_chance +0.03` |
| II | Opportunist II | all units `crit_chance +0.05`, `crit_damage +0.10` |
| III | Opportunist III | trigger `OnTakeHit` -> all units `attack_speed x1.07` |
| IV | Opportunist IV | `wind_damage_bonus +12%`, all units `crit_damage +0.20` |

### 11.4 Teo (Earth) - Frontline Authority and Siege Closure

Personality anchor: direct, reliable, finishes what he starts.  
Gameplay identity: hold center, grind favorable trades, then close with heavy pushes.

#### Line A: Bedrock Oath (formation anchor)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Bedrock Oath I | `max_health +150`, earth-unit `armor +3` |
| II | Bedrock Oath II | `max_health +260`, earth-unit `armor +5` |
| III | Bedrock Oath III | earth-unit `max_hp x1.12`, `magic_resist +5` |
| IV | Bedrock Oath IV | `damage_reduction +4`, earth-unit `max_hp x1.18`, `armor +8` |

#### Line B: Siege March (deliberate finishing power)

| Tier | Candidate Trait Name | Candidate Runtime Effects |
|---|---|---|
| I | Siege March I | earth-unit `attack_damage x1.06` |
| II | Siege March II | earth-unit `attack_damage x1.10`, `attack_range x1.04` |
| III | Siege March III | all units `soul_strength +1`, earth-unit `attack_speed x1.05` |
| IV | Siege March IV | `earth_damage_bonus +14%`, all units `soul_strength +2` |

## 12. Implementation Mapping Notes

- Tagging:
  - Summoner-exclusive lines should use `Tags = [TraitTags.Summoner, TraitTags.<SummonerId>]`.
  - Keep global/element lines unchanged to preserve hybrid builds.
- Prerequisite shape:
  - `Line I` has no prerequisite.
  - `Line II` requires `Line I`.
  - `Line III` requires `Line II`.
  - `Line IV` requires `Line III`.
- Trigger discipline:
  - Keep trigger durations short and deterministic.
  - Prefer one trigger per line until playtest confirms readability.

## 13. Open Balancing Questions

1. Do we want each summoner to have one "safe" line and one "greedy" line by rule?
2. Should Line IV be reachable by level 7 or reserved for level 8+ on summoners?
3. Should per-summoner lines be weighted higher than global lines in trait offer rolls?
