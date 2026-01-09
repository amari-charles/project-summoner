# Architecture Issues

This folder contains detailed documentation for significant architectural issues that need resolution.

## Purpose

These documents provide:
- Root cause analysis with specific code references
- Impact assessment
- Proposed solutions with implementation details
- Progress tracking

## Lifecycle

1. **Active**: Issue is identified, documented, and being worked on
2. **Resolved**: Implementation complete, tests passing
3. **Archived/Removed**: After resolution, either:
   - Move to `docs/architecture/issues/resolved/` for historical reference, OR
   - Delete entirely if the document adds no long-term value

## Current Issues

None.

## Resolved Issues

| Issue | Severity | Resolved | Notes |
|-------|----------|----------|-------|
| Card-Unit Tight Coupling | MEDIUM-HIGH | 2026-01-09 | Created UnitSummon, SummonResult abstractions |
| Stats/Upgrades Pipeline | MEDIUM | 2026-01-09 | Created StatKey, UnitStats, UnitStatCalculator |
| HP Bar Cleanup (Multi-Unit Spawns) | HIGH | 2026-01-08 | [resolved/hp-bar-lifecycle.md](resolved/hp-bar-lifecycle.md) |

## When to Create an Issue Doc

Create a doc here when:
- The issue spans multiple files/systems
- Root cause analysis is non-trivial
- The fix requires architectural changes (not just bug fixes)
- Multiple developers need to understand the problem

For simple bugs, use `docs/tracking/bugs.md` instead.
