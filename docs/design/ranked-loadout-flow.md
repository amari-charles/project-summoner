# Ranked Loadout Flow

**Status:** Accepted functional direction; visual design remains replaceable

## Decision

The Online screen shows the active summoner and that summoner's selected ranked
deck before matchmaking. Queueing is unavailable until the selection exists and
the deck is battle-ready.

- Summoner selection remains global. Changing it from Online changes the active
  summoner everywhere.
- Ranked deck selection is separate from the deck used by offline activities.
- Each summoner remembers one ranked deck because decks and cards belong to a
  summoner rather than a shared account collection.
- A summoner with no remembered ranked deck must choose one explicitly. The game
  does not silently use the first deck or overwrite the offline selection.
- Changing the ranked deck reuses the collection/deck-management screen in a
  ranked-selection context. Confirming a valid deck returns to Online.
- Matchmaking and deck exchange use the remembered ranked deck, not the offline
  active deck.

## Online Screen Scaffold

The functional scaffold contains:

- current competitive tier and league points;
- compact active-summoner artwork and ranked-deck summary;
- Change Summoner and Change Deck actions;
- one queue action with a clear missing/invalid-loadout state.

This specifies information and behavior for design handoff, not final art,
spacing, typography, or component styling.
