# Campus Shop Requirements

**Status:** Current

The Merriweathers own and operate the Academy Campus Shop. It is a persistent
campus destination, not a traveling or progression-node event.

- Purchases use account resources, principally account gold.
- Offerings and purchase limits are owned by `ShopService`.
- Card, item, cosmetic, and other grants use the shared reward and profile
  contracts; shop code does not duplicate ownership mutations.
- Quest content may direct a player to the shop, but quests do not own the shop
  catalog or execute purchases.
- Critical quest completion cannot depend on an unannounced rotating offer.
- Purchase state persists independently from per-summoner quest progress.

Mrs. Merriweather leads customer conversation and merchandising; Mr.
Merriweather handles stock, repairs, sourcing, and appraisal. Their current
character intent is documented in `docs/lore/characters/npcs/`.
