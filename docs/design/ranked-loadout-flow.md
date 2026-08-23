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
- Changing the ranked deck opens the shared collection/deck-management overlay
  in a ranked-selection context. Online remains visible and dimmed behind it;
  confirming a valid deck closes the overlay and resumes the same Online screen.
- Matchmaking and deck exchange use the remembered ranked deck, not the offline
  active deck.

## Online Screen Scaffold

The functional scaffold contains:

- current competitive tier and league points;
- centered active-summoner artwork that acts as the change-summoner control;
- the selected deck's actual cards, with the deck rail acting as the
  deck-management control;
- one queue action with a clear missing/invalid-loadout state.

This specifies information and behavior for design handoff, not final art,
spacing, typography, or component styling.
