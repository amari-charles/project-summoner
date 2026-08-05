# Universal Reward System Initiative Plan

**Status:** PASS 3: IMPLEMENTATION + TESTS COMPLETE WITH EXPLICIT CONSUMER DEFERRALS
**Initiative:** `universal-reward-system`
**Domain:** `meta`
**Last Updated:** `2026-07-25`
**Owner:** `Codex + User`

## Summary

Replace the parallel Academy, battle, and dictionary-based reward paths with one typed reward system. Reward-bearing content will author offers composed of options and typed grant bundles; the engine will resolve deterministic options, persist the complete promise made to the player, and claim rewards atomically and idempotently. Academy is the first full consumer and retains responsibility only for deciding when an offer is earned and whether unresolved choices block class progression. Existing reward consumers then migrate to the same contracts and the superseded models are removed.

## Goals

1. Support fixed rewards, selectable authored rewards, pool-based rewards, mixed rewards, bundled grants, and no-reward activities on both Academy activities and course completion.
2. Resolve random rewards deterministically for a summoner, persist resolved options, and prevent reloads or later content changes from rerolling them.
3. Validate and apply complete reward claims atomically, save once, and make retries safe through stable claim IDs and receipts.
4. Make new offers and pools data-only while allowing new grant and option-source behaviors through small registered implementations.
5. Give every consumer, including GDScript UI, one normalized view of previews, pending choices, selections, and receipts.
6. Migrate all current reward consumers and remove the competing legacy reward engines.

## Non-Goals

1. Choosing the actual rewards or balance for Practical Spellcraft or other Academy courses.
2. Defining grade, Honors, or conditional reward-upgrade rules.
3. Final reward-screen visual design, animation, or art.
4. Designing speculative grant types that have no concrete persistence meaning or current consumer.
5. Preserving backward compatibility with pre-overhaul development saves or legacy reward APIs.

## Architecture Decisions

1. A reward offer has a stable offer ID, a selection rule, a preview policy, an option source, and explicit eligibility/ownership filtering rules.
2. An automatic offer grants its authored option bundle without player selection. A selectable offer declares `showCount` and `chooseCount`; each option may contain multiple typed grants.
3. `AuthoredRewardOptionSource` and `PoolRewardOptionSource` implement a common option-source contract. Pools contain complete reward-option bundles rather than card IDs.
4. Reward offers are embedded in the JSON definition of the activity, course, battle, event, or shop entry that owns them. Reusable pools are separate JSON records addressed by stable pool IDs.
5. JSON is loaded into immutable typed C# definitions with strict discriminator, schema, reference, handler-coverage, and semantic validation. Invalid reward content fails startup/test validation; it never silently falls back.
6. Reward definitions contain data only. Each grant type has one registered handler responsible for validating and staging that grant; definitions never call repositories or services.
7. The initial grant set covers current consumers and approved Academy needs: card, resource, item, summoner unlock, cosmetic, emote, summoner experience, card experience, summoner trait, card trait, and Academy progress flag. Equipment and concrete consistency tools use item grants; transcript eligibility and statuses use typed Academy progress flags.
8. Every grant contains an explicit ownership scope and target. Handlers do not infer account, current summoner/campaign, card instance, or other ownership from ambient state.
9. Resolution uses a versioned deterministic random algorithm, a canonical candidate ordering, and a seed derived from the summoner's persistent Academy RNG seed plus stable context IDs. The complete resolved option snapshot is persisted at its resolution boundary.
10. Exact pre-enrollment previews resolve and persist on first reveal. Category-only pool previews resolve and persist when earned. Authored fixed options require no random resolution.
11. Filtering may reduce the result below `showCount` only when at least `chooseCount` eligible options remain. Fewer eligible options than `chooseCount` is an invalid state and never relaxes ownership or duplicate rules.
12. A stable, versioned claim ID hashes length-prefixed player/summoner, source occurrence, and offer IDs so delimiter-bearing IDs cannot collide. The claim service validates the entire selection and bundle before staging any mutation.
13. Grant handlers stage operations into a profile-owned reward transaction. The transaction commits all grants and the claim receipt together and performs one save; validation failure or commit failure leaves both rewards and receipt unapplied.
14. Retrying a committed claim returns the persisted receipt without applying grants again.
15. Resolved snapshots, pending selections, and receipts store immutable typed grant payloads, not only catalog references, so later JSON changes cannot alter an existing promise.
16. The reward application layer returns normalized typed view models. GDScript adapters convert those models for presentation but do not resolve pools, filter options, validate selections, or grant rewards.
17. Academy progression creates/resolves offers when their activity or course trigger is earned. Automatic offers claim immediately; all selectable offers become persistent pending choices, and later activities remain locked until every required choice is resolved.
18. Reward-bearing Academy course definitions migrate from the static C# reward catalog to JSON as the first source integration. Other source catalogs migrate afterward without retaining compatibility shims.

## Public API / Interface / Type Changes

1. Add immutable definition types:
   - `RewardOfferDefinition`
   - `RewardOptionDefinition`
   - `RewardSelectionRule`
   - `RewardPreviewPolicy`
   - `RewardOwnershipTarget`
   - `RewardGrantDefinition` concrete records
2. Add option resolution contracts:
   - `IRewardOptionSource`
   - `AuthoredRewardOptionSource`
   - `PoolRewardOptionSource`
   - `RewardResolutionContext`
   - `RewardResolver`
3. Add grant and claim contracts:
   - `IRewardGrantHandler<TGrant>`
   - `IRewardGrantTransaction`
   - `RewardClaimService`
   - `RewardClaimRequest`
   - `RewardClaimResult`
4. Add persisted state:
   - `ResolvedRewardOfferSnapshot`
   - `PendingRewardSelection`
   - `RewardClaimReceipt`
   - per-summoner `AcademyRewardSeed`
5. Add presentation contracts:
   - `RewardOfferViewModel`
   - `RewardOptionViewModel`
   - `RewardGrantViewModel`
6. Add a strict reward-content loader and validator used by runtime startup and content-validation tests.
7. Replace Academy `Rewards` collections with typed embedded offer definitions on activities and courses.
8. Replace battle/event/shop reward configuration and claim entry points with the same offer, resolution, and claim contracts.

## Placement

1. Immutable JSON-backed definitions and content loading belong under `scripts/csharp/Infrastructure/Data/Rewards/`.
2. Resolution, claim coordination, handler registration, and normalized view-model construction belong under `scripts/csharp/Meta/Services/Rewards/`.
3. Persisted snapshots, pending selections, receipts, and transaction support belong under `scripts/csharp/Meta/Domain/Profile/Rewards/` and `Infrastructure/Persistence/`.
4. Source-specific trigger and blocking integration remains with its owner, such as Academy progression or campaign battle completion.
5. JSON reward pools belong under `data/rewards/`; embedded offers remain inside their source JSON.

## Legacy Removal Scope

1. Remove `AcademyRewardKind`, `AcademyRewardPreviewType`, and `AcademyCourseReward`.
2. Remove the battle-only `Fateforged.Data.Events.RewardType`, `BattleRewardSpec`, and legacy fixed/flexible reward configuration.
3. Replace the current card-only `RewardPoolCatalog`, integer mirror enums, and nondeterministic `System.Random` draw path.
4. Remove `Fateforged.Meta.Rewards.RewardType` and dictionary-shaped reward grant/normalization paths after all consumers migrate.
5. Replace battle-specific `PendingRewardData` with universal pending selections while leaving battle completion semantics with the campaign owner.
6. Remove direct per-grant repository mutations from reward services and handlers.
7. Remove UI logic that hides or interprets rewards through `is_grantable`, raw dictionaries, or Academy-specific preview fields.
8. Update the existing battle reward architecture documentation to the universal model once implementation becomes authoritative.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. The approved product and architecture decisions are captured without implementation.
2. Baseline scenarios cover authored, pool, fixed, mixed, no-reward, persistence, determinism, filtering, atomicity, idempotency, ownership, UI, and legacy migration behavior.
3. Every validation case has an expected result, test type, file target, and `Design-Covered` status.
4. Pass 2 does not start without explicit user approval.

### PASS 2: STUBS + WIRING

1. The stub checklist maps every introduced type, registration point, persistence boundary, consumer migration, legacy removal, and validation case.
2. Final or approved-near-final interfaces compile with deterministic safe stub behavior.
3. Academy is wired as the first consumer without implementing reward resolution or grant behavior.
4. Conflicting legacy execution paths are disconnected or explicitly tracked for removal.
5. Test skeletons reference every baseline case ID.
6. Pass 3 does not start without explicit user approval.

### PASS 3: IMPLEMENTATION + TESTS

1. All required resolution, snapshot, claim, transaction, handler, content-loading, and view-model behavior is implemented.
2. Academy activity and course rewards support the approved combinations and progression blocking.
3. Academy reward consumers are migrated and their legacy path is removed; other consumer migrations and removals are explicitly deferred.
4. Every baseline case is `Implemented` or explicitly `Deferred` with rationale and follow-up.
5. Relevant C#, GDScript, content-validation, serialization, and full project checks pass.

### PR REVIEW: READY

1. The PR review confirms phase order, approval evidence, required artifacts, architecture placement, and validation mapping.
2. The review finds no unresolved correctness, data-loss, determinism, or duplicate-grant issue.
3. CI is green and the branch contains no unrelated user files.

Local review result (`2026-07-25`): ready. The autonomous review loop found and
fixed course-choice progression, persisted-promise fail-closed handling,
normalized grant-view layering, stable claim identity, handler validation,
category preview display, duplicate registration, tracker alignment, and stale
Pass 2 runtime status issues. No unresolved major or minor findings remain.
Remote CI remains to be evaluated after a PR is created.

## Open Risks

1. Atomic reward claims require a new profile transaction boundary because current repository methods mutate and autosave independently.
2. Migrating reward-bearing source definitions to JSON affects content loading as well as the reward engine and must preserve stable source IDs.
3. Polymorphic JSON and immutable persisted snapshots require explicit serialization tests and versioning discipline.
4. Replacing all legacy consumers is broad; temporary duplication must not become a permanent compatibility layer.
5. Exact preview resolution can mutate profile state from a read-oriented screen, so the first-reveal command boundary must be explicit.

## Assumptions and Defaults

1. The game is pre-release enough to permit breaking reward schema and development-save changes.
2. The summoner is the Academy randomization owner; an account-only seed is not used for Academy pool resolution.
3. Stable context includes source type, source ID, activity/course occurrence, offer ID, and resolution version.
4. Candidate collections are canonically ordered before deterministic sampling.
5. Complete snapshots remain valid even if the source offer or pool later changes or is removed.
6. Duplicate and ownership rules are authored per offer/pool and evaluated before sampling.
7. An activity with no reward continues normally; an activity with unresolved selectable offers blocks only later class progression.
8. Practical Spellcraft reward selection begins only after this universal foundation is implemented.

## Pass Gate Status

Current state:

1. `PASS 1: USE CASES + VALIDATION` - complete
2. `PASS 2: STUBS + WIRING` - complete
3. `PASS 3: IMPLEMENTATION + TESTS` - complete for the universal engine and Academy consumer
4. `URS-C24` battle migration - deferred pending a stable battle-attempt identity
5. `URS-C25` shop/event/campaign migration - deferred pending source transaction identities

Gate note:

1. Pass 1 to Pass 2 approval was explicitly recorded in the delivery thread on `2026-07-25`: `ready`.
2. Pass 2 to Pass 3 approval was explicitly recorded in the delivery thread on `2026-07-25`: `next`.
3. The next required workflow step is `PR REVIEW: READY`.
