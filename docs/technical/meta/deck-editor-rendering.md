# Deck Editor Rendering

The shared `DeckEditorPanel` is used by both the Dorms collection screen and Academy activity preparation. Its available-card grid preserves one `CardWidget` per card instance and reconciles membership and ordering instead of rebuilding the whole grid.

## Refresh Contract

The collection screen maintains a filtered, sorted cache containing card catalog and progression data.

- A collection, progression, filter, search, or sort change rebuilds the cache and calls `set_available_cards(entries, true)` so existing widgets receive updated content.
- A deck membership or selected-deck change does not invalidate card content. It calls `set_available_cards(entries, false)`, allowing the panel to add or remove only affected widgets without restyling the rest of the collection.
- `DeckService.DeckChanged` is the single refresh trigger after successful add and remove operations. Callers must not add a second manual refresh after those service calls.

This distinction keeps expensive UI mutations proportional to the membership change rather than recreating every owned card. Initial screen entry and actual collection/filter changes still perform the necessary full content refresh.
