# Trait Tree Screen Flow Spec

**Status:** Draft for implementation  
**Date:** 2026-03-09  
**Owner:** Meta UX / Progression

## 1. Goal

Define the canonical player flow for:

1. `Level Up` (grants trait points)
2. `Traits` (spends trait points in the tree)

This spec is intentionally icon-first and low-text.

## 2. Core Decisions

1. `Level Up` never forces trait selection immediately.
2. `Level Up` grants points only.
3. `Traits` is the only spend surface for progression traits.
4. A spend-available indicator appears on the `Traits` button when unspent points > 0.
5. One-off traits live in a separate tab inside the trait screen.

## 3. Primary Navigation Flow

1. Player sees two persistent actions: `Level Up` and `Traits`.
2. Player presses `Level Up`.
3. System applies level increase and grants trait points.
4. `Traits` button enters alert state (badge).
5. Player can continue playing or press `Traits`.
6. Pressing `Traits` opens `Trait Tree Screen`.
7. Player spends points on available nodes.
8. Badge clears when unspent points reaches 0.

## 4. Button State Rules

## 4.1 Level Up Button

1. `Disabled`: cannot level up yet.
2. `Ready`: can level up now.
3. `Pressed`: executes level-up transaction; on success updates points and badge.

## 4.2 Traits Button

1. `Default`: no badge, normal icon.
2. `Spend Available`: badge shown.
3. `No Unlock Available` (optional): badge can remain but button tint is muted warning if points exist but nothing currently unlockable.

## 4.3 Badge Logic

1. If unspent points == 0: no badge.
2. If unspent points == 1: show `!` badge.
3. If unspent points >= 2: show numeric badge (`2`, `3`, ...).
4. Badge clamps display at `9+`.

## 5. Trait Tree Screen IA

## 5.1 Top Bar

1. `Back`
2. `Trait Points` chip (icon + number)
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
3. Keep body descriptions in right-side detail panel only.
4. Avoid repeated explanatory paragraphs in main tree canvas.

Copy limits:

1. Node title: <= 24 chars target.
2. Short trait summary: <= 60 chars.
3. Locked reason line: one sentence, <= 70 chars.

## 8. Data Contract Hooks

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

## 11. MVP Implementation Slices

1. Add `Traits` button badge state driven by unspent points.
2. Add `Trait Tree Screen` shell with two tabs.
3. Implement `Progression Tree` using static layout first (no auto-layout).
4. Implement node states and unlock interaction.
5. Implement `One-Off Traits` list from `granted_only`.
6. Replace temporary icon placeholders with final art later.

## 12. Acceptance Criteria

1. Leveling up increases unspent trait points and updates `Traits` badge immediately.
2. Trait spending only occurs in the `Traits` screen.
3. Progression tab excludes `granted_only` traits.
4. One-off tab excludes `level_up_offer` traits.
5. Badge clears when unspent points is zero.
6. Player can understand why any locked trait is locked in one interaction.
