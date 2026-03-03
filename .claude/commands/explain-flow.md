# Explain Flow (Sequence View)

Trace a runtime event step-by-step across layer boundaries, showing what happens, where, and why.

## Instructions

1. **Identify the event to trace.** Based on what's being discussed (current task, bug being debugged, feature being built), determine the runtime event to trace. Examples: "player plays a card", "unit takes damage", "projectile hits target", "game phase changes", "unit spawns".

2. **Read the architecture docs** to ground your answer in settled decisions:
   - `docs/architecture/target-architecture.md` — layer definitions, boundary contracts, data flow
   - `docs/architecture/decisions.md` — settled decisions (especially #1 layers, #4 hybrid data model, #9 targeting)
   - `docs/architecture/gameplay/` — per-layer design docs
   - `docs/migration/implementation-checklist.md` — current migration state

3. **Output a Mermaid sequence diagram** showing:
   - Participants labeled with their **layer** (Input, Session, Simulation, View)
   - Each message showing what data crosses the boundary
   - Boundary crossings explicitly marked with the interface used (e.g., `IGameSession.SubmitCommand()`)
   - Return data where applicable

4. **Write the diagram to a temp file and open it.** Use Bash to:
   - Write the Mermaid diagram (wrapped in a markdown fenced code block) to a temp file: `/tmp/explain-flow-<topic>.md` (where `<topic>` is a short slug for the event, e.g. `card-play`)
   - Open it with: `open /tmp/explain-flow-<topic>.md`

5. **Below the diagram, provide a numbered step-by-step walkthrough in the conversation:**

   For each step:
   ```
   **Step N** [LAYER] ComponentName
   What happens and why it happens in this layer.
   Data in: what this step receives
   Data out: what this step produces
   ```

   At each boundary crossing, call it out explicitly:
   ```
   --- BOUNDARY: Layer A → Layer B via InterfaceName ---
   Data crossing: what passes through and in what form
   ```

5. **Distinguish current vs target.** If the flow is different today than in the target architecture:
   - Show the **target** flow as the primary diagram (what it should be)
   - Add a **"Current State"** section noting where the actual implementation diverges
   - Note which migration milestone addresses the gap

## Notes

- Prefer accuracy over completeness — if a step is uncertain, say so.
- Use the architecture docs as the source of truth for the target flow.
- For current-state divergences, reference actual code paths.
- If the conversation doesn't have an obvious event to trace, ask the user what runtime scenario they want to understand.
- Keep diagrams focused — trace one event end-to-end rather than branching into every possible side effect.
- For events that differ between singleplayer and multiplayer (e.g., command validation), show the singleplayer (LocalSession) path as primary and note multiplayer differences.
