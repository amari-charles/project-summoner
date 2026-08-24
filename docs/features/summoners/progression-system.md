# Summoner Progression

**Status:** Current foundation

Summoners gain XP from successful authored battles and other explicitly authored
rewards. Level and trait changes are applied through typed progression/reward
handlers and persisted on `SummonerInstance`.

Quest acceptance and curriculum capacity are separate from summoner leveling.
Quest rewards may target the account, the active summoner, a card instance, or
another supported ownership scope. Permanent choices must be represented by the
owning quest or trait contract, not by an implicit traversal path.

Normal gameplay items are summoner-owned. Shared content must opt into an
account binding explicitly. Decks retain a summoner owner and battle authority
rejects a selected deck belonging to a different summoner.
