## Project Rules

### Design Docs Are Source Of Truth

Product/design docs in `docs/design/`, lore docs, and user-authored implementation specs capture product intent. Do not edit those docs to make them match current code when code has drifted.

- If implementation conflicts with product docs, treat the code as suspect first.
- Fix implementation when the doc intent is clear and the change is safely scoped.
- If the doc intent is ambiguous, outdated, or too large to implement safely, report the mismatch and ask for direction.
- Only change product/design intent docs when the user explicitly asks to revise the design decision itself.
- Technical validation docs may be corrected for factual metadata, such as test paths or command names, but must not rewrite product intent.

### Record Meaningful Product Direction Changes

`docs/project/direction-log.md` records the history of medium- and large-scale product direction decisions. It complements the design docs; it does not replace them.

- Add or update an entry when the user explicitly approves a decision that changes the player experience, feature ownership, progression structure, a cross-feature constraint, or the existence of a major flow.
- Do not add entries for routine implementation choices, isolated bug fixes, small visual adjustments, refactors that preserve behavior, or unapproved prototypes and ideas.
- Do not infer product intent from code changes alone. If the product decision was not explicitly approved, ask before recording it as accepted direction.
- Update the direction log in the same work when an approved decision is introduced, revised, superseded, or retired, provided the documentation change is safely in scope.
- Preserve history: add a new entry that references the earlier decision instead of rewriting an old entry to make it appear that the direction never changed.
- Product/design docs remain the source of truth for the current intended behavior. Direction-log entries explain when and why that intent changed.

### Preserve Artwork Dimensions In UI Layouts

Character art, card art, portraits, icons, and other authored imagery must not be
silently stretched or resized to whatever space a parent `Container` happens to
allocate.

- Give artwork an explicit display size or aspect-ratio wrapper appropriate to
  the surface. `custom_minimum_size` alone is not a fixed-size guarantee.
- Set container size flags so the artwork slot shrinks or centers instead of
  expanding with sibling content.
- Use an aspect-preserving texture mode (`keep aspect centered` or `keep aspect
  covered`, according to the design) when the real asset is present.
- If art is not available yet, use a fixed-dimension placeholder representing
  the eventual art slot. Do not make the placeholder responsive in a way the
  final asset should not be.
- Do not add decorative panels, borders, or nested boxes merely to group nearby
  information. A visible container needs a clear interaction or hierarchy job.

### Size UI In Design Space, Not As Viewport Percentages

The project uses a 1920x1080 logical design resolution and lets Godot scale that
design to the player's display. Do not size authored UI surfaces by multiplying
or anchoring against a percentage of the runtime viewport.

- Give modals, cards, artwork slots, icons, buttons, and inventory cells explicit
  design-space dimensions. Center or place them with `Container` nodes and anchors.
- Use fractional anchors for placement, fullscreen coverage, and proportional
  layout inside an already fixed-size component, not to determine the component's
  display dimensions.
- Full-screen screens may fill the viewport. Use fixed design-space margins when
  their content needs an inset instead of percentage-based outer bounds.
- HUD elements may anchor to viewport edges, and drawers may read viewport bounds
  when needed to enter or leave the visible screen.
- Functional canvases such as maps, trait graphs, cameras, and scroll regions may
  use their available viewport or container size for centering, clipping, or
  navigation. This exception does not permit viewport-sized authored artwork.
- Prefer a stable configured row/column count for fixed inventory grids. Use
  scrolling for overflow rather than changing the composition by resolution.

## Skills
A skill is a set of local instructions to follow that is stored in a `SKILL.md` file. Below is the list of skills that can be used. Each entry includes a name, description, and file path so you can open the source for full instructions when using a specific skill.

### Available skills
- approval-gated-delivery: Run medium/large feature and refactor work using approval-gated passes: use cases+validation, stubs+wiring, implementation+tests, then PR review. Use when the user asks for pass-based planning/execution, approval gates, or use-cases then stubs then implementation. (file: /Users/amaricharles/.codex/skills/approval-gated-delivery/SKILL.md)
- create-pr: Create a feature branch from current work, commit relevant changes, push to origin, and open or locate a pull request. Use when the user asks to create a PR, package current changes for review, or share a reviewable branch URL. (file: /Users/amaricharles/.codex/skills/create-pr/SKILL.md)
- explain-arch: Explain architecture context for a specific component using a tree-style view of parents, children, and siblings. Use when the user asks how a system is structured, where a component belongs, or how architecture pieces relate. (file: /Users/amaricharles/.codex/skills/explain-arch/SKILL.md)
- explain-flow: Trace a runtime event across architecture layers with a sequence diagram and step-by-step explanation. Use when the user asks how a gameplay action or state change moves through input, session, simulation, and view. (file: /Users/amaricharles/.codex/skills/explain-flow/SKILL.md)
- merge-pr: Finalize and merge a pull request from the current branch by committing and pushing all local changes, merging the PR, updating local main, and deleting the merged feature branch locally and on origin. Use when the user asks to merge a PR end-to-end or ship a branch completely. (file: /Users/amaricharles/.codex/skills/merge-pr/SKILL.md)
- plan-work: Triage and group project bugs and todos into actionable work bundles with urgency, ease, and scope ratings. Use when the user asks what to do next, how to batch tasks, or how to prioritize backlog work. (file: /Users/amaricharles/.codex/skills/plan-work/SKILL.md)
- pr-review: Perform a comprehensive pull request review against project conventions, structural checklists, and tests. Use when the user asks for a PR review, code audit, or readiness check before merge. (file: /Users/amaricharles/.codex/skills/pr-review/SKILL.md)
- refactor-audit: Run a post-refactor architecture audit of a system using structured dimensions and project guidelines. Use when the user asks whether a refactor is complete, coherent, properly wired, and safe. (file: /Users/amaricharles/.codex/skills/refactor-audit/SKILL.md)
- skill-creator: Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Codex's capabilities with specialized knowledge, workflows, or tool integrations. (file: /Users/amaricharles/.codex/skills/.system/skill-creator/SKILL.md)
- skill-installer: Install Codex skills into $CODEX_HOME/skills from a curated list or a GitHub repo path. Use when a user asks to list installable skills, install a curated skill, or install a skill from another repo (including private repos). (file: /Users/amaricharles/.codex/skills/.system/skill-installer/SKILL.md)

### How to use skills
- Discovery: The list above is the skills available in this workspace. Skill bodies live on disk at the listed paths.
- Trigger rules: If the user names a skill (with `$SkillName` or plain text) OR the task clearly matches a skill's description shown above, use that skill for that turn. Multiple mentions mean use them all. Do not carry skills across turns unless re-mentioned.
- Missing/blocked: If a named skill isn't in the list or the path can't be read, say so briefly and continue with the best fallback.
- How to use a skill (progressive disclosure):
  1) After deciding to use a skill, open its `SKILL.md`. Read only enough to follow the workflow.
  2) When `SKILL.md` references relative paths (for example `scripts/foo.py`), resolve them relative to the skill directory listed above first, and only consider other paths if needed.
  3) If `SKILL.md` points to extra folders such as `references/`, load only the specific files needed for the request; do not bulk-load everything.
  4) If `scripts/` exist, prefer running or patching them instead of retyping large code blocks.
  5) If `assets/` or templates exist, reuse them instead of recreating from scratch.
- Coordination and sequencing:
  - If multiple skills apply, choose the minimal set that covers the request and state the order you'll use them.
  - Announce which skill(s) you're using and why (one short line). If you skip an obvious skill, say why.
- Context hygiene:
  - Keep context small: summarize long sections instead of pasting them; only load extra files when needed.
  - Avoid deep reference-chasing: prefer opening only files directly linked from `SKILL.md` unless you're blocked.
  - When variants exist (frameworks, providers, domains), pick only the relevant reference file(s) and note that choice.
- Safety and fallback: If a skill can't be applied cleanly (missing files, unclear instructions), state the issue, pick the next-best approach, and continue.
