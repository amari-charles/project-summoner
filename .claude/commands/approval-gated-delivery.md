# Approval-Gated Delivery Command

Run medium/large initiatives using explicit approval-gated passes.

## Canonical Source

This command must follow `docs/workflows/approval-gated-delivery.md` as the source of truth. If this file and the workflow doc diverge, update this command to match the workflow doc.

## Instructions

1. Confirm applicability:
   - Use for medium/large feature or refactor work.
   - Small local fixes may skip this command.

2. Execute `PASS 1: USE CASES + VALIDATION`:
   - Create/update:
     - `docs/technical/<domain>/<initiative>-plan.md`
     - `docs/technical/<domain>/<initiative>-validation-cases.md`
   - Use templates from `docs/workflows/templates/`:
     - `initiative-plan-template.md`
     - `initiative-validation-cases-template.md`
   - End the output with an explicit request for Pass 2 approval.
   - If explicit approval is not provided, output exactly: `blocked waiting approval` and stop.

3. Execute `PASS 2: STUBS + WIRING` (only after explicit approval):
   - Create/update `docs/technical/<domain>/<initiative>-stub-checklist.md`.
   - Use template `docs/workflows/templates/initiative-stub-checklist-template.md`.
   - Add compile-safe deterministic stubs and wiring.
   - Remove/disable conflicting legacy paths in scope.
   - Add test skeletons mapped to validation case IDs.
   - End the output with an explicit request for Pass 3 approval.
   - If explicit approval is not provided, output exactly: `blocked waiting approval` and stop.

4. Execute `PASS 3: IMPLEMENTATION + TESTS` (only after explicit approval):
   - Implement full behavior and complete tests.
   - Update validation case status values (`Implemented` or `Deferred` with rationale).
   - Summarize test outcomes and unresolved items.

5. Execute `PR REVIEW: READY`:
   - Run review flow using `docs/workflows/pr-review-guidelines.md`.
   - Include pass-gate compliance validation in review findings.

## Hard Gate Rules

1. Only explicit approval text advances from Pass 1 to Pass 2 and from Pass 2 to Pass 3.
2. Implied approval does not advance phases.
3. Keep phase labels visible in every pass output.
4. Do not compress multiple phases into one output for medium/large initiatives.

## Output Contract

Each pass output must include:
1. Phase label.
2. Acceptance criteria/checklist status.
3. Required artifacts touched.
4. If waiting at a gate: exact text `blocked waiting approval`.
