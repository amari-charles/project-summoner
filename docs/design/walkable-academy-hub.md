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

- **Shortcuts to every location always exist.** Walking is never required to
  reach a destination. The shortcut menu is the fast path; movement supplies
  atmosphere and future social presence.
- **Buildings are discrete placeable objects.** Ground, buildings, and later
  paths or decorations are separate pieces. A destination can be added or moved
  without repainting the whole campus.
- **Existing feature screens remain authoritative.** Entering a building routes
  to today's Class Hall, Campus Shop, Mission Hall, Dorms, or Online screen; the
  hub does not reimplement their functionality.
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
| Class Hall | `SCENE_ACADEMY_CLASS_HALL` |
| Campus Shop | `SCENE_SHOP_SCREEN` |
| Mission Hall | `SCENE_SPECIAL_EVENTS` |
| Dorms | `SCENE_COLLECTION_SCREEN` |
| Online Arena | `SCENE_ONLINE` |

Settings and the summoner screen are shortcut destinations without Phase 1
building requirements.

## Phase 1 Non-Goals

- Multiplayer presence, chat, emotes, or hub instancing.
- Final campus art, animation, lighting, or environmental dressing.
- Navmesh or point-and-click pathfinding.
- Reimplementing the feature screens behind each destination.
- Deleting the former menu hub before the recovered route is validated.
