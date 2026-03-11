# PR Review Command

Review the current changes following project guidelines and best practices.

## Instructions

1. First, read and internalize the project guidelines:
   - Read `.claude/claude.md` for project conventions
   - Read `docs/workflows/pr-review-guidelines.md` for review process
   - Read `docs/workflows/code-structure-checklist.md` for anti-patterns

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

## PR Review Prompt (Type Safety / Primitive Obsession)

As you review this PR, check for cases where primitive types (`string`, `int`, `float`, etc.) are being used to represent specific domain concepts.

If a variable or parameter represents a meaningful concept (for example: IDs, durations, health, coordinates, IP addresses, etc.), it should use an appropriate domain type or value object instead of a primitive.

Examples:
- `string` -> `UserId`, `MatchId`, `CardId`
- `int` -> `PlayerId`, `TeamId`
- `float` -> `Duration`, `Health`, `Damage`
- `string` -> `IPAddress`, `EmailAddress`

If you find cases like this:
- Refactor the code to introduce or use a more appropriate type.
- Update function signatures and call sites accordingly.
- Ensure the change improves clarity and correctness, not just abstraction.

Avoid over-engineering: only introduce new types when the concept is meaningful, reused, or easy to misuse as a primitive.

Your goal is to eliminate primitive obsession and improve type clarity across the codebase, not just point out issues.

Focus especially on:
- Anti-patterns from the code structure checklist
- AI-typical issues (meta comments, suspicious fallbacks, magic numbers)
- DRY violations and proper abstraction
- Test coverage for new behavior
- Documentation updates needed
- Primitive obsession and domain-type clarity (with pragmatic scope)
