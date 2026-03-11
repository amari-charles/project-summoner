# Refactor Audit Command

Perform a comprehensive post-refactor architecture audit of the specified system.

## Instructions

1. First, read and internalize the project guidelines:
   - Read `.claude/CLAUDE.md` for project conventions
   - Read `docs/workflows/refactor-audit-guidelines.md` for audit process

2. Identify the scope of the refactor:
   - If the user specified a system/feature, focus on that
   - If not specified, check recent commits: `git log --oneline -20`
   - Identify the relevant directories and files

3. Perform a thorough exploration:
   - Use the Explore agent to understand the current architecture
   - Search for the system's entry points, handlers, and data flow
   - Look for legacy artifacts, dead code, and orphaned files
   - Check for documentation about the intended design

4. Evaluate against the six audit dimensions from the guidelines:
   - Wiring & Integration
   - Conceptual Coverage
   - Legacy Artifacts
   - Best-Practice Alignment
   - Conceptual Clarity & Naming
   - Risk & Regression Analysis

5. Output your audit using the format specified in refactor-audit-guidelines.md:
   - Executive summary
   - Findings per dimension
   - Critical issues (must address)
   - Structural gaps
   - Legacy artifacts to remove
   - Best-practice concerns
   - Optional improvements

6. If critical issues are found, offer to fix them.

## Focus Areas

- Entry points correctly wired to new implementation
- Old pathways or references still active
- Orphaned modules or dead code paths
- Missing or partially implemented concepts
- Fallback/compatibility code that should be removed (per CLAUDE.md)
- Naming that doesn't match responsibilities
- High-risk state transitions or assumptions
