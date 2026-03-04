# PR Review Command

Review the current changes following project guidelines and best practices.

## Instructions

1. First, read and internalize the project guidelines:
   - Read `/Users/amaricharles/Code/project-summoner/.claude/CLAUDE.md` for project conventions
   - Read `/Users/amaricharles/Code/project-summoner/docs/workflows/pr-review-guidelines.md` for review process
   - Read `/Users/amaricharles/Code/project-summoner/docs/workflows/code-structure-checklist.md` for anti-patterns

2. Then perform a comprehensive PR review:
   - **IMPORTANT**: Always diff against main branch, not HEAD:
     - Run `git log main..HEAD --oneline` to see commits in the PR
     - Run `git diff main...HEAD --stat` to see changed files vs main
     - Run `git diff main...HEAD` to see the full diff vs main
   - Run full local suite to verify all tests pass:
     - `./tools/run_tests.sh`

3. Output your review using the format specified in pr-review-guidelines.md:
   - High-level summary
   - Major issues (must fix)
   - Minor issues / polish
   - AI-smell checklist
   - Code structure checklist items
   - Suggested next steps

Focus especially on:
- Anti-patterns from the code structure checklist
- AI-typical issues (meta comments, suspicious fallbacks, magic numbers)
- DRY violations and proper abstraction
- Test coverage for new behavior
- Documentation updates needed
