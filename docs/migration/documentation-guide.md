# Architecture Documentation Guide

Conventions for writing architecture docs and diagrams in this project. Living document — more principles will be added over time.

## Principles

### 1. Dependency = who knows about whom, not who gives data to whom

A depends on B if A holds a reference to B or calls B's methods. Data flow is a separate concept. Data can flow A→B while dependency also points A→B (Input sends Commands to Session). Or data can flow B→A while dependency still points A→B (View reads from Session — View depends on Session, not the other way around).

When documenting a relationship, ask: "does this component need to know the other exists?" If yes, that's a dependency. If it just receives data passively, it's not.

### 2. Data flow and dependency flow are independent

Always be explicit about which one an arrow represents. They often point in opposite directions. Use arrow style (solid vs dashed), labels, or both to distinguish them.

Bad: a single unlabeled arrow between two components — the reader can't tell if it means "calls," "reads from," or "sends data to."

Good: solid arrow labeled "sends Command" for dependency + data flow in the same direction; dashed arrow labeled "reads" for data flow opposite to dependency direction.

### 3. Start at the highest level of abstraction, then drill down

The first diagram a reader sees should be dead simple — just layer/component names and arrows. No internal details, no subcomponents, no method signatures. If you can't fit the overview on a napkin, it's too detailed.

Detailed breakdowns belong in subsequent diagrams or separate documents. Each level of detail gets its own diagram, linked from the level above.

### 4. Plain English first, diagram second

Every architectural concept gets a written explanation before any diagram. The reader should understand the idea from the words alone — the diagram reinforces, not replaces.

If a concept only makes sense by staring at the diagram, the prose needs work.

### 5. Peers don't reference each other

If two components are independent (like Input and View), their sections should not mention each other. Independence means you can understand one without reading about the other.

Exception: a brief note like "Input knows nothing about View" at the end of a section is fine — it sets expectations. But the section's content should stand alone.

### 6. Document "today" vs "target"

For each target component, note what exists today, where it's scattered, and what changes. This grounds the architecture in reality rather than being purely aspirational.

Pattern:
- State what the target component does
- Note what currently implements that behavior (and where)
- Describe what changes to get from here to there

### 7. Arrow legends are mandatory

Every diagram must state what solid arrows mean vs dashed arrows. Don't assume the reader knows. Even if it feels redundant, add a legend — either as a note below the diagram or built into the diagram itself.

Example: "**Solid arrows** = calls/sends. **Dashed arrows** = reads/reacts (no mutation)."

### 8. Diagrams must reflect the actual relationship

If a component is a hub that everything flows through, show it as a hub — centered, with connections radiating out. Don't hide it in a misleading stack or linear chain.

The visual layout should match the conceptual role. A central orchestrator belongs in the center. Independent peers belong side by side. A hierarchy belongs in a tree. Fight the temptation to make every diagram a top-to-bottom waterfall.

### 9. Detail docs for each component

Each major node in the high-level diagram gets its own document with implementation specifics. The overview doc links to these but stays high-level itself.

**Location:** `docs/architecture/gameplay/<layer>/<component-name>.md`

Components live under their layer's subtree. For example, View layer components live in `docs/architecture/gameplay/view/`. Each layer has a `README.md` index that links to its component docs.

**Contents of a component doc:**
- What it is (class, interface, abstract base)
- What methods/API it exposes
- What it owns (sub-components, data structures)
- What depends on it (upstream consumers)
- What it depends on (downstream dependencies)
- "Today" snapshot — what currently implements this behavior

This keeps the overview doc readable. Readers who want depth follow the link; readers who want the big picture stay on the overview page.

### 10. Lifecycle ownership determines layer placement

A component belongs in the layer that manages its lifecycle (creation/destruction). View component lifecycles are sim-driven — EntityManager diffs MatchState and spawns/destroys shells when entities appear/disappear. If a component's lifecycle is gesture-driven (created on drag start, destroyed on drag end) and MatchState has no concept of "in progress," that component belongs in Input, not View — because InputCollector must own its lifecycle, and Input cannot depend on View (they are peers).

This principle explains why SummonPreview, SpellPreview, SpawnZoneOverlay, and RedirectIndicator live in Input despite being visual code.
