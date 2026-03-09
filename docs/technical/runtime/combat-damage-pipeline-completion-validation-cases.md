# Combat Damage Pipeline Completion Validation Cases

Status legend: `Design-Covered`, `Implemented`, `Deferred`

## Scenario Matrix

| Case ID | Scenario | Expected Result | Test Mapping | Status |
|---|---|---|---|---|
| CDP-001 | Pure physical attack against target with armor only | Uses `PhysicalDefense` lane only; output unchanged vs baseline behavior | `SimDamageTest.Calculate_PhysicalDamage_UsesPhysicalDefense` | Implemented |
| CDP-002 | Pure magic attack against target with magic resist only | Uses `MagicDefense` lane only; output unchanged vs baseline behavior | `SimDamageTest.Calculate_MagicDamage_UsesMagicDefense` | Implemented |
| CDP-003 | True damage against high defenses | Ignores both defense lanes | `SimDamageTest.Calculate_TrueDamage_IgnoresAllDefense` | Implemented |
| CDP-004 | Mixed profile (e.g., 60% physical / 40% elemental) with asymmetric defenses | Physical portion reduced by armor, elemental portion reduced by magic resist, summed then rounded | `SimDamageTest.Calculate_MixedProfile_SplitsAcrossDefenseLanes` | Implemented |
| CDP-005 | Pure elemental unit profile | Routes entirely through magic lane while retaining elemental matchup handling | `SimDamageTest.Calculate_PureElementalProfile_UsesMagicLane` | Implemented |
| CDP-006 | Summoner general damage bonus set on attacker team | Damage scales by percent bonus in unit-vs-unit path | `SimDamageTest.Calculate_SummonerDamageBonus_IncreasesDamage` plus battle-init modifier wiring in `BattleScene`/`SimulationNode` | Implemented |
| CDP-007 | Summoner flat damage reduction set on defender team | Flat reduction applied after defense lanes; floors at zero | `SimDamageTest.Calculate_SummonerDamageReduction_DecreasesDamage` and `Calculate_SummonerDamageReduction_FloorAtZero` | Implemented |
| CDP-008 | Summoner elemental bonus (e.g., fire) set for attacker | Bonus applies only when attacker element matches configured bucket | `SimDamageTest.Calculate_SummonerElementalBonus_AppliesForMatchingElement` | Implemented |
| CDP-009 | Non-matching elemental bonus bucket exists | No bonus applied for non-matching attacker element | `SimDamageTest.Calculate_SummonerElementalBonus_DoesNotApplyForNonMatchingElement` | Implemented |
| CDP-010 | Sim template built from unit definition damage profile | Template carries split fields needed by runtime | `UnitDefinitionsTargetingProfileTest.BuildSimTemplate_DamageProfileFields_MapFromDefinition` | Implemented |
| CDP-011 | Spawned `UnitData` from template retains split profile fields | Runtime spawned unit uses template split data | `SimulationIntegrationTest.SpawnedUnit_RetainsDamageProfileFields` | Implemented |
| CDP-012 | Full pipeline order with mixed profile and summoner modifiers | Deterministic math order preserved with expected final rounded output | `SimDamageTest.Calculate_FullPipeline_MixedProfile_CorrectOrder` | Implemented |

## PASS 2 Stub Expectations
- Add compile-safe placeholders for mixed profile fields in sim template and unit runtime data.
- Add battle-init hook to populate summoner combat modifier fields in simulation state.
- Add empty/skeleton tests for new case IDs CDP-004, 005, 008, 009, 010, 011, 012.

## PASS 3 Completion Criteria
- All listed tests implemented and green.
- Status column for all case IDs updated to `Pass`.
- Any removed/replaced test mappings called out explicitly with replacement IDs.
