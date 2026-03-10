# Combat Damage Pipeline Completion Plan

## Initiative
Complete simulation combat correctness for two open items:
1. Integrate `DamageProfile` split damage (physical + elemental) into runtime damage resolution.
2. Ensure summoner secondary stats (`damage_bonus`, `damage_reduction`, `soul_guard`, elemental bonuses) are wired from profile-computed stats into simulation combat.

## Scope
In scope:
- Simulation-side damage routing and math in C#.
- Runtime data plumbing from unit definitions to sim templates to `UnitData`.
- Summoner stat plumbing from battle init to `SummonerData` combat fields.
- Deterministic test coverage updates for damage scenarios.

Out of scope:
- UI presentation changes for damage types on cards.
- Balance tuning beyond preserving existing semantics.
- Multiplayer protocol expansion unless required for host-authoritative behavior.

## Current Gaps
- `DamageProfile` exists as a data model but is marked as not integrated.
- `BuildSimTemplate` maps element and defenses, but mixed-ratio damage lanes are not represented in runtime unit data.
- `SummonerData` supports `DamageBonus`, `DamageReduction`, `SoulGuard`, and per-element bonuses, but battle initialization does not currently wire computed stats into those fields.

## Target Behavior
1. Unit damage resolution supports mixed attacks:
- Physical portion reduced by `PhysicalDefense`.
- Elemental portion reduced by `MagicDefense`.
- True damage remains defense-agnostic.

2. Summoner modifiers are applied in unit-vs-unit damage path:
- Attacker summoner `DamageBonus` as percent multiplier.
- Attacker elemental bonus bucket keyed by attacker element as percent multiplier.
- Defender summoner `DamageReduction` as flat subtraction after defense lanes.

3. Existing pure physical and pure magic outputs remain unchanged.

## Delivery Passes

## Approval Evidence
- PASS 1 -> PASS 2 approval captured on 2026-03-09 in delivery thread: "ok sounds good lets go to next step".
- PASS 2 -> PASS 3 approval captured on 2026-03-09 in delivery thread: "proceed".
- Post-review follow-up approval captured on 2026-03-09: "Yes do so!!!!!!!!".

### PASS 1: Use Cases + Validation
Status: `Complete`
- Produce plan and validation-case matrix with explicit test mapping IDs.

### PASS 2: Stubs + Wiring
Status: `Complete`
- Add/extend runtime data fields for damage split profile and summoner combat modifiers.
- Add deterministic compile-safe wiring from battle initialization into simulation state.
- Add/extend test skeletons tied to validation IDs.

### PASS 3: Implementation + Tests
Status: `Complete`
- Implement full split-lane damage calculation in `SimDamage`.
- Finalize test assertions and run targeted + full C# test pass.
- Update validation matrix statuses.

### PR REVIEW: READY
Status: `Complete`
- Required artifacts present (`plan`, `validation-cases`, `stub-checklist`).
- Validation matrix scenarios mapped to concrete tests and marked `Implemented`.
- Implementation and validation passes completed; follow-up review fixes incorporated.

## Likely Files
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs`
- `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
- `scripts/csharp/Battle/Simulation/Data/SimCardData.cs` (for `SimUnitTemplate`)
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/csharp/Battle/View/BattleScene.cs`
- `tests/csharp/Simulation/SimDamageTest.cs`
- `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs` (or renamed/expanded sim template tests)

## Risks and Controls
- Risk: changing damage order breaks determinism or expected combat outcomes.
  - Control: preserve existing operation order; add regression tests for pure physical/magic parity.
- Risk: summoner modifiers double-apply in some paths.
  - Control: centralize modifier application in `SimDamage` for unit-target damage and assert expected totals.
- Risk: mixed damage introduces floating-point drift.
  - Control: keep existing one-decimal rounding boundary and deterministic RNG usage unchanged.

## Acceptance Criteria
- Mixed `DamageProfile` values affect output damage as designed and are covered by deterministic tests.
- Existing pure damage tests continue to pass without behavior regression.
- Summoner `damage_bonus` / `damage_reduction` / `soul_guard` / elemental bonuses demonstrably affect simulation damage outcomes.
- Validation matrix scenarios are all marked `Implemented` by end of PASS 3.
