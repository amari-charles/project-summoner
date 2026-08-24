# Card Progression and Economy

**Updated:** 2026-08-24

Cards may be account-owned or bound to a summoner. A summoner's usable deck is
resolved from that durable ownership plus any explicit encounter loadout.

## Acquisition

- Quest and encounter rewards are authored as universal reward offers.
- The Campus Shop sells permanent account or summoner progression content.
- Debug authored battles may grant XP and first-clear offers through
  `ProgressionAuthority`.
- Reward claims must have stable source identity and be idempotent.

## Progression

Card levels use card XP. Summoner levels use summoner XP. Leveling is not gated
by gold. Encounters may impose fixed, owned, or flexible loadouts without
changing the underlying collection.

## Currency

Gold, gems, essence, and fragments are account resources owned by `Economy`.
The Campus Shop is the permanent shop and the Merriweathers are its owners.

See [Quest System](quest-system.md),
[Reward Architecture](../technical/runtime/reward-system-architecture.md), and
[Campus Shop Requirements](../features/shop/requirements.md).
