# Battle Progression Authority Delivery Checklist

**Status:** PR REVIEW COMPLETE — READY FOR MERGE APPROVAL
**Initiative:** `battle-progression-authority`
**Domain:** `meta`
**Last Updated:** `2026-08-05`

## Approval Evidence

1. Pass 2 was explicitly approved on `2026-08-05`: `oass 2`.
2. Pass 3 was explicitly approved on `2026-08-05`: `pass 3`.
3. The user explicitly chose ideal architecture over save compatibility; development saves may be discarded and no legacy persistence shim is retained.

## Implemented Boundary

1. `IProgressionAuthority` owns battle start, terminal completion, reward retrieval, pending retrieval, and reward selection through provider-neutral commands and results.
2. `LocalProgressionAuthority` persists cryptographically random attempt IDs, one durable summoner reward seed, authority-derived selected-deck card identities, frozen XP/reward snapshots, terminal receipts, and claim receipts.
3. `IProgressionProfileStore.TryCommitProgression` stages attempt state, campaign completion, grants, pending selections, and receipts against one cloned profile and performs one save.
4. Victory awards attempt-scoped card and summoner XP. Defeat and abandonment award nothing. Replays award XP but cannot repeat first-clear rewards.
5. First-clear offers support automatic, selectable, mixed, and absent rewards through universal reward definitions.
6. Card grants carry an explicit placement policy; tutorial offers request selected-deck placement without reward-screen mutation logic.
7. Reload reads frozen resolved snapshots; catalog changes and screen reloads do not reroll a promise.
8. Guard mutations make duplicate and competing completion/claim requests fail before any grant mutation, including across separate local adapter instances.
9. Campaign navigation caches refresh from profile changes so unlock state follows the authority-owned durable state.

## Runtime Wiring

1. Campaign and debug launches create and persist an attempt before navigation.
2. `BattleContext` and `BattleSessionConfig` carry only the attempt identity needed by runtime.
3. `BattleScene` reports victory/defeat at game over and abandonment only for unfinished exits; UI confirmation does not grant progression.
4. `RewardScreen` consumes normalized offers and submits only attempt, claim, and option IDs. It can resume pending claims after reload and process multiple sequential choices.
5. `ProgressionAuthorityService` is the Godot adapter; domain/application contracts contain no Godot, Nakama, or persistence-provider types.

## Legacy Removal

1. Removed battle `RewardType`, `BattleRewardConfig`, `FixedRewardEntry`, `BattleRewardSpec`, `PendingRewardData`, and `CampaignRewardHandler`.
2. Removed `current_battle`, legacy reward flags, direct battle XP calls, campaign pending-reward APIs, and the old reward-service GDScript wrapper.
3. Removed old Academy-seed and pending-reward save readers. Only the new `reward_seed_by_summoner` and universal reward state are accepted.
4. Battle event authoring now stores XP and universal first-clear offers directly; no backward-compatibility adapter remains.

## Verification

1. `dotnet build` — passed with 0 warnings and 0 errors.
2. Focused progression/persistence suite — 22 passed.
3. Full C# suite — 1,178 passed.
4. Full Godot/GUT suite — 237 passed with 1,746 assertions.
5. Validation matrix has no deferred battle-authority cases.

## Next Gate

PR #352 passed local review and full validation. Merge only after explicit user approval. The unrelated untracked art commission notes file is not part of this initiative.
