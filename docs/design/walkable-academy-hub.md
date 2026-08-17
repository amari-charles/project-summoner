# Walkable Academy Hub

**Status:** Design accepted — bounded hub recovery in progress (Phase 1)
**Type:** Product/design intent (source of truth for the Academy hub direction)
**Decision restored:** 2026-08-13

---

## Decision

The Academy's primary entry surface is a **bounded, top-down walkable hub**. The
player moves around one contained campus and enters discrete buildings to reach
the existing meta-game screens.

**This is a hub, not an open world.** There is no overworld map, stamina,
exploration grind, or open-world content burden. Its scope is deliberately one
room-sized shared space.

## Core Design Facts

- **The campus is the recurring home base.** The usual player rhythm begins at
  the campus with preparation and activity selection, continues into an
  excursion, and returns to campus for progression and the next choice.
- **An excursion can contain connected sub-locations.** Leaving campus does not
  imply that every forest, ruin, or other site is a separate destination. When
  the geography and quest make sense, the player can continue from one bounded
  area into another—for example, from a forest into ruins located within it—
  before returning to campus.
- **Shortcuts to every location always exist.** Walking is never required to
  reach a destination. The shortcut menu is the fast path; movement supplies
  atmosphere and future social presence.
- **Buildings are discrete placeable objects.** Ground, buildings, and later
  paths or decorations are separate pieces. A destination can be added or moved
  without repainting the whole campus.
- **Existing non-quest feature screens remain authoritative.** Entering a
  building routes to the Campus Shop, Mission Hall, or Online screen. The
  persistent Spellbook HUD action routes directly to collection and deck
  management; the hub does not reimplement their functionality. Quest
  acceptance and progression happen through NPCs, world targets, the Journal,
  and generic encounter screens rather than the retired Class Hall/Course Flow UI.
- **The competitive loop stays fast.** Online play and deck management remain
  immediately accessible through shortcuts.
- **The former menu hub remains available as a fallback during recovery.** It is
  not the target player experience and should not become a second source of
  routing truth.

## Rationale

1. **Social is the long-term reason.** The campus can eventually become a shared
   space where players see one another between matches. A menu cannot provide
   that sense of presence.
2. **The Academy becomes a place.** Walking through a magic campus reinforces
   the student fantasy instead of reducing it to a list of destinations.
3. **Placeable pieces keep expansion bounded.** New locations can be introduced
   incrementally without committing to an open-world content model.
4. **Shortcuts preserve convenience.** The walkable layer never needs to compete
   with direct menu navigation for speed.

The campus being a hub does not require all world locations to connect directly
to it or require a campus return between every quest stage. Exact departure,
unlocking, and fast-travel rules follow from the approved world roster rather
than being imposed before that roster exists.

## Hidden Underground Layer

The Academy can contain a persistent, bounded underground tunnel area that
functions like a small secondary hub rather than a default combat excursion. It
is reached through interaction with a covert campus contact, not through a
permanent shortcut or additional destination UI. This preserves secrecy, keeps
the interface lean, and makes the player engage with a character to enter.

Its minimum justified functions are:

- an illicit card-cracking operator or room;
- a dedicated room for card-progression rituals.

The tunnel environment can later support secret NPCs, discoveries, or authored
quest events, but those are opportunities rather than requirements. Combat is
not assumed merely because the area is underground. A small reusable tunnel kit
and bounded layout are preferred over requiring a full black-market district.

## Professor Landmarks

The five initial professors exist on the central campus from the beginning. The
general professor occupies a prominent foundational area. Fire, Water, Earth,
and Wind professors occupy compact subject-appropriate campus landmarks rather
than separate maps, dedicated classroom interiors, or a grouped selection area.

Their authoritative quest-marker, interaction, and Journal behavior is defined
in [Quest System](quest-system.md). Final landmark composition remains map-design
work.

## Phasing

1. **Phase 1 (current): single-player bounded hub.** The avatar moves inside
   fixed boundaries; placeholder buildings expose interaction zones; entering a
   building opens its existing screen; the shortcut menu reaches every campus
   destination. No networking.
2. **Phase 2: real-time presence (Nakama).** Other players' avatars appear and
   move in the campus.
3. **Phase 3: social interaction.** Emotes, chat, and name tags.
4. **Phase 4: instancing/capacity.** Introduce bounded player populations per
   campus instance if presence requires it.

Shortcuts remain available through every phase.

## Building-to-Screen Routing

| Building | Target scene constant |
|---|---|
| Campus Shop | `SCENE_SHOP_SCREEN` |
| Mission Hall | `SCENE_SPECIAL_EVENTS` |
| Online Arena | `SCENE_ONLINE` |

The Spellbook (`SCENE_COLLECTION_SCREEN`) is part of the persistent right-side
HUD action rail and has no physical campus building requirement. The Journal,
settings, and the summoner screen are shortcut destinations without Phase 1
building requirements. The physical Class Hall may be repurposed later, but it
is not currently a feature-screen destination.

## Phase 1 Non-Goals

- Multiplayer presence, chat, emotes, or hub instancing.
- Final campus art, animation, lighting, or environmental dressing.
- Navmesh or point-and-click pathfinding.
- Reimplementing the feature screens behind each destination.
- Deleting the former menu hub before the recovered route is validated.
