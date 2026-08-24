# Universal Reward System Validation Cases

**Status:** PASS 3 IMPLEMENTED (1 CONSUMER MIGRATION DEFERRED)
**Initiative:** `universal-reward-system`
**Domain:** `meta`
**Last Updated:** `2026-07-25`
**Companion Plan:** `universal-reward-system-plan.md`

## How To Use

1. Pass 2 created a mapped test skeleton for every baseline case and preserved these IDs.
2. Pass 3 changes each case to `Implemented` or records it under Deferred Cases with a reason and follow-up.
3. Exact class rewards and balance are fixtures for these behaviors, not product decisions made by this initiative.

Allowed status values:

1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| URS-C01 | An Academy activity earns one automatic authored offer containing a fixed grant bundle. | The complete bundle is claimed once without player input and progression continues. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C02 | A selectable authored offer declares `show 3, choose 1`. | The same three authored options are shown and exactly one valid option can be claimed. | unit | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | Implemented |
| URS-C03 | One activity earns an automatic offer and two selectable offers. | The automatic grant commits, both choices persist, and later activities remain locked until both are claimed. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C04 | An activity or course has no reward offer. | Completion and progression proceed without creating a snapshot, pending choice, or receipt. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C05 | A category-only pool offer is visible before enrollment and later earned. | Preview exposes category information only; exact options resolve and persist when earned. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C06 | An exact-preview pool offer is revealed before enrollment. | Exact options resolve and persist at first reveal and are returned unchanged on later reads. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C07 | Reward JSON or pool membership changes after exact options were persisted. | The existing snapshot and pending choice retain their original complete typed grants. | serialization | `tests/csharp/Serialization/RewardPersistenceTest.cs` | Implemented |
| URS-C08 | Ownership filtering leaves fewer than `showCount` but at least `chooseCount` options. | Resolution returns the smaller eligible set without duplicates or relaxed filters. | unit | `tests/csharp/Meta/Rewards/RewardResolverTest.cs` | Implemented |
| URS-C09 | Ownership filtering leaves fewer options than `chooseCount`. | Content/runtime validation fails loudly and no partial snapshot or claim is created. | unit | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | Implemented |
| URS-C10 | The player leaves while one or more reward choices are pending and reloads the profile. | Complete options and pending state round-trip unchanged; the choices remain claimable. | serialization | `tests/csharp/Serialization/RewardPersistenceTest.cs` | Implemented |
| URS-C11 | A player submits a valid `choose M` selection whose options contain multiple grant types. | The entire selection validates, all grants and one receipt commit, and the profile saves once. | integration | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | Implemented |
| URS-C12 | The same stable claim ID is submitted again after a successful claim. | The existing receipt is returned and no grant is applied or saved again. | integration | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | Implemented |
| URS-C13 | One grant in a selected bundle is invalid or its handler rejects it. | No grants, receipt, or partial profile mutation are committed. | integration | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | Implemented |
| URS-C14 | Persistence fails while committing a staged reward transaction. | The transaction exposes failure and leaves rewards and receipt unapplied on reload. | integration | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | Implemented |
| URS-C15 | A grant explicitly targets account, current summoner/campaign, or a card instance. | Only the declared owner/target changes; handlers never redirect based on ambient state. | unit | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | Implemented |
| URS-C16 | Reward content uses each registered baseline grant discriminator. | The correct typed handler validates and stages card, resource, item, unlock, cosmetic, emote, XP, trait, or Academy progress data. | unit | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | Implemented |
| URS-C17 | JSON contains an unknown discriminator, missing handler, invalid reference, duplicate stable ID, or invalid selection counts. | Catalog loading reports a precise validation error and refuses the invalid content. | content validation | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | Implemented |
| URS-C18 | A new valid offer or pool uses only existing discriminators and references. | It loads without code or registration changes. | content validation | `tests/csharp/Meta/Rewards/RewardCatalogValidationTest.cs` | Implemented |
| URS-C19 | A new grant or option-source discriminator is added without its implementation/registration. | Handler/source coverage validation fails before gameplay. | structural | `tests/csharp/Meta/Rewards/RewardRegistrationTest.cs` | Implemented |
| URS-C20 | UI requests a fixed preview, category preview, exact preview, pending choice, and claimed receipt. | The service returns normalized typed view models for each state; adapters do not inspect raw reward definitions. | unit | `tests/csharp/Meta/Rewards/RewardViewModelFactoryTest.cs` | Implemented |
| URS-C21 | GDScript renders and submits a selectable reward. | The combined Results surface renders normalized options and sends offer/claim/option IDs without resolving, filtering, or granting locally. | GDScript integration | `tests/unit/meta/test_post_battle_results.gd` | Implemented |
| URS-C22 | A lesson reward is claimed before the final course activity. | The lesson grant persists independently and course completion does not grant it again. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C23 | A course-completion offer is earned after the final activity. | It uses the same automatic/pending flow as lesson offers and course state reflects unresolved required choices. | integration | `tests/csharp/Services/AcademyRewardIntegrationTest.cs` | Implemented |
| URS-C24 | A battle reward containing currency, summoner XP, card XP, and a card choice migrates to universal offers. | Every distinct victorious attempt grants its XP once, including replays; first-clear currency/cards grant once per summoner/campaign/battle; defeat and abandonment grant nothing. | integration | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| URS-C25 | Existing event/shop/campaign consumers migrate to universal grants. | Each uses typed offers/claims and no production call reaches a legacy dictionary or reward-enum path. | structural | follow-up consumer migration initiative | Deferred |
| URS-C26 | An Academy progress flag, summoner trait, or card trait is granted. | A dedicated typed handler updates only its explicit target and the receipt describes the applied grant. | integration | `tests/csharp/Meta/Rewards/RewardGrantHandlerTest.cs` | Implemented |
| URS-C27 | Multiple concurrent submissions race for the same pending claim. | At most one transaction commits; all successful responses identify the same receipt and grants appear once. | integration | `tests/csharp/Meta/Rewards/RewardClaimServiceTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| URS-D01 | Fixed summoner seed | Same source occurrence, offer ID, pool snapshot, filters, and resolution version | resolve, reload, resolve again | Ordered option IDs and serialized grant snapshots are identical. | Implemented |
| URS-D02 | Two different summoner seeds | Same source occurrence, offer ID, pool, and filters | resolve for each summoner | Each result is deterministic for its owner; fixture seeds produce the expected distinct option order. | Implemented |
| URS-D03 | Fixed summoner seed | Same candidates supplied in different enumeration orders | canonicalize, resolve | Canonical input and resolved option snapshot hashes are identical. | Implemented |
| URS-D04 | Fixed summoner seed | Same offer ID used in two different stable source contexts | resolve each context | Context-derived seeds and fixture option results differ while remaining repeatable. | Implemented |
| URS-D05 | Fixed summoner seed | Same exact-preview snapshot, then modified pool JSON and algorithm version | load persisted state | Persisted snapshot hash remains unchanged and no reroll occurs. | Implemented |

Planned determinism test file: `tests/csharp/Meta/Rewards/RewardDeterminismTest.cs`.

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| URS-C25 | Shop purchases and non-battle events need source-owned transaction/occurrence IDs so retries are idempotent without collapsing distinct purchases. | Migrate shop/event/campaign consumers after their stable occurrence contracts are documented. |

## Exit Criteria Mapping

### Pass 2

1. Every `URS-C*` and `URS-D*` case has a named test skeleton or an explicit mapping to an existing test.
2. Stub behavior is deterministic and cannot accidentally grant a real reward.
3. The stub checklist accounts for all source integrations and legacy removals in the companion plan.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Every deferred case has explicit rationale and a concrete follow-up target.
3. Test results include focused reward suites, Academy integration, serialization/content validation, GDScript UI integration, and the project-wide test commands.
