# Campus Shop Architecture

**Status:** Current

`ShopService` owns offering lookup, availability, price calculation, refresh
state, purchase limits, and purchase transactions. It reads and writes through
`ProfileRepository`; account resource mutations use `EconomyService` contracts,
and content grants use shared reward handlers.

The walkable campus opens the ordinary shop screen. Narrative Director may
present Merriweather dialogue around the visit, but narrative cues never mutate
inventory or complete transactions.

Shop purchase keys and refresh epochs are account-level profile state. They are
not stored with `SummonerProgress`.
