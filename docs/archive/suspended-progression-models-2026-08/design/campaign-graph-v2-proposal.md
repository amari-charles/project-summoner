# Campaign Graph V4 (Implemented Scaffold)

> **Status: RETIRED.** Fateforged no longer has a campaign map or campaign
> progression model. Professor-led quests and reusable encounters are current.
> This historical scaffold will move to `docs/archive/`.

**Date:** 2026-03-11
**Status:** Implemented (single-act, expanded)
**Scope:** `summoners_path` Act I graph only

## Current Scale

- Total nodes: 34
- Combat nodes: 28
- Choice nodes: 3
- Caravan nodes: 3

## Branching Model

- Opening choice: 3 options (`aggressive`, `prepared`, `insight`)
- Mid-route choice: 3 options (`ridge`, `river`, `grove`)
- Major path choice: 3 options (`elite`, `standard`, `gambit`)

## Arc Shape

1. Intro spine (first_trial -> second_challenge)
2. Opening doctrine fan-out (3-way)
3. Midline reconverge
4. Route fan-out (3-way)
5. Caravan + chokepoint + gatekeeper
6. Major path split (elite/standard/gambit)
7. Deep branch chains
8. Rejoin trial + final ante + storm breaker + act boss

## Where To Tune It

- Event IDs + types: `scripts/csharp/Infrastructure/Data/Events/EventId.cs`
- Choice IDs: `scripts/csharp/Meta/Services/Campaign/ChoiceId.cs`
- Node definitions + coordinates: `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs`
- Edge wiring: `scripts/csharp/Infrastructure/Data/Events/CampaignCatalog.cs`
- Node text: `localization/data/en.json`
- Map canvas size: `scenes/meta/screens/campaign_map.tscn`
