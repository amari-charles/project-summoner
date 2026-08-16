# Quest System Foundation Plan

**Status:** Active
**Product source:** `docs/design/quest-system.md`

## Objective

Replace catalog-first Academy enrollment with one professor-led introductory
quest slice while preserving existing battle, reward, persistence, and
curriculum behavior behind explicit quest boundaries.

## Current Starting Point

- `AcademyProgressHandler` owns course enrollment, activities, completion,
  transcript state, and curriculum capacity.
- `AcademyCourseDefinition` owns the authored course/activity graph.
- The walkable campus has buildings and shortcut navigation but no authoritative
  professor NPC component.
- The current Course Flow launches activities from a node graph.
- A first `GetQuestJournalState` projection and stacked-card Journal graybox
  exist on `feature/professor-led-quest-model`; their data boundary is reusable,
  but the accepted Journal layout is now a three-region category/list/detail
  composition.

## Implementation Boundary

### 1. Domain and authored data

- Add typed professor definitions and stable professor identifiers.
- Add course-steward ownership, named campus landmark, and opportunity visibility
  metadata.
- Represent announced and hidden visibility explicitly even though only
  announced content is exercised initially.
- Stop auto-enrolling the required introduction.
- Enforce the accepted dependency sequence in authored data and validation.
- Preserve permanent curriculum commitment through the existing enrollment
  authority.

### 2. Quest projection and state

- Extend the Journal projection with professor, landmark, marker, tracked, and
  current-objective data.
- Persist the tracked quest identifier.
- Expose professor interaction state and one acceptance command through the
  authoritative Campaign service.
- Do not duplicate course completion or reward state in GDScript.

### 3. Walkable campus presentation

- Create one reusable professor/quest-giver component.
- Place five placeholder instances on the existing campus.
- Render `!`, `?`, or no marker from authoritative quest state.
- Add a reusable dialogue interaction for overview, Accept/Not Yet, active state,
  and natural turn-in.
- Keep authored character dialogue separate from activity labels and Journal
  objectives. Use the general professor's initial supportive-mentor voice for
  the introductory quest.
- Add the one-line tracked quest banner below the profile icon.
- Keep the Journal in persistent right-side navigation.

### 4. Journal presentation

- Replace stacked cards with the accepted category rail, selected-category
  quest list, and quest-detail layout.
- Keep Active, Open, and Completed categories.
- Select the tracked or first relevant quest on entry.
- Clicking the HUD banner opens the tracked quest details.
- Show quest source identity/location and known reward previews in details.

### 5. Introductory battle loop

- Route the accepted introductory assignment into one existing training battle
  as graybox content.
- Return to the walkable campus with the battle outcome applied.
- Make the professor display `?` after the successful objective.
- Complete the introduction during closing dialogue and unlock the foundation
  fork.

## Explicit Non-Goals

- Finished professor characters or complete dialogue writing beyond the initial
  supportive-mentor proof.
- Hidden quest discovery gameplay.
- Magical trails or a minimap.
- Final campus landmark art.
- Migration of every existing course and lesson in this slice.
- A second battle system or summoner movement.

## Verification

- Unit coverage for visibility, dependency, permanent commitment, tracking, and
  Journal categorization.
- Scene coverage for five professor instances, marker states, Journal layout,
  tracker placement, and navigation.
- End-to-end test for offer → accept → battle objective → return → dialogue
  completion → foundation unlock.
- Full C# and GDScript suites remain green.
