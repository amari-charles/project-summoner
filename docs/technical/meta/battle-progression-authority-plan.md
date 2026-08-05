# Battle Progression Authority Initiative Plan

**Status:** PASS 3 COMPLETE — READY FOR PR REVIEW
**Initiative:** `battle-progression-authority`
**Domain:** `meta`
**Last Updated:** `2026-08-05`
**Owner:** `Codex + User`

## Summary

Introduce a backend-neutral authority boundary for campaign battle attempts, outcomes, XP, and first-clear rewards. The first adapter remains local and persists through `ProfileRepository`; a future secure adapter may call any backend without changing battle, reward-domain, or UI code. Battle outcomes become coarse-grained authority commands rather than direct calls from `BattleScene` into card, summoner, campaign, and reward services. This initiative also supplies the stable battle occurrence identity required to finish the universal battle-reward migration and delete its legacy reward path.

## Goals

1. Persist a typed, authority-created battle attempt before campaign battle launch.
2. Award card and summoner XP once per victorious attempt, including replay victories, and never on defeat or abandonment.
3. Award first-clear rewards once per summoner/campaign/battle identity through the universal reward engine.
4. Make outcome completion and retries idempotent, persistence-safe, and independent of reward-screen navigation.
5. Keep authority contracts free of Godot, JSON-store, Nakama, or other backend-provider types.
6. Replace direct battle progression mutations and remove the superseded battle reward contracts.

## Non-Goals

1. Selecting Nakama or any other production backend.
2. Providing anti-cheat guarantees while the local adapter is authoritative.
3. Building battle simulation verification, replay upload, or a remote adapter in this initiative.
4. Migrating Academy, shop, item, trait, or ranked mutations to remote authority in this initiative; those are tracked follow-ups.
5. Redesigning reward-screen visuals or changing authored battle reward balance.

## Architecture Decisions

1. `IProgressionAuthority` is an application port, not a repository. It exposes coarse-grained player-intent operations and returns normalized results; consumers never request arbitrary profile mutations.
2. The initial port is deliberately narrow: start a campaign battle attempt, complete it with a typed terminal outcome, inspect its normalized reward presentation, and submit a reward selection.
3. `LocalProgressionAuthority` implements the port using the existing profile repository and universal reward runtime. A future remote adapter can implement the same contract over any backend.
4. The authority creates `BattleAttemptId`. The local adapter uses a cryptographically random 128-bit identifier; a future remote adapter receives an ID created by the server. The identifier provides uniqueness and idempotency, not authorization by itself.
5. `BattleAttempt` is scoped to account/profile, summoner, campaign, battle, and attempt. It records a lifecycle state (`Started`, `Victory`, `Defeat`, or `Abandoned`) and enough source identity to reproduce claim IDs.
6. Starting an attempt persists `Started` before scene navigation. Starting another attempt abandons any stale active attempt without granting rewards.
7. A terminal result is recorded when the authoritative session emits game over, not when the player confirms the UI transition. Leaving an unfinished battle records `Abandoned`.
8. Victory creates an attempt-scoped XP claim. A unique attempt can grant its XP once; a replay uses a new attempt and therefore earns XP again.
9. First-clear offers use a stable summoner/campaign/battle occurrence identity rather than the attempt identity. Replays cannot repeat cards, currency, or other first-clear grants.
10. Defeat and abandonment create no XP claim and no first-clear claim.
11. Completion is retry-safe. Repeating the same terminal command returns the persisted result; a conflicting outcome or mismatched summoner/campaign/battle is rejected.
12. The local implementation must commit terminal attempt state, automatic grants, reward receipts, campaign completion, and pending selection state through one durable application transaction or an equivalent fail-closed boundary. UI navigation happens only after that operation returns.
13. The local profile remains explicitly untrusted. The future secure design moves the same application operations and transaction ownership behind a remote adapter; local state then becomes a cache/read model.
14. Do not grow `IProgressionAuthority` into a universal service locator. Separate authority ports will own commerce and competitive operations because their transaction and validation rules change for different reasons.

## Public API / Interface / Type Changes

1. Add pure typed contracts under `scripts/csharp/Meta/Domain/Progression/`:
   - `BattleAttemptId`
   - `BattleAttempt`
   - `BattleAttemptState`
   - `BattleTerminalOutcome`
   - start/complete/claim request and result records
2. Add application coordination under `scripts/csharp/Meta/Services/Progression/`:
   - `IProgressionAuthority`
   - `LocalProgressionAuthority`
   - `BattleOutcomeCoordinator`
3. Add persisted attempt state to the per-summoner campaign profile shape and explicit mapper coverage.
4. Carry attempt identity in typed battle session configuration so battle runtime reports the exact launched occurrence.
5. Return normalized universal reward view models and receipts through the authority boundary; GDScript submits only attempt, claim, and option IDs.

## Placement

1. Attempt identity and lifecycle records belong in `Meta/Domain/Progression` because they describe durable player progression state and contain no persistence or engine behavior.
2. Authority interfaces and local orchestration belong in `Meta/Services/Progression` because they coordinate campaign, reward, XP, and persistence owners at an application use-case boundary.
3. Profile serialization remains in `Infrastructure/Persistence`; it implements storage, not outcome policy.
4. Battle session config carries identity but does not grant rewards. `BattleScene` reports terminal outcomes to the coordinator and renders/navigates from returned state.

## Legacy Removal Scope

1. Remove direct `BattleScene.GrantCardXp()` and `BattleScene.GrantSummonerXp()` calls and methods.
2. Remove battle completion logic that reaches card, summoner, campaign, or profile autoloads independently.
3. Replace `BattleRewardSpec` and battle-specific `PendingRewardData` with universal offers, pending selections, and receipts.
4. Remove reward-screen interpretation of `is_replay`, `requires_choice`, battle `reward_type`, and legacy current-battle fallbacks after typed authority wiring is complete.
5. Remove migrated direct-grant methods from `CampaignRewardHandler`; retain only campaign behavior not superseded by universal rewards or progression authority.

## Related Authority Audit

The 2026-08-05 trust-boundary audit found three follow-up groups. They are tracked in `docs/tracking/todos.md` and are intentionally outside this initiative:

1. Permanent progression commands: Academy completion, campaign choices, leveling, trait spending, and item equipment currently trust local service calls. Extend capability-specific progression ports before secure account migration.
2. Commerce/economy: shop purchase flows currently spend, grant, roll back, and increment limits through separate local calls. Introduce an atomic commerce authority before real-money or server-valued economies ship.
3. Competitive results: ranked rating is calculated and persisted locally before a client-authored match report is submitted. Move match result validation and rating ownership to a competitive authority; validate submitted deck/equipment ownership at match start.

Local settings, UI preferences, debug state, and ordinary deck editing are not authority migrations by themselves. A secure competitive service must validate the resulting loadout, but the editor may remain client-side.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Outcome, replay, failure, persistence, ownership, and backend-migration semantics are decision-complete.
2. Every baseline scenario maps to a test type and target file.
3. Related valuable-state mutation systems are audited and concrete follow-ups are added to the tracker.
4. No production stubs or runtime wiring are added before explicit Pass 2 approval.

### PASS 2: STUBS + WIRING

1. Final or approved-near-final domain records and `IProgressionAuthority` compile without Godot or provider dependencies.
2. The local adapter, outcome coordinator, typed session config, persistence fields, and normalized reward boundary are wired with deterministic fail-closed stubs.
3. Legacy direct XP execution is disconnected so no parallel grant path remains.
4. Test skeletons cover every validation case and the stub checklist records all wiring/removal points.
5. Pass 3 does not start without explicit approval.

### PASS 3: IMPLEMENTATION + TESTS

1. Local attempt lifecycle, atomic victory handling, universal first-clear rewards, repeatable attempt XP, defeat, and abandonment are fully implemented.
2. Persistence/reload and duplicate/concurrent request behavior pass mapped tests.
3. Reward screen consumes only normalized authority output and all scoped legacy battle reward code is deleted.
4. All cases are `Implemented` or explicitly `Deferred` with a follow-up target.

### PR REVIEW: READY

1. Review confirms pass order, artifact completeness, boundary placement, and no direct battle-to-profile mutation path.
2. Review confirms local trust limitations are documented and no backend provider leaked into domain/application contracts.
3. CI and focused reward/progression/persistence tests pass with no unrelated user files included.

## Open Risks

1. A future secure backend still requires authoritative outcome validation. Swapping adapters alone does not make a client-reported victory trustworthy.

## Assumptions and Defaults

1. Development saves may be discarded. New architecture takes priority over backward compatibility, and no legacy reader, adapter, or migration shim is required.
2. Campaign battle XP applies to the active summoner and the exact deck card instances captured at attempt start.
3. Victory XP is awarded for every distinct victorious attempt, including replay victories.
4. Defeat and abandonment award no XP and no first-clear reward.
5. An interrupted `Started` attempt is abandoned rather than resumed unless battle-resume support is designed later.
6. Backend technology remains undecided; contracts use serializable primitives and stable typed identifiers only.

## Pass Gate Status

Current state:

1. `PASS 1: USE CASES + VALIDATION` — complete
2. `PASS 2: STUBS + WIRING` — complete
3. `PASS 3: IMPLEMENTATION + TESTS` — complete
4. `PR REVIEW: READY` — not started

Gate note:

1. Pass 2 approval was explicitly recorded on `2026-08-05`: `oass 2`.
2. Pass 3 approval was explicitly recorded on `2026-08-05`: `pass 3`.
3. Pass 3 implementation and full local verification are complete; PR review is the next gate.
