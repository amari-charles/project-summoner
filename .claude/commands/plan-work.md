# Plan Work Command

Analyze the todo list and bugs list to identify related items that can be tackled together, then rate them by urgency, ease, and scope.

## Instructions

1. Read the tracking documents:
   - Read `docs/tracking/bugs.md` for active bugs
   - Read `docs/tracking/todos.md` for planned tasks

2. Group related items that can be done in one swoop:
   - Look for items touching the same systems/files
   - Identify bugs that would be fixed by a planned todo
   - Find items with shared dependencies
   - Group by feature area (e.g., "Camera", "Units & Combat", "UI")

3. For each group, provide ratings:
   - **Urgency**: 🔴 High / 🟡 Medium / 🟢 Low
     - High: Blocks other work, affects core gameplay, or causes crashes
     - Medium: Noticeable issues but workarounds exist
     - Low: Nice to have, polish items
   - **Ease**: Easy / Medium / Hard
     - Easy: Single file, clear fix, <1 hour
     - Medium: Multiple files, some investigation needed, 1-4 hours
     - Hard: Architecture changes, significant testing, 4+ hours
   - **Scope**: Small / Medium / Large
     - Small: Isolated change, no ripple effects
     - Medium: Touches a few systems
     - Large: Cross-cutting concern, many files affected

4. Output format:

```
## Triage Summary

### Group 1: [Group Name]
**Items:**
- Bug: [bug title]
- Todo: [todo title]

**Ratings:**
- Urgency: 🔴/🟡/🟢
- Ease: Easy/Medium/Hard
- Scope: Small/Medium/Large

**Rationale:** [Why these are grouped, recommended approach]

**Files likely affected:**
- file1.gd
- file2.cs

---

[Repeat for each group]
```

5. After grouping, provide a recommended priority order based on:
   - High urgency + Easy + Small scope = do first
   - Low urgency + Hard + Large scope = defer
   - Consider dependencies between groups

## Notes

- Focus on actionable groupings, not just categories
- Highlight quick wins (easy fixes with high impact)
- Flag any items that seem outdated or already resolved
- If a bug has a matching todo, note that they should be done together
