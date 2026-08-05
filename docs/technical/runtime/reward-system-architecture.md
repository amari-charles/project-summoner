# Battle Reward and Progression Architecture

## Overview

Campaign battle progression is an authority-owned use case built on the universal reward runtime. Battle content authors XP and zero or more universal first-clear offers. No battle-specific reward enum, spec, pending shape, or grant service exists.

Development saves are disposable at this stage. The architecture intentionally has no reader or adapter for the superseded battle reward schema.

## Ownership

| Concern | Owner |
|---|---|
| Battle XP and first-clear offer authoring | `BattleEventDefinition` / `EventCatalog` |
| Attempt lifecycle and occurrence identity | `IProgressionAuthority` |
| Local durable orchestration | `LocalProgressionAuthority` |
| Atomic profile commit | `IProgressionProfileStore` / `ProfileRepository` |
| Offer resolution and typed grant handlers | `UniversalRewardRuntime` |
| Engine/GDScript boundary | `ProgressionAuthorityService` |
| Presentation and option submission | `RewardScreen` |

## Runtime Flow

1. Campaign or debug launch asks the authority to start a battle.
2. The authority creates a random 128-bit attempt ID, freezes the deck identities, XP amounts, and eligible first-clear resolved offers, then persists before navigation.
3. Battle runtime reports `Victory`, `Defeat`, or `Abandoned` against that attempt ID.
4. On victory, one profile transaction records completion, applies XP and automatic grants, and persists selectable offers. Defeat and abandonment create no grants.
5. The reward screen reads normalized offer state. A selection submits only attempt, claim, and option IDs.
6. Claim validation, grants, pending removal, and receipt creation commit atomically. Retrying returns the existing result.

## Identity and Determinism

- Attempt identity scopes replayable XP: each distinct victorious attempt earns XP once.
- First-clear identity is stable per summoner, campaign, and battle: replay attempts cannot repeat it.
- Each summoner owns a persistent randomization seed.
- Resolved offers are frozen into the attempt at start, so reloads and later catalog edits cannot reroll a promise.
- Claim and completion guards run before grant mutations, preventing competing callers from applying value twice.

## Authoring Shape

```csharp
new BattleEventDefinition
{
    CardXpReward = 15,
    SummonerXpReward = 20,
    FirstClearRewardOffers =
    [
        BattleRewardAuthoring.ChooseOneCard(
            EventIds.FirstTrial,
            30,
            true,
            CardIds.Charge,
            CardIds.FireWisp,
            CardIds.Puff
        )
    ],
};
```

Offers may be automatic or player-selectable and may bundle cards, resources, or any other registered universal grant. A battle may also have XP with no immediate first-clear offer, or no rewards at all.

## Security Boundary

The local adapter is correct and idempotent but not cheat-resistant because the client owns the profile and reports outcomes. A future secure adapter can implement `IProgressionAuthority` remotely without changing battle runtime or reward UI. The server must then validate battle outcomes; changing adapters alone is not anti-cheat.
