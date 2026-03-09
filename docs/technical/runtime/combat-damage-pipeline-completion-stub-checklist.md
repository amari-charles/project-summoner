# Combat Damage Pipeline Completion Stub Checklist

PASS 2 objective: compile-safe stubs and data wiring only.

Status legend: `Pending`, `Done`

## Artifact Checklist

- [x] `combat-damage-pipeline-completion-plan.md` present
- [x] `combat-damage-pipeline-completion-validation-cases.md` present
- [x] `combat-damage-pipeline-completion-stub-checklist.md` present

## Wiring Checklist

1. Sim template supports damage split placeholders (`PhysicalDamageRatio`, `ElementalDamageRatio`) : `Done`
2. Runtime unit state carries split placeholders : `Done`
3. Unit-definition build path writes split placeholders : `Done`
4. Spawn path copies template split placeholders into `UnitData` : `Done`
5. Summoner load result captures combat modifiers (`damage_bonus`, `damage_reduction`, elemental buckets) : `Done`
6. Battle init pushes summoner combat modifiers into simulation state : `Done`
7. Simulation API includes explicit setter for summoner combat modifiers : `Done`

## Validation Case Skeleton Mapping

- CDP-004: `SimDamageTest.Calculate_MixedProfile_SplitsAcrossDefenseLanes_Pass2Stub`
- CDP-005: `SimDamageTest.Calculate_PureElementalProfile_UsesMagicLane_Pass2Stub`
- CDP-008: `SimDamageTest.Calculate_SummonerElementalBonus_AppliesForMatchingElement_Pass2Stub`
- CDP-009: `SimDamageTest.Calculate_SummonerElementalBonus_DoesNotApplyForNonMatchingElement_Pass2Stub`
- CDP-010: `UnitDefinitionsTargetingProfileTest.BuildSimTemplate_MixedDamageProfile_MapsFields_Pass2Stub`
- CDP-011: `SimulationIntegrationTest.SpawnedUnit_RetainsDamageProfileFields_Pass2Stub`
- CDP-012: `SimDamageTest.Calculate_FullPipeline_MixedProfile_CorrectOrder_Pass2Stub`

## Notes

- PASS 2 does not finalize mixed damage math in `SimDamage`.
- PASS 3 will replace stub assertions with real scenario assertions and update validation statuses.
