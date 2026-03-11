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

## 10. Per-Summoner Trait Lines (Simple V1)

This section is intentionally simple and uses only existing summoner stats.  
No unit modifiers. No triggers. No conditional logic.

### 10.1 Rules

- Each core summoner gets exactly 2 exclusive lines.
- Each line has 4 tiers (`I -> IV`) with strict prerequisites.
- Level-up offer target remains 3 options (to avoid bloat).
- Keep number curves simple and consistent:
  - Percent lines: `+5%`, `+10%`, `+15%`, `+20%`
  - Flat health line: `+100`, `+200`, `+300`, `+400`
  - Flat reduction line: `+1`, `+2`, `+3`, `+4`

## 11. Summoner Identity Lines (Simple Draft)

### 11.1 Cole (Fire)

| Tier | Trait Name | Effect |
|---|---|---|
| I | Element Affinity I | `fire_damage_bonus +5%` |
| II | Element Affinity II | `fire_damage_bonus +10%` |
| III | Element Affinity III | `fire_damage_bonus +15%` |
| IV | Element Affinity IV | `fire_damage_bonus +20%` |
| I | Damage Bonus I | `damage_bonus +5%` |
| II | Damage Bonus II | `damage_bonus +10%` |
| III | Damage Bonus III | `damage_bonus +15%` |
| IV | Damage Bonus IV | `damage_bonus +20%` |

### 11.2 Selene (Water)

| Tier | Trait Name | Effect |
|---|---|---|
| I | Max Health I | `max_health +100` |
| II | Max Health II | `max_health +200` |
| III | Max Health III | `max_health +300` |
| IV | Max Health IV | `max_health +400` |
| I | Mana Regen I | `mana_regen +5%` |
| II | Mana Regen II | `mana_regen +10%` |
| III | Mana Regen III | `mana_regen +15%` |
| IV | Mana Regen IV | `mana_regen +20%` |

### 11.3 Mei (Wind)

| Tier | Trait Name | Effect |
|---|---|---|
| I | Cast Speed I | `cast_speed +5%` |
| II | Cast Speed II | `cast_speed +10%` |
| III | Cast Speed III | `cast_speed +15%` |
| IV | Cast Speed IV | `cast_speed +20%` |
| I | Element Affinity I | `wind_damage_bonus +5%` |
| II | Element Affinity II | `wind_damage_bonus +10%` |
| III | Element Affinity III | `wind_damage_bonus +15%` |
| IV | Element Affinity IV | `wind_damage_bonus +20%` |

### 11.4 Teo (Earth)

| Tier | Trait Name | Effect |
|---|---|---|
| I | Damage Reduction I | `damage_reduction +1` |
| II | Damage Reduction II | `damage_reduction +2` |
| III | Damage Reduction III | `damage_reduction +3` |
| IV | Damage Reduction IV | `damage_reduction +4` |
| I | Element Affinity I | `earth_damage_bonus +5%` |
| II | Element Affinity II | `earth_damage_bonus +10%` |
| III | Element Affinity III | `earth_damage_bonus +15%` |
| IV | Element Affinity IV | `earth_damage_bonus +20%` |

## 12. Implementation Notes (Simple V1)

- Use summoner-only tags:
  - `Tags = [TraitTags.Summoner, TraitTags.Cole]`
  - `Tags = [TraitTags.Summoner, TraitTags.Selene]`
  - `Tags = [TraitTags.Summoner, TraitTags.Mei]`
  - `Tags = [TraitTags.Summoner, TraitTags.Teo]`
- Prerequisites stay linear (`I -> II -> III -> IV`).
- Keep this pass stat-only; add complex behavior in V2 only after playtest.

## 13. V2 Expansion (Deferred)

1. Add mixed traits (summoner + unit effects).
2. Add triggers/conditions (on-kill, below HP, etc.).
3. Add oath-linked capstone interactions.
