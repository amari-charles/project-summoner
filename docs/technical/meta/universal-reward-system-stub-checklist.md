# Universal Reward System Stub Checklist

> PASS 3 completion note (2026-07-25): the stubs mapped below were replaced by the
> real resolver, persisted snapshot/claim transaction, grant handlers, strict JSON
> loading, normalized view models, Academy integration, and choice UI. The map is
> retained as phase-history evidence. Battle and non-Academy consumer migrations
> remain explicitly deferred as URS-C24 and URS-C25 in the validation matrix.

**Status:** PASS 2 CHECKLIST
**Initiative:** `universal-reward-system`
**Domain:** `meta`
**Last Updated:** `2026-07-25`

## Approval Evidence

1. PASS 1 to PASS 2 approval was explicitly recorded in the delivery thread on `2026-07-25`: `ready`.

## Types Created

1. `RewardOfferId`, `RewardOptionId`, `UniversalRewardPoolId`, and `RewardClaimId` - stable universal identifiers.
2. `RewardOfferDefinition`, `RewardOptionDefinition`, and `RewardPoolDefinition` - immutable offer, option-bundle, and reusable-pool data.
3. `RewardSelectionRule`, `RewardPreviewPolicy`, and `RewardEligibilityDefinition` - explicit choice, preview, and duplicate-filter policies.
4. `RewardOwnershipTarget` - explicit account, summoner/campaign, summoner, or card-instance target.
5. `RewardOptionSourceDefinition` with authored and pool definitions - JSON-discriminated option-source data.
6. `RewardGrantDefinition` with eleven concrete baseline grant records - JSON-discriminated typed grant data.
7. `RewardSourceContext`, `ResolvedRewardOfferSnapshot`, `PendingRewardSelection`, `RewardClaimReceipt`, and `RewardProfileState` - universal persisted state shapes.
8. `RewardResolutionContext`, `RewardResolutionResult`, `RewardGrantContext`, `RewardGrantPreparation`, `RewardClaimRequest`, and `RewardClaimResult` - runtime request/result contracts.
9. `RewardOfferViewModel`, `RewardOptionViewModel`, and `RewardGrantViewModel` - normalized presentation shapes.
10. `RewardContentCatalog` and `RewardContentLoadResult` - strict JSON catalog boundary.
11. `UniversalRewardRuntime` - composition root exposed through the existing RewardService autoload.

## Interfaces Created

1. `IRewardOptionSource` - resolves one typed source definition for a deterministic context.
2. `IRewardGrantHandler<TGrant>` and non-generic dispatch contract - validates and prepares one grant type.
3. `IRewardGrantMutation` - opaque staged profile mutation produced by a handler.
4. `IRewardGrantTransaction` and `IRewardGrantTransactionFactory` - stages mutations and receipt for one atomic commit.
5. `IRewardProfileStore` - exposes universal reward state and the transaction boundary without expanding per-resource repository methods.
6. `IRewardContentLoader` and `IRewardContentValidator` - content loading and strict validation boundaries.

## Wiring Points Updated

1. `ProfileData` now owns a top-level `RewardProfileState`.
2. `ProfileRepository` implements `IRewardProfileStore` and returns an unavailable transaction stub.
3. `AcademyCourseDefinition` and `AcademyCourseActivity` expose embedded immutable `RewardOffers`.
4. `AcademyProgressHandler` owns a universal runtime seam and exposes its status in the course view dictionary.
5. The existing `RewardService` autoload composes `UniversalRewardRuntime` and exposes `GetUniversalRewardStatus()` to GDScript.
6. Authored and pool option-source implementations are registered in the stub runtime.
7. The content-loader, view-model, resolver, claim, and transaction seams are all reachable without executing reward behavior.

## Legacy Paths Removed or Disabled

1. Universal resolution and claiming are `disabled`: every stub returns `Pass3Pending`, produces no snapshot or receipt, and cannot stage or commit a mutation.
2. Academy-specific `AcademyCourseReward`, battle `RewardType`/`BattleRewardSpec`, card-only pools, battle pending data, and dictionary grants remain temporarily active for unchanged gameplay and are explicitly tracked for migration and deletion in PASS 3.
3. No universal-to-legacy fallback or compatibility adapter was introduced.
4. The universal Academy fields are not yet consumed by completion logic; this prevents parallel grants while the new handlers and transaction are incomplete.

## Compile-Safe Stub Behavior Checks

1. The runtime status is always `pass_3_pending`, with `can_resolve=false` and `can_claim=false`.
2. Authored and pool resolvers return the same deterministic disabled result and never consume randomness.
3. Claim requests return no receipt and invoke no transaction.
4. `UnavailableRewardGrantTransaction` refuses staging and commit.
5. The content loader fails closed with an explicit PASS 3 error and an empty catalog.
6. Profile reward collections start empty; no Pass 2 code writes them.
7. The normalized view-model factory emits a typed pending state without interpreting reward content in GDScript.
8. Build and focused test execution are green.

## Deliberately Deferred To PASS 3

1. JSON parsing, schema/reference/semantic validation, and source catalog migration.
2. Canonical candidate ordering, versioned seed derivation, and deterministic sampling.
3. Ownership/duplicate filtering and `showCount`/`chooseCount` enforcement.
4. Reward handler implementations and handler-coverage registration.
5. Copy-on-write profile transaction, one-save commit, rollback, and concurrency control.
6. Profile dictionary serialization for `RewardProfileState`; Pass 2 cannot populate the state, so no data can be lost.
7. Snapshot, pending-choice, receipt, and per-summoner seed lifecycle.
8. Academy trigger/blocking behavior and all battle/event/shop migrations.
9. Legacy type, service, GDScript mirror, and documentation removal.
10. Full normalized UI models and reward-screen migration.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| URS-C01 | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | `URS_C01_C03_C04_C05_C06_C22_C23_AcademyHasUniversalOfferSeams` | Academy offer seam |
| URS-C02 | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | `URS_C02_C05_C06_OptionSourcesExposeSafePass3Stub` | Authored selection source |
| URS-C03 | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | `URS_C01_C03_C04_C05_C06_C22_C23_AcademyHasUniversalOfferSeams` | Multiple-offer seam |
| URS-C04 | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | `URS_C01_C03_C04_C05_C06_C22_C23_AcademyHasUniversalOfferSeams` | Empty offers supported |
| URS-C05 | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | `URS_C02_C05_C06_OptionSourcesExposeSafePass3Stub` | Category pool source |
| URS-C06 | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | `URS_C02_C05_C06_OptionSourcesExposeSafePass3Stub` | Exact-preview source |
| URS-C07 | `tests/csharp/Serialization/RewardPersistenceTest.cs` | `URS_C07_C10_D05_ProfileOwnsUniversalRewardState` | Snapshot state seam |
| URS-C08 | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | `URS_C08_C09_EligibilityAndSelectionContractsAreExplicit` | Explicit counts/filter |
| URS-C09 | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | `URS_C09_C17_C18_ContentLoaderStubFailsClosed` | Fail-closed validation |
| URS-C10 | `tests/csharp/Serialization/RewardPersistenceTest.cs` | `URS_C07_C10_D05_ProfileOwnsUniversalRewardState` | Pending persistence shape |
| URS-C11 | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | `URS_C11_C12_C13_C27_ClaimStubCannotMutateOrIssueReceipt` | Atomic claim seam |
| URS-C12 | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | `URS_C11_C12_C13_C27_ClaimStubCannotMutateOrIssueReceipt` | Receipt/idempotency seam |
| URS-C13 | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | `URS_C11_C12_C13_C27_ClaimStubCannotMutateOrIssueReceipt` | No partial grant |
| URS-C14 | `tests/csharp/Infrastructure/Persistence/RewardGrantTransactionTest.cs` | `URS_C14_TransactionStubFailsClosedWithoutCommit` | Transaction refuses commit |
| URS-C15 | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | `URS_C15_OwnershipTargetRequiresScopeAndOptionalExplicitTarget` | Explicit target |
| URS-C16 | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | `URS_C16_C26_BaselineGrantDefinitionsAreSeparateTypes` | Baseline grant types |
| URS-C17 | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | `URS_C09_C17_C18_ContentLoaderStubFailsClosed` | Strict loader seam |
| URS-C18 | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | `URS_C09_C17_C18_ContentLoaderStubFailsClosed` | Data-only content seam |
| URS-C19 | `tests/csharp/Meta/Rewards/RewardRegistrationTest.cs` | `URS_C19_BuiltInOptionSourceImplementationsDeclareDefinitionTypes` | Source registration |
| URS-C19 | `tests/csharp/Meta/Rewards/RewardRegistrationTest.cs` | `URS_C19_GrantHandlerContractRequiresAnExplicitGrantType` | Handler registration |
| URS-C20 | `tests/csharp/Meta/Rewards/RewardViewModelFactoryTest.cs` | `URS_C20_ViewModelStubCarriesNormalizedOfferState` | Typed UI boundary |
| URS-C21 | `tests/unit/meta/test_reward_screen.gd` | `test_urs_c21_universal_reward_bridge_is_fail_closed_in_pass_2` | GDScript bridge |
| URS-C22 | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | `URS_C01_C03_C04_C05_C06_C22_C23_AcademyHasUniversalOfferSeams` | Activity offers |
| URS-C23 | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | `URS_C01_C03_C04_C05_C06_C22_C23_AcademyHasUniversalOfferSeams` | Course offers |
| URS-C24 | `tests/csharp/Services/UniversalBattleRewardIntegrationTest.cs` | `URS_C24_BattleMigrationTargetsUniversalRuntime` | Battle migration seam |
| URS-C25 | `tests/csharp/Meta/Rewards/LegacyRewardRemovalTest.cs` | `URS_C25_UniversalContractsDoNotDependOnLegacyRewardEnums` | Legacy deletion target |
| URS-C26 | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | `URS_C16_C26_BaselineGrantDefinitionsAreSeparateTypes` | Trait/progress types |
| URS-C27 | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | `URS_C11_C12_C13_C27_ClaimStubCannotMutateOrIssueReceipt` | Concurrent claim seam |
| URS-D01 | `tests/csharp/Meta/Rewards/RewardDeterminismTest.cs` | `URS_D01_D02_D03_D04_StubNeverConsumesRandomnessOrCreatesSnapshot` | Repeatability seam |
| URS-D02 | `tests/csharp/Meta/Rewards/RewardDeterminismTest.cs` | `URS_D01_D02_D03_D04_StubNeverConsumesRandomnessOrCreatesSnapshot` | Summoner seed seam |
| URS-D03 | `tests/csharp/Meta/Rewards/RewardDeterminismTest.cs` | `URS_D01_D02_D03_D04_StubNeverConsumesRandomnessOrCreatesSnapshot` | Canonical order seam |
| URS-D04 | `tests/csharp/Meta/Rewards/RewardDeterminismTest.cs` | `URS_D01_D02_D03_D04_StubNeverConsumesRandomnessOrCreatesSnapshot` | Context seed seam |
| URS-D05 | `tests/csharp/Serialization/RewardPersistenceTest.cs` | `URS_C07_C10_D05_ProfileOwnsUniversalRewardState` | Persisted snapshot seam |

## Verification

Pass 2 historical verification:

1. `dotnet build Fateforged.csproj --no-restore` - passed with 0 warnings and 0 errors.
2. `GODOT_BIN=/Users/amaricharles/.local/bin/godot dotnet test Fateforged.csproj --no-build --no-restore --filter "FullyQualifiedName~Reward"` - 57 passed.
3. `dotnet test Fateforged.csproj --settings test.runsettings --no-build --no-restore` - 1,155 passed.
4. Godot/GUT headless suite - 245 passed, including `test_urs_c21_universal_reward_bridge_is_fail_closed_in_pass_2`.
5. `dotnet format whitespace ... --verify-no-changes --include <PASS 2 C# paths>` - passed.

Pass 3 verification:

1. `dotnet build Fateforged.csproj --no-restore` - passed with 0 warnings and 0 errors.
2. Focused Reward and Academy C# suites - 100 passed.
3. `GODOT_BIN=/Users/amaricharles/.local/bin/godot dotnet test Fateforged.csproj --settings test.runsettings --no-build --no-restore` - 1,165 passed.
4. Godot/GUT headless suite - 241 passed with 1,721 assertions.
5. `git diff --check` - passed.

## Gate Output Requirement

1. PASS 2 ends with an explicit request for PASS 3 approval.
2. If approval is not provided, state: `blocked waiting approval`.
