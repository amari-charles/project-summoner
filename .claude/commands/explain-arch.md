# Explain Architecture (Tree View)

Show the composition and ancestry of the architecture node most relevant to the current conversation.

## Instructions

1. **Identify the node.** Based on what's being discussed (current task, file being edited, system being debugged), determine which node in the architecture tree is most relevant. A "node" is any named component, layer, subsystem, or concept in the architecture (e.g., `EntityManager`, `Simulation`, `UnitVisual`, `CommandRouter`).

2. **Read the architecture docs** to ground your answer in settled decisions:
   - `docs/architecture/target-architecture.md` — layer definitions, component descriptions, invariants
   - `docs/architecture/decisions.md` — settled decisions and rationale
   - `docs/architecture/gameplay/` — per-layer design docs (view, input, session)
   - `docs/migration/implementation-checklist.md` — current migration state (what exists vs what's planned)

3. **Output a Mermaid diagram** showing:
   - The **node itself** in the center
   - Its **children/parts** below it — what it contains
   - Its **parent chain** above it — what contains it, up to the root layer
   - Its **siblings** at the same level — peer nodes it relates to

4. **Write the diagram to a temp file and open it.** Use Bash to:
   - Write the Mermaid diagram (wrapped in a markdown fenced code block) to a temp file: `/tmp/explain-arch-<topic>.md` (where `<topic>` is a short slug for the node, e.g. `card-data-building`)
   - Open it with: `open /tmp/explain-arch-<topic>.md`

5. **Below the diagram, provide prose sections in the conversation:**

   ### Contains (Children)
   For each child of this node:
   - What it is and what it does (one line)
   - Why it belongs here (not somewhere else)

   ### Why Grouped
   What organizing principle holds these children together. What would break if you split them apart.

   ### What It Does
   The combined effect of this node's parts — what it accomplishes as a whole. One paragraph.

   ### Belongs To (Ancestry)
   Walk up the parent chain to the root layer. For each ancestor:
   - Name and one-line description
   - What role this node plays in its parent

   ### Siblings
   Other nodes at the same level. For each:
   - Name and one-line description
   - How it relates to the current node (depends on, independent peer, shares data with, etc.)

5. **Mark migration status.** For each node mentioned, note whether it:
   - Exists and is implemented
   - Exists as a stub (placeholder for future implementation)
   - Is planned but not yet created
   - Is being replaced (old name → new name)

## Notes

- Prefer accuracy over completeness — if you're unsure about a relationship, say so rather than guessing.
- Use the architecture docs as the source of truth, not inferences from code alone.
- If the conversation doesn't have an obvious focal node, ask the user what component or system they want to understand.
- Keep the Mermaid diagram readable — don't include the entire architecture, just the relevant subtree and immediate context.
