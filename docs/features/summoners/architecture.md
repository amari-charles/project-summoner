# Summoner Architecture

**Status:** Current

Summoners have typed catalog definitions and profile-owned instances. A
`SummonerInstance` owns level, XP, acquired traits, and equipped item references.
Cards and normal items are bound explicitly rather than inferred from a route or
playthrough.

`SummonerSelectionService` resolves the active summoner. `DeckService`, quest
state, encounter loadouts, battle attempts, and reward targeting use that typed
summoner identity.

Per-summoner quest and authored-battle state lives in
`ProfileData.SummonerProgressMap`. Account resources, cosmetics, emotes, and
universal reward receipts remain account aggregates.
