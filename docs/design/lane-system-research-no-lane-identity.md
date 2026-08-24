# Spatial Structure Research (No Hard Lanes)

**Status:** Working draft (problem-first rewrite)  
**Last Updated:** 2026-03-09  
**Intent:** Explain what is failing in the current open arena, analyze how similar games solve those failures, and define better solution directions for Fateforged without turning into a lane pusher.

---

## 1. Problem Statement (Rewritten)

The current battlefield is an open rectangle with side-based spawn limits. In practice, many matches collapse into a center-heavy blob fight.

This creates two core failures:

1. **Aggro Shadow:** flank paths exist physically, but flank attempts pull aggro and get absorbed into the same central fight.
2. **Spatial Collapse:** most tactical value concentrates around one midline engagement, so large map areas become low-value space.

This is not "we need lanes."  
This is a **spatial economy problem**: the game does not consistently reward using space away from the center.

---

## 2. Open Arena Failure Modes (Current Game)

This section is intentionally concrete and tied to current runtime behavior.

### 2.1 Aggro Shadow (user-reported)

**What players feel**
- Trying to route around the side still drags units into nearby enemy aggro, so flanks collapse into front clashes.

**Why it happens in current logic**
- Units retarget based on enemy scans inside aggro radius and score by distance/health, not by route or role discipline.
- Out-of-range behavior then chases that selected target.

**Code evidence**
- `SimTargeting` filters/score-based acquire: `scripts/csharp/Battle/Simulation/Combat/Targeting/SimTargeting.cs`
- Chase fallback in behavior tick: `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`

### 2.2 Midline Vortex (user-reported)

**What players feel**
- Most matches become one main collision around center progression.

**Why it happens in current logic**
- No-target default movement is forward along X.
- Spawn-side rules constrain X territory only.
- AI spawn selection already biases a central Z band.

**Code evidence**
- Forward intent along X axis: `scripts/csharp/Battle/Simulation/Movement/DirectIntentGenerator.cs`
- Spawn-side boundary only: `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`
- AI lane-like Z bias: `scripts/csharp/Battle/Simulation/AI/HeuristicAiStrategy.cs`

### 2.3 Edge Dead Space

**What players feel**
- Map edges are available but rarely meaningful except as temporary movement detours.

**Why it happens in current rules**
- No edge-specific incentives, objectives, or route rewards.
- Same target acquisition logic applies everywhere, so center proximity usually dominates.

**Related design evidence**
- Battlefield spec is intentionally flat/open with no terrain objectives in MVP: `docs/features/battlefield/system.md`

### 2.4 Reinforcement Convergence Loop

**What players feel**
- New units tend to be sucked into the existing main fight instead of creating new pressure lines.

**Why it happens**
- Behavior pipeline continuously loops: targeting -> chase/attack -> movement.
- Broad enemy availability plus nearest-biased scoring tends to reinforce the current dominant engagement.

**Code evidence**
- Per-tick unit processing sequence: `scripts/csharp/Battle/Simulation/Simulation.cs`
- Target score and acquisition loop: `scripts/csharp/Battle/Simulation/Combat/Targeting/SimTargeting.cs`

### 2.5 Formation Half-Life

**What players feel**
- Preparation phase formation matters early, then quickly dissolves once battle starts.

**Why it happens**
- Once active, units are driven by local target/chase logic; there is no persistent formation/command constraint layer.

**Design tension**
- Vision emphasizes formation fantasy, but runtime incentives pull toward local opportunistic fights.

### 2.6 Role Flattening

**What players feel**
- Distinct roles can blur in dense fights because everyone collapses around similar engagement surfaces.

**Why it happens**
- Global target availability + shared chase mechanics reduce positional differentiation unless explicit role rules exist.

### 2.7 Decision Compression

**What players feel**
- Many spawn choices feel equivalent, so practical decision space shrinks.

**Why it happens**
- If center collision dominates outcomes, off-center spawn plans are lower-confidence and often lower-value.

### 2.8 Density-Driven Stability/Perf Risk

**What players feel**
- Congested fights are where movement and readability failures spike.

**Why it matters**
- You already have tracked evidence of congestion-related instability and ongoing hot-path optimization work.

**Evidence**
- Blocked-idle congestion bug resolution: `docs/tracking/bugs-resolved.md`
- 40-100 unit hot-path item still active: `docs/tracking/todos.md`

---

## 3. What Similar Games Are Actually Solving

This section uses tighter comparators only.

### 3.1 Mini Warriors Reborn (closest style reference)

**Primary problem solved**
- Open-battle chaos is constrained through role/row behavior logic.

**How it solves**
- Unit classes use structured targeting/interaction rules (row/force-aware behavior), not purely free nearest-chase flow.

**What it buys**
- Better frontline readability.
- Clearer role identity.
- Less tactical entropy in large clashes.

**Tradeoff**
- Less freeform emergent movement compared to unconstrained open arenas.

**Lesson for Fateforged**
- You can shape combat with **behavioral structure** (targeting rules) without hard geometry lanes.

### 3.2 Clash Royale (control benchmark, not identity target)

**Primary problem solved**
- Readability, pacing, and spawn safety through strict map topology.

**How it solves**
- Hard channels and crossing points create deterministic confrontation surfaces.

**What it buys**
- Extremely legible pressure lines.
- Strong macro predictability.

**Tradeoff**
- Lower expressive space; can feel rail-constrained.

**Lesson for Fateforged**
- Hard lanes solve many problems fast, but conflict with your "battlefield, not lane-pusher" identity.

### 3.3 Minion Masters (bridge-control iteration case)

**Primary problem solved**
- Creates structured conflict around controllable center resources.

**How it solves**
- Objective control systems shape movement/value concentration.
- Patch history shows active anti-degenerate rule tuning (for backcapping and bridge abuse).

**What it buys**
- High decision clarity around key points.
- Better macro pacing.

**Tradeoff**
- Objectives can dominate deck/gameplay diversity if rule edges are exploitable.

**Lesson for Fateforged**
- If you add structured objectives, anti-cheese constraints must ship with them.

### 3.4 Battle Legion (formation-first contrast)

**Primary problem solved**
- Emphasizes lineup composition and role arrangement over live lane navigation.

**How it solves**
- Strategic depth is pushed into formation and matchup planning, reducing reliance on mid-match path topology.

**Lesson for Fateforged**
- Strong pre-battle structure can reduce lane dependence, but only if runtime preserves that structure long enough to matter.

---

## 4. Design Requirements Derived from the Failures

Any proposed solution should satisfy all of these:

1. Preserve no-hard-lane identity.
2. Break the center-only gravity pattern.
3. Make flanking behaviorally viable (not just geometrically possible).
4. Keep formation decisions valuable beyond first contact.
5. Improve readability at 40-100 units.
6. Avoid introducing dominant degenerate exploits.
7. Stay compatible with deterministic sim/session architecture.

---

## 5. Expanded Solution Brainstorm (More Imaginative)

This is intentionally broader than the earlier conservative set.

### A. Aggro Regimes (contextual aggro rules)

- Replace one-size aggro with regime-based aggro:
  - `Frontline regime`: normal aggro.
  - `Flank transit regime`: delayed/intermittent aggro acquisition unless threatened.
  - `Commit regime`: full aggro once attack/cast starts.
- Goal: make flanking behaviorally real.

### B. Command Cohesion Layer

- Add lightweight "order memory" after prep (hold line, push line, screen).
- Units keep partial adherence to formation intent unless broken by strong tactical triggers.
- Goal: increase formation half-life.

### C. Spatial Value Injection (edge value)

- Add low-count side objectives or pressure zones that create off-center value.
- Reward should be tactical (tempo/vision/formation leverage), not raw snowball economy.
- Goal: prevent edge dead space.

### D. Engagement Cells (soft battlefield partition)

- Partition field into dynamic influence cells (not hard lanes).
- Target selection gets a locality weight favoring current/adjacent cells before global fallbacks.
- Goal: reduce global target churn and improve local clarity.

### E. Frontline Tension Bands

- Derive live clash bands from unit density/velocity conflict.
- Use bands for UI readability and mild behavior weighting only.
- Goal: visible "battle line" without rails.

### F. Role-Specific Pursuit Limits

- Add pursuit depth budgets by role (frontliners stick longer, skirmishers peel sooner, flankers avoid deep aggro traps).
- Goal: preserve role identity and avoid universal center collapse.

### G. Reinforcement Routing Rules

- New spawns route to assigned pressure sectors unless explicitly redirected.
- Stops instant automatic convergence into one blob.

---

## 6. Candidate Direction Bundles (For Discussion)

These bundles combine ideas above into coherent experiments.

### Bundle 1: Behavioral Structure First

- Aggro Regimes + Role Pursuit Limits + Command Cohesion.
- Risk: tuning complexity.
- Upside: biggest identity fit (battlefield feel preserved).

### Bundle 2: Spatial Incentive First

- Spatial Value Injection + Reinforcement Routing.
- Risk: objective-centric gameplay drift.
- Upside: fastest way to activate unused map space.

### Bundle 3: Readability First

- Frontline Tension Bands + Engagement Cells (soft weighting).
- Risk: may improve clarity more than strategic depth if done alone.
- Upside: strong immediate spectator/player comprehension gains.

### Bundle 4: Hybrid (recommended for first prototype pass)

- Aggro Regimes + Frontline Tension Bands + one low-impact side objective.
- Why: directly addresses your two reported failures while avoiding full-system rewrite.

---

## 7. Option Inventory (Do Not Forget)

This list is a memory aid of plausible options discussed so far.

### 7.1 Behavioral options

- Virtual lanes (3 logical lanes, no physical walls)
- Lane stickiness (prefer assigned lane unless trigger to switch)
- Cross-lane aggro penalty / gating
- Flanker profile (reduced center pull while side-routing)
- Role pursuit limits (frontliner/backliner/flanker chase depth differences)
- Reinforcement lane assignment (left/center/right routing on spawn)
- Lane handoff rules (explicit reasons to change lanes)
- Command cohesion (formation/order memory after prep)

### 7.2 Readability options

- Frontline band/tension overlay
- Engagement cells (soft partition for locality-aware targeting)
- Debug overlays for lane usage + frontline spread

### 7.3 Spatial/map options

- Side objectives / pressure points (lightweight, non-dominant)
- Soft separators (terrain friction between lanes, still crossable)
- Hard lanes/chokepoints (kept as fallback option, not preferred identity path)

### 7.4 Currently selected implementation experiments

- Virtual lanes
- Tactical roles

---

## 8. Prototype-First Measurement Plan

Before choosing any bundle permanently, measure these:

1. **Flank survival window:** average time flank units remain off-mainline before hard engagement.
2. **Spatial utilization index:** share of combat events by map bands (center vs edges).
3. **Frontline spread:** variance of active engagement Z positions.
4. **Formation persistence:** time until initial formation structure drops below threshold.
5. **Target churn rate:** target switches per unit-minute in dense fights.
6. **Congestion stability:** blocked/overlap correction spikes per minute.
7. **Perf budget impact:** simulation tick cost in 40/80/100-unit test scenarios.

---

## 9. Plain-Language Summary

The issue is not "there are no lanes."  
The issue is that the current rules make center fights too dominant and flanks too fragile.

So the answer is not necessarily hard lanes.  
The answer is adding **behavioral and spatial structure** so the whole map becomes tactically meaningful.
