# Walkable Academy Hub

**Status:** Design accepted — prototype in progress (Phase 1)
**Type:** Product/design intent (source of truth for the academy hub direction)

---

## Decision

The academy meta-hub is moving from a **menu-style screen** (buttons over a single
background image — see `scripts/meta/screens/academy_hub.gd`) to a **walkable hub**:
a bounded, top-down scene the player walks around, entering buildings to reach each
meta-game feature.

**This is a hub, not an open world.** It is one contained scene (in the spirit of
Destiny's Tower, the House of Hades, or Darkest Dungeon's Hamlet). There is no map to
traverse, no stamina, no exploration grind, and no open-world content burden. The scope
is deliberately one room-sized space.

---

## Core design facts

- **Menu shortcuts to every location always exist.** Walking is never required to reach
  a destination. The shortcut menu is the fast path; the walkable hub is optional
  ambiance and presence. This is a settled design decision, not a future risk to manage.
- **Buildings are discrete placeable objects**, never painted into one baked background.
  The ground is a plain placeholder; each building (and later, trees/paths/decor) is its
  own piece placed at a position. Adding or moving a location is cheap by construction.
- **Competitive loop stays fast.** The game is 1v1 ranked PvP plus single-player campaign;
  because shortcuts exist, ranked queue and deck-building remain one click from anywhere.

---

## Why a walkable hub (rationale)

1. **Social is the real reason.** The long-term goal is a shared space where players see
   each other between matches — presence, belonging, community. A menu fundamentally
   cannot do this; the hub can. This is the primary justification.
2. **It makes the world felt, not just listed.** Walking a magic-academy campus turns
   "you are a student here" into an experience and reinforces existing lore, rather than
   reducing the academy to a list of buttons.
3. **Extensibility, done right.** Buildings as separate pieces means the academy can grow
   over time at low marginal cost — the right version of the original "I can move things
   around later" instinct.
4. **Proven precedent.** Click-to-enter building hubs (Darkest Dungeon's Hamlet), walkable
   social hubs with shortcut menus (Destiny's Tower, FFXIV cities), and worlds built from
   placeable pieces (the city-builder genre) all ship this at scale.

The justification is **social-first**. The hub earns its cost through presence and
atmosphere, backed by the shortcut menu so it never becomes a slower path than the menu
it complements.

---

## Phasing

1. **Phase 1 (current): single-player walkable hub.** Player avatar moves; square
   placeholder buildings have door zones; entering a door opens that building's existing
   menu screen. No networking. Built alongside the existing hub, not replacing it yet.
2. **Phase 2: real-time presence (Nakama).** Other players' avatars appear and move.
3. **Phase 3: social interaction.** Emotes, chat, name tags.
4. **Phase 4: instancing/capacity** (~20–50 players per instance).

Menu shortcuts to all locations remain available through every phase.

---

## Building → screen routing (mirrors current hub)

| Building | Target scene constant |
|---|---|
| Class Hall | `SCENE_ACADEMY_CLASS_HALL` |
| Shop | `SCENE_SHOP_SCREEN` |
| Mission Hall | `SCENE_SPECIAL_EVENTS` |
| Dorms | `SCENE_COLLECTION_SCREEN` |
| Online | `SCENE_ONLINE` |

Plus Settings and the summoner screen (currently icon-accessed).

---

## Non-goals (Phase 1)

- No multiplayer / presence / chat / Nakama.
- No final art — placeholder squares and flat colors only, named obviously temporary.
- No navmesh/pathfinding (direct movement; the battlefield uses none either).
- Do not delete or rewrite the existing `academy_hub`; prototype in parallel.
