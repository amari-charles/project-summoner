# Trait Tree Screen Flow Spec

**Status:** Partially superseded; interface topology under reconsideration
**Date:** 2026-03-09  
**Owner:** Meta UX / Progression

> The default-tree and separate one-off-tab assumptions in this document are no
> longer current direction. See
> [Discovery-Driven Development](discovery-driven-development.md). Automatic
> levels, banked points, the spend-available indicator, and the compact owned
> trait summary remain applicable while the replacement interface is designed.
> The former global `Upgrades` button is also superseded: owned trait circles are
> now the primary entry into the selected trait's development view.

## 1. Goal

Define the canonical player flow for automatic summoner levels and banked,
player-chosen upgrades.

This spec is intentionally icon-first and low-text.

## 2. Core Decisions

1. Summoner levels apply automatically when XP crosses a threshold.
2. Each gained level grants an upgrade point without forcing an immediate choice.
3. The selected-trait development interface spends points for opportunities whose
   configured acquisition method is direct point spending.
4. Spend availability is indicated in the Traits area without a separate global
   `Upgrades` button.
5. Traits are not divided into a mandatory `Progression Tree` and `One-Off`
   interface. The replacement organization remains to be designed.
6. The Summoner screen shows owned traits as clickable build-summary icons; it
   does not reproduce every trait's full development path.

## 3. Primary Navigation Flow

1. Player earns enough XP to cross one or more level thresholds.
2. The system applies every affordable level, carries remaining XP forward, and
   banks one upgrade point per level.
3. The Summoner Traits area enters its spend-available state.
4. Player can continue playing or select an owned trait from the Summoner screen.
5. Selecting an owned trait circle opens that trait's development surface.
6. Player selects an available opportunity and completes its configured
   acquisition action.
7. Badge clears when unspent points reaches 0.

## 4. Traits-Area State Rules

## 4.1 Spend-Availability Indicator

1. `Default`: no badge, normal trait presentation.
2. `Spend Available`: point badge shown in the Traits area.
3. `No Unlock Available` (optional): the badge may remain with a muted warning
   treatment if points exist but nothing currently unlockable is known.

## 4.2 Badge Logic

1. If unspent points == 0: no badge.
2. If unspent points == 1: show `!` badge.
3. If unspent points >= 2: show numeric badge (`2`, `3`, ...).
4. Badge clamps display at `9+`.

## 5. Historical Trait Tree IA

This IA is retained as reference for the earlier prototype. It is not a current
requirement. A replacement must support the shared opportunity states and
configurable acquisition methods before its final topology is chosen.

## 5.1 Top Bar

1. `Back`
2. `Upgrade Points` chip (icon + number)
3. Tabs:
4. `Progression Tree`
5. `One-Off Traits`

## 5.2 Progression Tree Tab

1. Center canvas with nodes and connectors.
2. Right-side detail panel for selected node.
3. Unlock action appears in detail panel, not inline in node.

## 5.3 One-Off Traits Tab

1. Card list grouped by source (`Story`, `Event`, `Challenge`, `Other`).
2. Mostly informational (acquired/not acquired).
3. If a one-off is claimable, CTA shows in card (`Claim`), otherwise read-only.

## 6. Node Visual State Model

Each trait node is exactly one state:

1. `Owned`: solid fill, check icon.
2. `Available`: highlighted border, pulse/glow.
3. `Locked`: desaturated with small lock icon.
4. `Preview` (hover/focus): temporary emphasis and connector highlight to parents/children.

Connector rules:

1. Owned->Owned path: bright line.
2. Any path touching locked node: muted line.
3. On node focus: immediate parents/children thicken.

## 7. Icon-First / Low-Text Rules

1. Use icons for category and state before words.
2. Keep always-visible node text to name only.
3. Keep body descriptions in contextual node popovers rather than permanently
   occupying tree space.
4. Avoid repeated explanatory paragraphs in main tree canvas.

Copy limits:

1. Node title: <= 24 chars target.
2. Short trait summary: <= 60 chars.
3. Locked reason line: one sentence, <= 70 chars.

## 7.1 Current Node Inspection Pattern

1. The selected path owns the overlay; no permanent side inspector compresses
   the tree canvas.
2. Mouse hover and keyboard/controller focus preview a contextual popover beside
   the node.
3. Clicking or tapping pins the popover so its action can be used.
4. Clicking elsewhere dismisses it.
5. The popover contains the node name, effect, rank when relevant, cost,
   unmet requirement, and contextual action.
6. It opens on whichever side has room and must not cover the selected node or
   the branch relationship the player is evaluating.
7. The overlay header contains trait identity and available points only. It does
   not attempt to summarize the combined effects of acquired nodes.

## 8. Legacy Data Contract Hooks

The existing acquisition modes below describe the current catalog bridge, not
the final player-facing organization. They must not force future content into a
default-tree versus one-off-tab split.

Use existing trait metadata:

1. `prerequisites` drives connector graph and availability.
2. `acquisition_mode` drives tab routing:
3. `level_up_offer` => Progression Tree
4. `granted_only` => One-Off Traits

Current backend hooks to use:

1. Trait catalog dictionary includes `acquisition_mode`.
2. Catalog bridge can filter by acquisition mode.
3. Summoner progression provides all owned trait ids and unspent points.

## 9. Availability Evaluation

A `level_up_offer` trait is `Available` when all are true:

1. Not already owned.
2. All prerequisites owned.
3. Summoner is eligible by tags/requirements.
4. Summoner meets level bounds.

Otherwise it is `Locked`.

## 10. Edge Cases

1. Unspent points but no available traits:
2. Show a clear non-blocking callout in tree header: `No unlocks at current level`.
3. Owned trait removed/retired by content update:
4. Render as legacy owned node (read-only) with archive marker.
5. Very large trees:
6. Enable zoom + pan; preserve last camera position per summoner.

## 11. Revised MVP Implementation Slices

1. Preserve the Upgrades badge state driven by unspent points.
2. Preserve the compact owned-trait summary on the Summoner screen.
3. Define the shared hidden, known-locked, available, acquired, and closed states
   in data before committing to a replacement tree or collection layout.
4. Support configurable access, costs, and acquisition actions without hard
   source rules for quests, rituals, or other world actions.
5. Prototype the replacement development surface with representative summoner
   and card examples before scaling content.
6. Replace temporary icon placeholders with final art later.

## 12. Acceptance Criteria

1. An XP grant can apply multiple levels and banks one upgrade point for each.
2. No player-facing manual level-up action remains.
3. Direct point spending occurs in the development surface, while a ritual that
   costs a point commits it inside the ritual flow.
4. Hidden opportunities do not appear unless their content configuration says
   otherwise.
5. Opportunity costs can be free or combine points, materials, and other
   authored requirements.
6. Badge clears when unspent points is zero.
7. Player can understand why any visible locked trait is locked in one
   interaction.
