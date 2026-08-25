# Academy World Definition

**Status:** Working design notes — physical details are not yet accepted canon
**Type:** Product and worldbuilding workspace
**Started:** 2026-08-24

## Purpose

Define the Academy of Summoning Arts as a believable physical place before
authoring the opening sequence or expanding quest content. This document owns:

- where the Academy is in the wider world;
- why it was built there and how people reach it;
- the campus's geographic boundaries, districts, and circulation;
- which buildings and outdoor places exist;
- what the player can do at each place;
- how institutional history and culture appear in the environment;
- the relative placement and relationships between important locations.

The exact geography, architecture, building roster, and layout remain open
until explicitly accepted. Ideas recorded as candidates are not canon.

## Relationship to Other Documents

- `docs/lore/world.md` defines the wider setting and the Academy's institutional
  character.
- `docs/design/walkable-academy-hub.md` defines traversal, Travel, overlays, and
  feature-routing behavior.
- `docs/design/quest-system.md` defines how characters, landmarks, objectives,
  and encounters participate in quests.
- This document defines the physical world those systems inhabit.

## Already Accepted Constraints

- The Academy is a bright, welcoming magical institution with a quietly
  ruthless meritocratic core.
- The material culture is broadly pre-industrial, while magic can support
  sophisticated institutions, craft, and infrastructure.
- The central campus is a compact, bounded, top-down walkable home base rather
  than an open world or a destination menu.
- Buildings and landmarks are discrete placeable locations. The campus can
  change without repainting one monolithic background.
- The Merriweathers own a permanent Campus Shop. The shop's appearance and
  location are not yet settled.
- Competitive play has a physical Arena presence on campus. Its form and
  location are not yet settled.
- Professors can occupy recognizable outdoor landmarks; they do not each need a
  dedicated classroom or elemental mini-biome.
- Spellbook, Journal, Inventory, Summoner Profile, Friends, and Settings are UI
  tools, not buildings that must be duplicated in the world.
- A covert underground area is an accepted direction for illicit card cracking
  and progression rituals, but its origin, entrance, layout, and exact status
  within the Academy are open.

## Inherited Names Are Provisional

Several earlier documents or graybox scenes use location names without having
defined them as places. They must not be treated as required campus canon.

| Working name | What currently exists | Status |
|---|---|---|
| Crystal Chamber | One old sentence about affinity-revealing stones | Provisional; keep, replace, combine, or delete |
| Training Grounds | Generic label for controlled early battles | Provisional; the gameplay need may survive under another place or name |
| Mission Hall | Current graybox route to the special-events screen | Implementation label, not accepted worldbuilding |
| Class Hall | Physical graybox building with no current authoritative screen | Unassigned shell; repurpose or remove |
| Dorms / Residence Hall | A familiar Academy-space concept and current route label | Purpose, necessity, and location unresolved |
| Arena | Physical representation of competitive play | Function accepted; name, form, and location open |
| Campus Shop | Permanent shop owned by the Merriweathers | Function and owners accepted; building details open |

## Questions to Settle

### 1. Place in the Wider World

- What region, nation, or political territory contains the Academy?
- Is it near a city, isolated from ordinary society, or effectively a settlement
  of its own?
- What climate, terrain, and horizon define its visual identity?
- Why was the Academy founded at this exact site?
- How do new students, staff, merchants, and supplies reach it?
- What can be seen beyond the campus, and which nearby places can eventually be
  visited?

### 2. Age, Construction, and Institutional History

- Who founded the Academy, and what existed at the site beforehand?
- Was the campus planned at once or accumulated across different eras?
- Which structures express the welcoming public face?
- Which spaces reveal the institution's harsher values or concealed history?
- How has summoning physically changed the land and architecture?

### 3. Scale and Population

- How many students and staff plausibly live or work here?
- Is the playable campus the entire Academy or one central district of a larger
  institution?
- Where do food, water, materials, creatures, and summoned beings come from?
- Which places need to exist for the world to feel inhabited even when they are
  not feature destinations?

### 4. Interactable Place Roster

Every interactable place needs at least one clear job:

- a player-system job;
- a recurring character or social job;
- a quest or discovery job;
- an institutional/worldbuilding job that benefits from physical interaction.

A building should not exist merely to duplicate a persistent UI action. Places
may be exterior landmarks, courtyards, gates, bridges, gardens, workshops,
ruins, or rooms; they do not all need to be conventional buildings.

| Place | Player-facing job | World/character job | Required relationships | Decision status |
|---|---|---|---|---|
| Campus Shop | Permanent purchases | Home and livelihood of the Merriweathers | Accessible from ordinary campus circulation | Function accepted; details open |
| Competitive venue | Enter online play | Public prestige and institutional competition | Easy to reach without dominating academic space | Function accepted; details open |
| Professor landmarks | Conversation and quest interaction | Give each professor a regular place in campus life | Distributed, memorable, compact | Pattern accepted; roster open |
| Underground area | Cracking and rituals | Concealed counterculture beneath official Academy life | Secret access; no automatic Travel waypoint | Direction accepted; details open |
| Arrival/departure place | Enter campus and begin excursions | Connect the institution to the wider world | Must explain travel and supplies | Open |
| Academy authority space | Institutional interactions | Express Merlin and the Academy's public identity | Likely prominent, but not necessarily central | Open |
| Practice/battle access | Reach controlled encounters | Show how dangerous magic is taught safely | Relationship to Arena and excursions unresolved | Open |
| Research/knowledge place | Research, discovery, or quest interactions | Preserve and restrict magical knowledge | Must not duplicate the Spellbook UI | Open |
| Student social place | Character encounters and campus-life quests | Make the Academy feel inhabited | Should lie on natural daily circulation | Open |
| Residential/support spaces | Contextual interactions if justified | Explain daily life and campus logistics | Need not all be enterable | Open |

## Layout Workbench

Do not lock a map until the geographic premise and place roster are accepted.
When they are, define the campus through relationships before exact coordinates:

1. arrival and first view;
2. primary public route;
3. institutional center or centers;
4. everyday student circulation;
5. quiet, restricted, dangerous, and hidden edges;
6. outward routes to excursions;
7. sightlines and landmarks used for player orientation;
8. Travel waypoints that shorten repetition without erasing place.

## Decision Order

1. Settle the Academy's geographic premise and relationship to the wider world.
2. Settle its scale, age, and campus identity.
3. Approve the interactable-place roster by function.
4. Decide districts, adjacencies, and circulation.
5. Produce a campus map and name the accepted places.
6. Only then author architecture, environmental stories, characters, and quests
   against those locations.

## Explicit Non-Decisions

- No caldera, island, mountain, city, valley, or other geographic proposal is
  accepted yet.
- The Crystal Chamber is not a required place.
- The Training Grounds, Mission Hall, Class Hall, and Dorms are not settled
  names or required buildings.
- The first playable introduction does not determine the campus layout.
- Placeholder scene positions do not establish canon geography.
