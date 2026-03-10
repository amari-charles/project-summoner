# Trait Tree Skill Tree Stub Checklist

**Status:** PASS 2 CHECKLIST  
**Initiative:** `trait-tree-skill-tree`  
**Domain:** `meta`  
**Last Updated:** `2026-03-10`

## Types Created

1. `TraitAcquisitionMode` (`scripts/csharp/Infrastructure/Data/Traits/TraitAcquisitionMode.cs`) - typed routing between progression-tree and one-off trait surfaces.
2. `TraitTreeScreen` (`scripts/meta/screens/trait_tree_screen.gd`) - summoner trait-tree screen scaffold with tab split and unlock flow wiring.
3. `CardTraitTreeScreen` (`scripts/meta/screens/card_trait_tree_screen.gd`) - card-instance trait-tree screen scaffold.
4. `TraitTreeCanvas` (`scripts/meta/components/trait_tree_canvas.gd`) - shared tree connector drawing surface for tree screens.

## Interfaces Created

1. No new formal C# interface type in Pass 2.
2. Shared runtime/UI contract reused through existing API facades (`SummonerProgressionApi`, `CardServiceApi`, `TraitCatalogApi`) and scene navigation (`SceneManager`, `NavigationContext`).

## Wiring Points Updated

1. Scene routes added for trait trees in `scripts/application/scene_manager.gd`.
2. Card flow wiring from collection/card detail into card trait tree in `scripts/meta/screens/collection_screen.gd` and `scripts/meta/modals/card_detail_modal.gd`.
3. Summoner traits navigation wired to tree screen in `scripts/meta/screens/summoner_screen.gd`.
4. Level-up panel copy/flow updated to points-only and external traits spend path in `scripts/meta/modals/card_level_up_panel.gd`.
5. Acquisition-mode filtering exposed in `scripts/infrastructure/services/trait_catalog_api.gd` and `scripts/csharp/Infrastructure/Data/Traits/TraitCatalogBridge.cs`.

## Legacy Paths Removed or Disabled

1. Inline trait-offer controls in card detail modal (`trait_offer_header`, `trait_offers_container`, `apply_trait_button`) - `disabled`.
2. Card level-up immediate trait selection flow - `disabled` (level-up now grants points and defers spend to trait tree).
3. Collection-global card trait spend pathway - `disabled` (card-instance-scoped route via `trait_tree_card_instance_id`).

## Compile-Safe Stub Behavior Checks

1. Trait-tree screens load and render deterministic empty-state messaging when required context is missing.
2. Trait unlock attempts call service-layer spend endpoints and depend on service return values for acceptance/rejection.
3. Trait filtering by acquisition mode uses typed/normalized source (`level_up_offer`, `granted_only`) through bridge/API wrappers.
4. Validation matrix file targets all have concrete test files present after this pass.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| C01 | `tests/unit/meta/test_card_level_up_flow.gd` | `test_case_c01_level_up_grants_points_without_forced_trait_choice` | Pass 2 skeleton |
| C02 | `tests/unit/meta/test_card_detail_modal_trait_button.gd` | `test_case_c02_trait_button_badge_reflects_unspent_points` | Pass 2 skeleton |
| C03 | `tests/unit/meta/test_collection_trait_navigation.gd` | `test_case_c03_collection_opens_card_instance_trait_tree` | Pass 2 skeleton |
| C04 | `tests/unit/meta/test_summoner_trait_tree_screen.gd` | `test_case_c04_summoner_traits_use_tree_surface` | Pass 2 skeleton |
| C05 | `tests/unit/meta/test_trait_tree_node_popup.gd` | `test_case_c05_locked_node_popup_shows_details_and_disabled_unlock` | Pass 2 skeleton |
| C06 | `tests/unit/meta/test_trait_tree_unlock_confirmation.gd` | `test_case_c06_available_node_opens_confirmation_modal` | Pass 2 skeleton |
| C07 | `tests/csharp/Services/TraitSpendValidationTest.cs` | `CardService_SpendCardTraitPoint_RejectsUnknownAndIneligibleTraits` | Existing implemented coverage |
| C08 | `tests/csharp/Services/TraitSpendValidationTest.cs` | `CardService_SpendCardTraitPoint_RejectsUnknownAndIneligibleTraits` | Existing implemented coverage |
| C09 | `tests/csharp/Services/TraitSpendValidationTest.cs` | `SummonerProgressionService_SpendTraitPoint_ValidatesCatalogAndEligibility` | Existing implemented coverage |
| C10 | `tests/csharp/Services/CardTraitIsolationTest.cs` | `CardTraitIsolation_C10_Skeleton` | Pass 2 skeleton |
| C11 | `tests/csharp/Traits/TraitCatalogTest.cs` | `GetTraitsByAcquisitionMode_ReturnsOnlyGrantedOnlyTraits` | Existing implemented coverage |
| C12 | `tests/unit/meta/test_trait_tree_canvas_layout.gd` | `test_case_c12_tree_canvas_layout_bottom_up_without_overlap` | Pass 2 skeleton |

## Gate Output Requirement

1. End Pass 2 report with an explicit request for Pass 3 approval.
2. If approval not provided, state: `blocked waiting approval`.
