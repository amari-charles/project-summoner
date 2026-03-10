# Trait Tree Skill Tree Validation Cases

**Status:** PASS 2 WIRED (Skeleton Coverage Added)  
**Initiative:** `trait-tree-skill-tree`  
**Domain:** `meta`  
**Last Updated:** `2026-03-10`  
**Companion Plan:** `docs/technical/meta/trait-tree-skill-tree-plan.md`

## How To Use

1. Define baseline scenarios in Pass 1.
2. Keep every case mapped to test type + target test file.
3. Update status values in Pass 3.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| C01 | Player levels up a card with no trait spend action taken yet. | Trait points increase; no immediate trait-choice modal is forced. | integration | `tests/unit/meta/test_card_level_up_flow.gd` | Design-Covered |
| C02 | Card has unspent points after level up. | Traits entry button shows spend-available indicator (`!`, `n`, `9+` rules). | integration | `tests/unit/meta/test_card_detail_modal_trait_button.gd` | Design-Covered |
| C03 | Player opens traits from collection card detail. | Card trait tree opens with selected `card_instance_id`; no collection-global spending path is used. | integration | `tests/unit/meta/test_collection_trait_navigation.gd` | Design-Covered |
| C04 | Player opens summoner traits. | Summoner traits render in tree visualization (not list-only fallback). | integration | `tests/unit/meta/test_summoner_trait_tree_screen.gd` | Design-Covered |
| C05 | Player clicks a locked trait node. | Node details show name + description; unlock CTA disabled with clear reason. | integration | `tests/unit/meta/test_trait_tree_node_popup.gd` | Design-Covered |
| C06 | Player clicks an available, affordable node. | Confirmation modal appears with trait details and confirm/cancel actions. | integration | `tests/unit/meta/test_trait_tree_unlock_confirmation.gd` | Design-Covered |
| C07 | Player confirms unlock with sufficient points and valid prerequisites. | C# service accepts spend, trait is owned, points decrement exactly once, UI refreshes. | unit | `tests/csharp/Services/TraitSpendValidationTest.cs` | Design-Covered |
| C08 | Player confirms unlock with insufficient points. | C# service rejects spend with deterministic rejection reason; ownership unchanged. | unit | `tests/csharp/Services/TraitSpendValidationTest.cs` | Design-Covered |
| C09 | Player confirms unlock with missing prerequisite(s). | C# service rejects spend; node remains locked and reason remains visible in popup. | unit | `tests/csharp/Services/TraitSpendValidationTest.cs` | Design-Covered |
| C10 | Two cards share same catalog but different instance ids. | Unlocking trait on one instance does not unlock it for the other instance. | unit | `tests/csharp/Services/CardTraitIsolationTest.cs` | Design-Covered |
| C11 | Trait catalog includes mixed acquisition modes. | Progression tree includes only `level_up_offer`; one-off tab includes only `granted_only`. | unit | `tests/csharp/Traits/TraitCatalogTest.cs` | Design-Covered |
| C12 | Tree layout contains tier II/III/IV branches. | Bottom-up layout renders non-overlapping connectors and circular icon nodes for each tier. | integration | `tests/unit/meta/test_trait_tree_canvas_layout.gd` | Design-Covered |

## Determinism Cases (If Applicable)

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `N/A` | Repeated spend attempts against same snapshot state | before/after spend and rejected spend | Spend acceptance/rejection + resulting points/ownership are deterministic for identical input state | Design-Covered |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| `None` | No pass-1 deferrals. | N/A |

## Exit Criteria Mapping

### Pass 2

1. Every required case has planned test type + target file.
2. Every case has valid status value.
3. Test skeletons exist for new target files listed above.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Any deferred case includes explicit rationale + follow-up target.
3. Rejection-path behavior and card-instance isolation are verified in test output summary.
