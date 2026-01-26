# Refactor Audit Guidelines

Guidelines for performing post-refactor architecture audits.

## Purpose

A refactor audit evaluates whether a significant refactor is:
- **Complete** - All intended changes are implemented
- **Coherent** - The new design is internally consistent
- **Aligned** - Matches the intended vision and design
- **Clean** - No legacy artifacts or dead code remain

## When to Perform

- After completing a major refactor
- Before merging a large architectural PR
- When inheriting or reviewing unfamiliar refactored code
- As part of periodic system health checks

## Audit Dimensions

### 1. Wiring & Integration

Verify that all pathways lead to the new implementation.

**Check for:**
- Entry points correctly connected to new code
- Old pathways, references, or flows still active
- Orphaned modules, dead code paths, unused services
- Signal/event connections properly updated
- Dependency injection wired correctly

**Output format:**
```
| Entry Point | Status | Notes |
|-------------|--------|-------|
| [name]      | ✅/⚠️/❌ | [details] |
```

### 2. Conceptual Coverage

Verify all intended concepts are represented.

**Check for:**
- All design requirements implemented
- Partial implementations or awkward mappings
- Support for planned future extensions
- Missing abstractions or data structures

**Output format:**
```
| Concept | Status | Implementation |
|---------|--------|----------------|
| [name]  | ✅/⚠️/❌ | [how it's done] |
```

### 3. Legacy Artifacts

Identify remnants that should be removed.

**Look for:**
- Leftover files, classes, services from old system
- Naming that no longer reflects behavior
- Comments referencing old approach
- Temporary migration or glue code
- Fallback mechanisms (prohibited per CLAUDE.md)
- "Backwards compatibility" code

**Output format:**
```
| Artifact | Location | Action |
|----------|----------|--------|
| [name]   | [file]   | Delete/Refactor/Keep (with reason) |
```

### 4. Best-Practice Alignment

Evaluate adherence to good architecture.

**Check for:**
- Separation of concerns
- Clear ownership and responsibility boundaries
- Appropriate coupling vs decoupling
- Sensible use of global/shared state
- Data-driven vs hard-coded logic
- Consistent patterns across similar components

**Output format:**
```
| Component | Responsibility | Assessment |
|-----------|----------------|------------|
| [name]    | [what it does] | ✅/⚠️ [notes] |
```

### 5. Conceptual Clarity & Naming

Verify names match responsibilities.

**Check for:**
- Names that reflect actual behavior
- Meaningful, non-leaky abstractions
- God objects or overloaded responsibilities
- Inconsistent terminology
- Legacy naming that confuses new design

**Output format:**
```
| Name | Reflects Responsibility? | Notes |
|------|--------------------------|-------|
| [name] | ✅/⚠️/❌ | [explanation] |
```

### 6. Risk & Regression Analysis

Identify potential future problems.

**Check for:**
- High-risk state transitions or assumptions
- Implicit or undocumented contracts
- Areas likely to regress as system evolves
- Missing error handling or edge cases
- Race conditions or timing dependencies

**Output format:**
```
| Risk | Severity | Mitigation |
|------|----------|------------|
| [description] | 🔴/🟡/🟢 | [how to address] |
```

## Output Format

### Executive Summary

Brief overview of audit findings:
- What was refactored
- Overall assessment (ready/needs work/critical issues)
- Key findings in 2-3 sentences

### Dimension Tables

One table per dimension (see formats above).

### Issue Categories

**Critical Issues (Must Address)**
- Blocking problems that prevent production use
- Data loss risks
- Broken functionality

**Structural Gaps**
- Missing features from the design
- Incomplete implementations
- Integration issues

**Legacy Artifacts to Remove**
- Dead code
- Unused files
- Obsolete comments

**Best-Practice Concerns**
- Code smells
- Maintainability issues
- Pattern violations

**Optional Improvements**
- Nice-to-have enhancements
- Future optimization opportunities
- Documentation improvements

### Files Modified

List all files that were examined or would need changes.

### Conclusion

- Ready for production?
- Remaining work items with priorities
- Recommended next steps

## Severity Levels

- 🔴 **Critical** - Must fix before merge/deploy
- 🟡 **Medium** - Should fix soon, not blocking
- 🟢 **Low** - Nice to have, can defer

## Priority Levels

- **P0** - Fix immediately
- **P1** - Fix before merge
- **P2** - Fix soon after merge
- **P3** - Fix when convenient
- **P4** - Optional/cosmetic

## Example Audit

See `docs/architecture/refactor-audit-2026-01-25-campaign-graph.md` for a complete example.
