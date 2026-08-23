# Friends and Shared Presence

**Status:** Future-facing product direction — UI placement accepted; implementation scope deferred
**Type:** Product/design intent
**Decision recorded:** 2026-08-23

---

## Purpose

Friends is the long-term social connection between the Academy, battles, quests,
and possible shared-world play. It is not merely a contact list and should not be
designed as a battle-only feature.

None of the capabilities in this document are required for the initial release
unless they are separately promoted into release scope. The immediate design
need is to leave a coherent place for the system without building dead UI or
premature networking infrastructure.

## Accepted UI Placement

Friends belongs in a persistent panel opened from the walkable campus HUD's
right-side action rail. It sits with other global player utilities rather than
occupying a physical campus building or living inside Settings.

- The panel is available from the campus and other future non-combat walkable
  spaces where the persistent HUD is present.
- Opening it should preserve the current world context rather than navigating
  away to a disconnected full-screen destination by default.
- The action rail should be designed with enough capacity for a Friends entry,
  but a nonfunctional Friends button should not ship merely to reserve the slot.
- Battle-specific social actions can later surface contextually, but the battle
  HUD does not need the persistent Friends panel.

## Potential Capabilities

The social system may eventually support:

- sending, accepting, and declining friend requests;
- viewing friends and relevant presence state;
- removing friends;
- inviting a friend to a direct battle;
- inviting friends into cooperative or joint quests;
- forming a party and moving through supported maps together;
- sending gifts, subject to economy and exclusivity safeguards;
- seeing friends and strangers in a shared Academy hub.

These capabilities can coexist, but listing them does not commit them to the
same milestone or require one oversized first implementation.

## Shared Academy Direction

The bounded Academy may eventually become a populated shared space. Friends and
strangers could appear in the same campus instance, making the Academy feel like
a real institution rather than a private menu with walking.

This is closer to an instanced server or social hub than to an unrestricted open
world:

- each Academy instance can have a bounded population;
- player presence and movement are synchronized inside that instance;
- friend relationships affect discovery and invitations, but are not required
  for strangers to share the space;
- parties can remain together when entering activities that support cooperative
  play;
- shortcuts and direct feature access remain available even when the campus is
  populated.

The exact server topology, instance size, visibility rules, and transition
behavior remain technical and product decisions for a later multiplayer pass.

## Activity Integration

Friends should invite players into authored game activities rather than create a
separate social minigame.

### Battles

A friend invitation can create or join a direct battle using the normal battle
format. Deck confirmation, eligibility, and result authority remain owned by the
online battle flow rather than by the Friends panel.

### Joint Quests

A quest can explicitly support a party without making every quest cooperative.
Later design must define who owns quest acceptance, how stages synchronize, what
happens when party members have different progress, and how rewards preserve the
game's exclusivity and choice constraints.

### Shared Exploration

Supported maps may allow a party to run around together. This does not imply
that every campus area, excursion, dialogue, or battle becomes multiplayer. Each
activity needs an explicit participation contract.

### Gifts

Gifting must respect permanent choices, scarce rewards, account ownership, and
the intended card ecosystem. A social connection must never become an accidental
bypass around exclusivity or progression gates.

## Capability Boundaries

The system should eventually separate these concerns even if one panel presents
them together:

- **Relationship:** requests, friendship state, removal, blocking.
- **Presence:** online state, current location/activity, joinability.
- **Party and invitations:** temporary group membership and activity invites.
- **Shared-space membership:** which players occupy a hub or map instance.
- **Gifting:** validated, authoritative transfer or grant rules.

This separation allows battle invitations to exist before shared-map movement,
or shared Academy presence to exist before joint quests.

## Safety and Privacy Requirements

Any implementation involving strangers or persistent relationships will need
privacy, blocking, reporting, invitation permissions, and presence-visibility
rules. These are requirements of the eventual social feature, not optional
polish. Their exact player-facing behavior is intentionally undecided.

## Deferred Decisions

- Whether a compact panel is sufficient for every social action or expands into
  a larger management surface when needed.
- Which capability is the first shippable social slice.
- Friend limits and request-discovery methods.
- Whether text chat, voice chat, emotes, or only structured interactions exist.
- Hub instance population and matchmaking rules.
- Party leadership, quest-state ownership, disconnect, and rejoin behavior.
- Gift eligibility, costs, limits, and allowed reward types.
- Cross-platform identity and account-discovery rules.

## Designer Handoff

The campus HUD should reserve conceptual room for a Friends/social action in the
right-side rail. No detailed Friends panel screens are required for the current
handoff. If the designer explores it, the minimum useful state set is closed,
compact list, incoming request, friend online/offline, and activity invitation;
those explorations remain non-release concepts until separately approved.

## Related Documents

- [Walkable Academy Hub](walkable-academy-hub.md)
- [Quest System](quest-system.md)
