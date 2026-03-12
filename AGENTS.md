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
