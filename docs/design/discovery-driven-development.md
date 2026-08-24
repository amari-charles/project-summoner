# Discovery-Driven Development

**Status:** Accepted direction; detailed content and final presentation remain in design
**Date:** 2026-08-18
**Areas:** Summoners, Cards, Traits, Quests, Rituals, Progression

## 1. Goal

Make character and card development a combination of reliable earned capacity
and world discovery. Repeating activities may contribute XP, but leveling alone
must not expose a complete default upgrade catalog. Players discover traits and
development opportunities, theorize about builds, and commit constrained
resources toward the possibilities their play has made available.

## 2. Shared Progression Grammar

Summoners and cards use the same high-level language without needing identical
content:

1. Gameplay grants entity-specific XP. Summoner XP may come from battles,
   quests, lessons, exploration, and other meaningful activities; it is not a
   battle-only reward.
2. XP thresholds apply levels automatically and award bankable, entity-bound
   development points.
3. Summoners spend Trait Points on development belonging to that summoner.
4. Cards spend card-bound development points on development belonging to that
   card.
5. Levels provide reliable pacing and a readable measure of development;
   discovery determines what the player can develop.
6. Each level also provides modest automatic base-stat growth so leveling has
   value even when its points are banked or spent on sideways development.
7. Spending and branch commitments are permanent unless a specific authored
   rule explicitly says otherwise.

Summoner level growth applies to both health and maximum mana. Stronger Cards
may require more mana, making increased capacity part of the wider power/cost
curve. Automatic Card growth is configured by Card because creatures, damaging
spells, utility spells, and other Card types do not share one meaningful stat
formula. Exact values remain later balance work.

## 3. Development Opportunity States

Every summoner-trait or card-development opportunity must support the following
states, even though individual content will not necessarily use every state:

1. `Hidden`: absent from the player-facing development interface.
2. `Known Locked`: visible, but its access requirements are not satisfied.
3. `Available`: access is unlocked and its configured acquisition action may be
   taken.
4. `Acquired`: active and permanent.
5. `Closed`: permanently unavailable because of an exclusive choice or another
   authored consequence.

The primary rendered node states are `Known Locked`, `Available`, and
`Acquired`. `Hidden` opportunities render no node. `Closed` remains a persisted
progression state but its node and closed descendants are removed from the
default tree to avoid clutter; an optional `Show Closed Paths` toggle may reveal
them. Hover/focus/selection and a temporary newly-revealed/newly-unlocked
attention treatment are interaction states, not additional progression states.

An opportunity remains `Available` after access is unlocked even when the player
cannot currently afford its configured points or materials. Affordability is
shown in its cost treatment and acquisition action rather than misrepresenting
the opportunity as access-locked.

The default for undiscovered opportunities is `Hidden`. Content may instead use
a visible locked node, an unidentified branch, or another supported presentation
when telegraphing the possibility serves that specific experience.

## 4. Access, Cost, and Acquisition Are Separate

Each development opportunity independently defines:

- how it becomes known;
- how access becomes unlocked;
- how it is acquired;
- its costs;
- its resulting effects and permanently closed alternatives.

Costs are fully configurable. An acquisition may be free once unlocked, cost a
Trait Point or card point, consume one or more materials, require another
resource or sacrifice, or combine several of these.

This separation lets the world control access while points still act as a
constrained development budget. It also permits intentionally authored
exceptions without creating a different progression system for each source.

## 5. World-Action Behavior

Quests, rituals, events, and other world actions use the same configurable
effects. Any of them may reveal an opportunity, unlock access, directly acquire
a trait or upgrade, transform something already owned, or combine several
effects when authored that way.

The preferred content pattern is for quests to unlock access and for rituals to
acquire or transform, but these are guidelines rather than hard source rules. A
quest can directly acquire, and a ritual can reveal or unlock, when the specific
situation warrants it.

## 6. Ritual Behavior

A ritual that is configured to acquire something is the acquisition action, not
merely a gate followed by a duplicate purchase elsewhere. Rituals configured to
reveal or unlock an opportunity do not acquire it unless their authored effects
also say so.

1. The ritual declares its eligibility rules, exact effects, costs, and closed
   alternatives before commitment.
2. A ritual may cost no development point, or it may require a Trait Point or
   card point alongside materials or other configured costs.
3. When a point is required, it is committed through ritual initiation. The
   player does not complete the ritual and then return to another screen to buy
   the result.
4. When acquisition is one of its configured effects, the ritual grants that
   trait, upgrade, or transformation as part of the same committed flow.

Transaction and interruption behavior must ensure that a player cannot lose a
cost without receiving the authored result. The exact presentation and whether
rituals can fail remain implementation-design questions.

## 7. Summoner and Card Differences

Summoner development begins with acquired traits. Innate traits, quests,
quests, exploration, choices, and rituals can add traits or make their
development available. Trait Points constrain how deeply the player can invest
across the traits available to that summoner.

Summoners do not have a separate default Core development tree. Modest automatic
level growth owns universal numerical advancement, while acquired traits own
chosen identity and specialization. A summoner is expected to hold a curated
collection on the order of zero to twenty traits rather than hundreds. Not every
trait must have upgrades.

Card development begins with the card's existing mechanical identity. Card XP
and levels provide card-bound development capacity, while quests, discoveries,
materials, and rituals can make particular behavioral branches available. Each
owned card is a unique instance with its own XP, level, Card Points, acquired
traits, and upgrades; development never mutates every copy of the same catalog
definition.

Every Card has an authored, card-native `Core`. The Core describes how that
creature or spell can naturally develop; it is not assembled from a global pool
of generic stat upgrades. Its paths are visible when the Card is acquired, even
when individual nodes remain locked by level, prerequisites, or cost. A Core
branch should begin with a meaningful change in behavior or strategic identity.
Supporting stat upgrades may appear farther along that branch when they
reinforce the chosen direction, but direct stat investment is not the organizing
idea of Core development.

The Core graph has one inherent, already-owned root representing the Card's base
identity. Every native development path originates from that root. Mutually
exclusive first choices are sibling children of the root, not disconnected root
nodes, so the player can read both their shared origin and the commitment being
made.

Hidden and discovered development belongs primarily to acquired Card traits,
not to the Card's native Core. Quests, rituals, and other world actions may add
those traits or affect access to their paths. Authored exceptions remain
possible, but the normal player-facing distinction is: `Core` is the Card's
known natural possibility space; acquired traits are additional possibilities
the player finds in the world.

## 8. Development Interface Structure

The player never needs to render every possible path as one unbounded graph.

1. A Summoner development surface presents the summoner's trait collection and
   renders the selected trait's compact development path.
   The player enters by selecting an owned trait circle from the Summoner
   overview; there is no separate global `Upgrades` button or global tree route.
   The Traits section receives enough vertical space for multiple wrapped rows
   and scrolls only when the visible grid is full; it does not collapse owned
   traits behind a separate `+N` or `View All` route.
   The selected trait's tree opens as a large overlay over the Summoner screen.
   The initial version does not repeat the trait collection inside that overlay;
   the player closes it and selects another trait from the Summoner screen.
2. A Card development surface presents the card's Core path plus its acquired
   trait collection and renders only the selected path.
   `Core` appears as the first selectable circle beside the acquired trait
   circles; selecting any circle opens its tree in the same large-overlay
   presentation. Core nodes are authored for that Card and are visible from
   acquisition; a broad eligibility tag or global trait catalog cannot add a
   node to Core.
3. Card Points are shared across that card instance's Core and acquired-trait
   paths. Trait Points are shared across that summoner's acquired-trait paths.
   Available Trait Points appear both in the Summoner Traits header and inside
   the open trait-tree overlay.
4. A newly acquired card trait appears as a new selectable trait, not as a
   dynamically positioned island on one enormous card canvas.
5. A path may be a simple linear track, a small permanent fork, or a more
   involved graph. The presentation may remain compact when the authored
   relationships do not warrant a large tree.
   An atomic trait is represented consistently as a valid one-node tree rather
   than receiving a separate detail-only presentation.
6. Cross-path requirements are communicated in opportunity details rather than
   through connector lines spanning multiple paths.
7. Closed paths are hidden by default. A `Show Closed Paths` toggle may reveal
   them for inspection, but permanent alternatives do not occupy ordinary tree
   space after the player commits elsewhere.
8. A reveal or unlock that does not grant power uses a compact toast notification
   rather than interrupting play. Actual acquisition or transformation uses the
   larger generic reward presentation.
9. The tree remains the dominant visual surface. Hovering or focusing a node
   previews its name, effect, requirements, costs, and available action in a
   contextual popover positioned beside that node. Clicking pins the popover;
   clicking elsewhere dismisses it. The popover flips sides when necessary so
   it does not obscure the selected node or important branches.
10. Connector lines and node layout communicate branching and exclusivity. An
    acquisition confirmation may restate the selected result, cost, and
    permanence, but it does not enumerate the alternative path that will close.
11. A ritual-acquired opportunity explains its ritual requirement and known
    location in the tree. It cannot be acquired remotely from the overlay; a
    future Track action may guide the player to the physical ritual.
12. Critical information is never mouse-hover-only. Keyboard and controller
    focus produce the same contextual popover, while touch/click pins it.
13. The overlay header identifies the selected trait and available points; it
    does not display an effect summary. Effects belong to individual nodes so
    the header cannot become inaccurate after multiple developments are acquired.

Not every acquired trait must have upgrades. Traits that do develop use the same
state, access, cost, and acquisition rules as every other opportunity; atomic
traits remain valid permanent identity. Rare free developments and authored
sideways transformations are permitted and do not need to be forced into the
point economy.

## 9. Permanence Under Hidden Information

Spending and branch commitments remain permanent even though future traits and
opportunities may be hidden. A player who spends all available points before
discovering a later trait may be unable to develop it. This is an intentional
consequence of exclusivity, not a condition the system must automatically
refund, respec, or compensate.

## 10. Still To Decide

- The exact Summoner health/mana growth curve and per-Card automatic growth
  values.
- How the temporary effective Card level is projected for capped battles. A cap
  limits level and automatic level-scaled stats; it does not remove acquired
  upgrades or select a subset of the Card's build.
- The standard number and shape of developments belonging to a trait or card.
- How much of an acquired trait's undiscovered possibility space is represented
  without telegraphing its contents.
- The final visual composition of the Summoner overview, trait collection, Card
  Core path, and selected-path view.
- Point award cadence, level caps, and balance budgets for free versus costly
  world-granted development.
